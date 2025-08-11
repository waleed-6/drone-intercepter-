using UnityEngine;
using Unity.MLAgents;                // for DecisionRequester
using Unity.MLAgents.Policies;       // for BehaviorParameters, BehaviorType, InferenceDevice
using Unity.MLAgents.Actuators;      // for ActionSpec

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

        // Define a 3D continuous action space: [pitch, yaw, throttle]
        bp.ActionSpec = ActionSpec.MakeContinuous(3);

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
}
