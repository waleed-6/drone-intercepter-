using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class DefenderDroneAgent : Agent
{
    [Header("Refs")]
    public Transform target;
    public Transform baseTransform;

    [Header("Flight")]
    public float maxThrust = 22f;
    public float turnTorque = 5f;
    public float desiredAltitude = 28f;
    public float altitudeGain = 0.9f;
    public float drag = 0.22f;

    [Header("Rewards")]
    public float stepPenalty = -0.0008f;
    public float closeRateCoeff = 0.0035f;        // improve distance to target
    public float facingCoeff = 0.0025f;           // align nose with target dir
    public float velMatchCoeff = 0.0015f;         // match velocity vectors
    public float smoothnessPenaltyCoeff = -0.0005f; // penalize jittery rotation
    public float interceptBonus = 3.0f;
    public float failPenalty = -3.0f;
    public float timePenalty = -0.3f;

    Rigidbody rb;
    Vector3 prevDelta;
    Vector3 prevAngVel;

    const float distNorm = 250f; // normalize distances

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = drag;
        rb.angularDrag = 0.05f;

        prevDelta = Vector3.zero;
        prevAngVel = Vector3.zero;
    }

    public void ResetAgentState()
    {
        // spawn on a ring around base at desired altitude
        Vector2 ring = Random.insideUnitCircle.normalized * Random.Range(24f, 70f);
        Vector3 pos = baseTransform.position + new Vector3(ring.x, desiredAltitude, ring.y);
        transform.position = pos;
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        prevDelta = target ? (target.position - transform.position) : Vector3.zero;
        prevAngVel = Vector3.zero;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (!target || !baseTransform)
        {
            sensor.AddObservation(new float[24]); // keep obs size consistent
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        Vector3 toBase   = baseTransform.position - transform.position;

        float dIT = Mathf.Clamp01(toTarget.magnitude / distNorm);
        float dIB = Mathf.Clamp01(toBase.magnitude / distNorm);

        Vector3 dirIT = toTarget.normalized;
        Vector3 dirIB = toBase.normalized;

        Vector3 vSelfLocal = transform.InverseTransformDirection(rb.velocity);

        Vector3 vTar = Vector3.zero;
        var trb = target.GetComponent<Rigidbody>();
        if (trb) vTar = trb.velocity;

        Vector3 vRelLocal = transform.InverseTransformDirection(vTar - rb.velocity);

        float facing = Vector3.Dot(transform.forward, dirIT); // [-1..1]

        sensor.AddObservation(dirIT);          // 3
        sensor.AddObservation(dIT);            // 1
        sensor.AddObservation(dirIB);          // 3
        sensor.AddObservation(dIB);            // 1
        sensor.AddObservation(vSelfLocal);     // 3
        sensor.AddObservation(vRelLocal);      // 3
        sensor.AddObservation(facing);         // 1
        sensor.AddObservation(transform.up);   // 3 (rough attitude)
        sensor.AddObservation(1f);             // 1 LOS placeholder
        sensor.AddObservation((desiredAltitude - transform.position.y) / 60f); // 1
        sensor.AddObservation(rb.angularVelocity * 0.1f); // 3
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!target) return;

        // Continuous actions: [pitch, yaw, thrust01]
        float pitch = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float yaw   = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float thr01 = Mathf.Clamp01((actions.ContinuousActions[2] + 1f) * 0.5f);

        // Steering torques
        rb.AddRelativeTorque(new Vector3(pitch * turnTorque, yaw * turnTorque, 0f), ForceMode.Acceleration);

        // Forward thrust
        rb.AddForce(transform.forward * (thr01 * maxThrust), ForceMode.Acceleration);

        // Altitude hold
        float altErr = desiredAltitude - transform.position.y;
        rb.AddForce(Vector3.up * (altErr * altitudeGain), ForceMode.Acceleration);

        // Rewards
        Vector3 delta = target.position - transform.position;

        // 1) Get closer
        float dd = (prevDelta.magnitude - delta.magnitude);
        AddReward(dd * closeRateCoeff);

        // 2) Face target
        float face = Vector3.Dot(transform.forward, delta.normalized);
        AddReward(face * facingCoeff);

        // 3) Match velocity direction (useful for intercept geometry)
        Vector3 vTar = Vector3.zero;
        var trb = target.GetComponent<Rigidbody>();
        if (trb) vTar = trb.velocity;
        float velAlign = Vector3.Dot(
            (rb.velocity.normalized + Vector3.one * 1e-6f),
            (vTar.normalized + Vector3.one * 1e-6f)
        );
        AddReward(velAlign * velMatchCoeff);

        // 4) Smoothness (penalize sudden angular changes)
        Vector3 dang = rb.angularVelocity - prevAngVel;
        AddReward(dang.magnitude * smoothnessPenaltyCoeff);
        prevAngVel = rb.angularVelocity;

        // Small step penalty
        AddReward(stepPenalty);

        prevDelta = delta;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        float pitch = Input.GetKey(KeyCode.UpArrow) ? 1f :
                      (Input.GetKey(KeyCode.DownArrow) ? -1f : 0f);
        float yaw   = Input.GetKey(KeyCode.LeftArrow) ? -1f :
                      (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f);
        float thr   = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);

        ca[0] = pitch;
        ca[1] = yaw;
        ca[2] = Mathf.Clamp(thr, -1f, 1f);
    }

    // Callbacks from manager
    public void OnInterceptSuccess(float catchDist)
    {
        AddReward(interceptBonus + Mathf.Clamp01(1f - catchDist / 20f));
        EndEpisode();
    }

    public void OnDefenderFailed()
    {
        AddReward(failPenalty);
        EndEpisode();
    }

    public void OnTimeExpired()
    {
        AddReward(timePenalty);
        EndEpisode();
    }
}
