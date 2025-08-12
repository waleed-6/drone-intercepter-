using System;
using System.Reflection;
using UnityEngine;
using Unity.MLAgents.Policies; // BehaviorParameters, BehaviorType, InferenceDevice

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

        // Try to define a 3D continuous action space in a version-agnostic way
        bool configured = TrySetActionSpec(bp, 3) || TrySetBrainParameters(bp, 3);
        if (!configured)
        {
            // Fall back so the build still runs (heuristic control only)
            bp.BehaviorType = BehaviorType.HeuristicOnly;
            Debug.LogWarning("[Bootstrap] Could not configure action space via ML-Agents API; falling back to HeuristicOnly.");
        }

        // DecisionRequester (optional; add only if the type exists)
        TryAddDecisionRequester(defenderGO, decisionPeriod: 5, takeBetween: true);

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

    // Modern: BehaviorParameters.ActionSpec = ActionSpec.MakeContinuous(n)
    static bool TrySetActionSpec(BehaviorParameters bp, int nContinuous)
    {
        try
        {
            var bpType = typeof(BehaviorParameters);
            var prop = bpType.GetProperty("ActionSpec", BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return false;

            var actionSpecType = Type.GetType("Unity.MLAgents.Actuators.ActionSpec, Unity.MLAgents");
            if (actionSpecType == null) return false;

            var make = actionSpecType.GetMethod("MakeContinuous",
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
            if (make == null) return false;

            var spec = make.Invoke(null, new object[] { nContinuous });
            prop.SetValue(bp, spec);
            Debug.Log("[Bootstrap] Set ActionSpec continuous=" + nContinuous);
            return true;
        }
        catch { return false; }
    }

    // Legacy: BehaviorParameters.BrainParameters.VectorActionSize/SpaceType
    static bool TrySetBrainParameters(BehaviorParameters bp, int nContinuous)
    {
        try
        {
            var bpType = typeof(BehaviorParameters);
            var bpProp = bpType.GetProperty("BrainParameters", BindingFlags.Public | BindingFlags.Instance);
            if (bpProp == null) return false;

            object brain = bpProp.CanRead ? bpProp.GetValue(bp) : null;
            var brainType = brain?.GetType() ?? bpProp.PropertyType;
            if (brain == null) brain = Activator.CreateInstance(brainType);

            // SpaceType enum (location varies by version)
            var spaceType = brainType.Assembly.GetType("Unity.MLAgents.Policies.SpaceType")
                           ?? brainType.Assembly.GetType("MLAgents.Policies.SpaceType");
            if (spaceType == null) return false;

            // Fields (older)…
            var fType = brainType.GetField("VectorActionSpaceType", BindingFlags.Public | BindingFlags.Instance);
            var fSize = brainType.GetField("VectorActionSize",      BindingFlags.Public | BindingFlags.Instance);
            if (fType != null && fSize != null)
            {
                fType.SetValue(brain, Enum.Parse(spaceType, "Continuous"));
                fSize.SetValue(brain, new int[] { nContinuous });
                if (bpProp.CanWrite) bpProp.SetValue(bp, brain);
                Debug.Log("[Bootstrap] Set BrainParameters via fields continuous=" + nContinuous);
                return true;
            }

            // …or properties (newer legacy)
            var pType = brainType.GetProperty("VectorActionSpaceType", BindingFlags.Public | BindingFlags.Instance);
            var pSize = brainType.GetProperty("VectorActionSize",      BindingFlags.Public | BindingFlags.Instance);
            if (pType != null && pType.CanWrite && pSize != null && pSize.CanWrite)
            {
                pType.SetValue(brain, Enum.Parse(spaceType, "Continuous"));
                pSize.SetValue(brain, new int[] { nContinuous });
                if (bpProp.CanWrite) bpProp.SetValue(bp, brain);
                Debug.Log("[Bootstrap] Set BrainParameters via properties continuous=" + nContinuous);
                return true;
            }

            return false;
        }
        catch { return false; }
    }

    // Add DecisionRequester if present in this ML-Agents version
    static void TryAddDecisionRequester(GameObject go, int decisionPeriod, bool takeBetween)
    {
        try
        {
            var drType = Type.GetType("Unity.MLAgents.DecisionRequester, Unity.MLAgents");
            if (drType == null) return;

            var comp = go.AddComponent(drType);
            var pPeriod = drType.GetProperty("DecisionPeriod");
            var pTake   = drType.GetProperty("TakeActionsBetweenDecisions");
            if (pPeriod != null && pPeriod.CanWrite) pPeriod.SetValue(comp, decisionPeriod);
            if (pTake   != null && pTake.CanWrite)   pTake.SetValue(comp, takeBetween);
            Debug.Log("[Bootstrap] DecisionRequester attached.");
        }
        catch { /* no-op if missing */ }
    }
}
