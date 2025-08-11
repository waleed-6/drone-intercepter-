using UnityEngine;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Actuators;

public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        var baseGO = new GameObject("Base"); baseGO.transform.position = Vector3.zero;

        var targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere); targetGO.name="Target";
        var trb = targetGO.AddComponent<Rigidbody>(); trb.useGravity=false;
        var targetDrone = targetGO.AddComponent<TargetDrone>(); targetDrone.baseTransform = baseGO.transform;

        var defenderGO = GameObject.CreatePrimitive(PrimitiveType.Capsule); defenderGO.name="Defender";
        var drb = defenderGO.AddComponent<Rigidbody>(); drb.useGravity=false;
        var agent = defenderGO.AddComponent<DefenderDroneAgent>();
        agent.baseTransform = baseGO.transform; agent.target = targetGO.transform;

        var bp = defenderGO.AddComponent<BehaviorParameters>();
        bp.BehaviorName = "DroneInterceptor"; bp.InferenceDevice = InferenceDevice.CPU;
        bp.ActionSpec = ActionSpec.MakeContinuous(3);

        var dr = defenderGO.AddComponent<DecisionRequester>();
        dr.DecisionPeriod = 5; dr.TakeActionsBetweenDecisions = true;

        var gmGO = new GameObject("GameManager"); var gm = gmGO.AddComponent<DroneGameManager>();
        gm.baseTransform = baseGO.transform; gm.target = targetDrone; gm.defender = agent;

        var camGO = new GameObject("Main Camera"); var cam = camGO.AddComponent<Camera>();
        cam.transform.position = new Vector3(0,40,-60); cam.transform.LookAt(baseGO.transform);
    }
}
