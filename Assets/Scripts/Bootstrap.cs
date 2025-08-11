using System;
using System.Reflection;
using UnityEngine;
using Unity.MLAgents;                // DecisionRequester
using Unity.MLAgents.Policies;       // BehaviorParameters, BehaviorType, InferenceDevice
using Unity.MLAgents.Actuators;      // ActionSpec (if available)

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        // ---- Base (goal) ----
        var baseGO = new GameObject("Base");
        baseGO.transform.position = Vector3.zero;

        // ---- Target (attacker) ----
        var targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetGO.name = "Target";
        var trb = targetGO.AddComponent<Rigidbody>();
        trb.useGravity = false;
        var targetDrone = targetGO.AddComponent<TargetDrone>();
        targetDrone.baseTransform = baseGO.transform;

        // ---- Defender (agent) ----
        var defenderGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        defenderGO.name = "Defender";
        var drb = defenderGO.AddComponent<Rigidbody>();
        drb.useGravity = false;

        var agent = defenderGO.AddComponent<DefenderDroneAgent>();
        agent.baseTransform = baseGO.transform;
        agent.target = targetGO.transform;

        // ---- ML-Agents behavior setup ----
        var bp = defenderGO.AddComponent<BehaviorParameters>();
        bp.BehaviorName    = "DroneInterceptor";
        bp.InferenceDevice = InferenceDevice.CPU;
        bp.BehaviorType    = BehaviorType.Default;

        // Try modern API first: BehaviorParameters.ActionSpec (ML-Agents >= ~1.7)
        bool configured = TrySetActionSpec(bp, 3);

        // Fallback to legacy BrainParameters if present (older ML-Agents)
        if (!configured) { configured = TrySetBrainParameters(bp, 3); }

        // If neither API was available, at least make the build runnable (heuristic only).
        if (!configured) { bp.BehaviorType = BehaviorType.HeuristicOnly; }

        // Request regular decisions
        var requester = defenderGO.AddComponent<DecisionRequester>();
        requester.DecisionPeriod = 5;
        requester.TakeActionsBetweenDecisions = true;

        // ---- Game manager ----
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<DroneGameManager>();
        gm.baseTransform = baseGO.transform;
        gm.target = targetDrone;
        gm.defender = agent;

        // ---- Simple camera for demo builds ----
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.transform.position = new Vector3(0, 40, -60);
        cam.transform.LookAt(baseGO.transform);
    }

    // --- Helpers -------------------------------------------------------------

    // Modern path: BehaviorParameters.ActionSpec = ActionSpec.MakeContinuous(n)
    static bool TrySetActionSpec(BehaviorParameters bp, int nContinuous)
    {
        try
        {
            var prop = typeof(BehaviorParameters).GetProperty("ActionSpec",
                BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return false;

            // Create ActionSpec via static MakeContinuous(int)
            var make = typeof(ActionSpec).GetMethod("MakeContinuous",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            if (make == null) return false;

            var spec = make.Invoke(null, new object[] { nContinuous });
            prop.SetValue(bp, spec);
            return true;
        }
        catch { return false; }
    }

    // Legacy path: BehaviorParameters.BrainParameters.VectorActionSize / SpaceType
    static bool TrySetBrainParameters(BehaviorParameters bp, int nContinuous)
    {
        try
        {
            var bpProp = typeof(BehaviorParameters).GetProperty("BrainParameters",
                BindingFlags.Public | BindingFlags.Instance);
            if (bpProp == null || !bpProp.CanWrite && !bpProp.CanRead) return false;

            object brain = null;

            // If read-only getter exists, try to clone/modify it; else create a new instance.
            if (bpProp.CanRead)
                brain = bpProp.GetValue(bp);

            var brainType = brain?.GetType() ?? bpProp.PropertyType;
            if (brain == null) brain = Activator.CreateInstance(brainType);

            // SpaceType enum lives under Unity.MLAgents.Policies in older builds
            var spaceType = brainType.Assembly.GetType("Unity.MLAgents.Policies.SpaceType")
                            ?? brainType.Assembly.GetType("MLAgents.Policies.SpaceType");

            // Try fields first (older versions used fields)
            var fActType = brainType.GetField("VectorActionSpaceType", BindingFlags.Public | BindingFlags.Instance);
            var fActSize = brainType.GetField("VectorActionSize",      BindingFlags.Public | BindingFlags.Instance);

            if (fActType != null && fActSize != null && spaceType != null)
            {
                fActType.SetValue(brain, Enum.Parse(spaceType, "Continuous"));
                fActSize.SetValue(brain, new int[] { nContinuous });
                if (bpProp.CanWrite) bpProp.SetValue(bp, brain);
                return true;
            }

            // Try properties (some mid versions used properties)
            var pActType = brainType.GetProperty("VectorActionSpaceType", BindingFlags.Public | BindingFlags.Instance);
            var pActSize = brainType.GetProperty("VectorActionSize",      BindingFlags.Public | BindingFlags.Instance);

            if (pActType != null && pActType.CanWrite && pActSize != null && pActSize.CanWrite && spaceType != null)
            {
                pActType.SetValue(brain, Enum.Parse(spaceType, "Continuous"));
                pActSize.SetValue(brain, new int[] { nContinuous });
                if (bpProp.CanWrite) bpProp.SetValue(bp, brain);
                return true;
            }

            return false;
        }
        catch { return false; }
    }
}
