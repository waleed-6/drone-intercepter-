using UnityEngine;
using Unity.MLAgents;                // Needed for DecisionRequester
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        var baseGO = new GameObject("Base");
        baseGO.transform.position = Vector3.zero;

        var targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        targetGO.name = "Target";
        var trb = targetGO.AddComponent<Rigidbody>();
        trb.useGravity = false;
        var targetDrone = targetGO.AddComponent<TargetDrone>();
        targetDrone.baseTransform = baseGO.transform;

        var defenderGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        defenderGO.name = "Defender";
        var drb = defenderGO.AddComponent<Rigidbody>();
        drb.useGravity = false;
        var agent = defenderGO.AddComponent<DefenderDroneAgent>();
        agent.baseTransform = baseGO.transform;
        agent.target = targetGO.transform;

        // Behavior Parameters (ML-Agents 2.0.1 compatible)
        var bp = defenderGO.AddComponent<BehaviorParameters>();
        bp.BehaviorName = "DroneInterceptor";
        bp.InferenceDevice = InferenceDevice.CPU;
        bp.BehaviorType = BehaviorType.Default;

        bp.BrainParameters = new BrainParameters
        {
            VectorObservationSize = 0, // Observations come from CollectObservations
            NumStackedVectorObservations = 1,
            VectorActionSpaceType = SpaceType.Continuous,
            VectorActionSize = new[] { 3 } // pitch, yaw, thrust
        };

        // Decision Requester
        var dr = defenderGO.AddComponent<DecisionRequester>();
        dr.DecisionPeriod = 5;
        dr.TakeActionsBetweenDecisions = true;

        // Game Manager
        var gmGO = new GameObject("GameManager");
        var gm = gmGO.AddComponent<DroneGameManager>();
        gm.baseTransform = baseGO.transform;
        gm.target = targetDrone;
        gm.defender = agent;

        // Camera for visual demo builds
        var camGO = new GameObject("Main Camera");
        var cam = camGO.AddComponent<Camera>();
        cam.transform.position = new Vector3(0, 40, -60);
        cam.transform.LookAt(baseGO.transform);
    }
}
