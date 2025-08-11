using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(Rigidbody))]
public class DefenderDroneAgent : Agent
{
    public Transform target;
    public Transform baseTransform;
    public float maxThrust = 22f;
    public float turnTorque = 5f;
    public float desiredAltitude = 28f;
    public float altitudeGain = 0.9f;
    public float drag = 0.22f;

    public float stepPenalty = -0.0008f;
    public float closeRateCoeff = 0.0035f;
    public float facingCoeff = 0.0025f;
    public float velMatchCoeff = 0.0015f;
    public float smoothnessPenaltyCoeff = -0.0005f;
    public float interceptBonus = 3.0f;
    public float failPenalty = -3.0f;
    public float timePenalty = -0.3f;

    Rigidbody rb; Vector3 prevDelta; Vector3 prevAngVel;
    const float distNorm = 250f;

    public override void Initialize(){
        rb=GetComponent<Rigidbody>(); rb.useGravity=false; rb.drag=drag; rb.angularDrag=0.05f;
        prevDelta=Vector3.zero; prevAngVel=Vector3.zero;
    }
    public void ResetAgentState(){
        Vector2 ring=Random.insideUnitCircle.normalized*Random.Range(24f,70f);
        Vector3 pos=baseTransform.position+new Vector3(ring.x,desiredAltitude,ring.y);
        transform.position=pos; transform.rotation=Quaternion.Euler(0f,Random.Range(0f,360f),0f);
        rb.velocity=Vector3.zero; rb.angularVelocity=Vector3.zero;
        prevDelta = target? (target.position-transform.position): Vector3.zero; prevAngVel=Vector3.zero;
    }
    public override void CollectObservations(VectorSensor sensor){
        if(!target || !baseTransform){ sensor.AddObservation(new float[24]); return; }
        Vector3 toTarget=target.position-transform.position;
        Vector3 toBase=baseTransform.position-transform.position;
        float dIT=Mathf.Clamp01(toTarget.magnitude/distNorm);
        float dIB=Mathf.Clamp01(toBase.magnitude/distNorm);
        Vector3 dirIT=toTarget.normalized; Vector3 dirIB=toBase.normalized;
        Vector3 vSelfLocal=transform.InverseTransformDirection(rb.velocity);
        Vector3 vTar=Vector3.zero; var trb=target.GetComponent<Rigidbody>(); if(trb) vTar=trb.velocity;
        Vector3 vRelLocal=transform.InverseTransformDirection(vTar-rb.velocity);
        float facing=Vector3.Dot(transform.forward,dirIT);
        sensor.AddObservation(dirIT); sensor.AddObservation(dIT);
        sensor.AddObservation(dirIB); sensor.AddObservation(dIB);
        sensor.AddObservation(vSelfLocal); sensor.AddObservation(vRelLocal);
        sensor.AddObservation(facing); sensor.AddObservation(transform.up);
        sensor.AddObservation(1f);
        sensor.AddObservation((desiredAltitude-transform.position.y)/60f);
        sensor.AddObservation(rb.angularVelocity*0.1f);
    }
    public override void OnActionReceived(ActionBuffers actions){
        if(!target) return;
        float pitch=Mathf.Clamp(actions.ContinuousActions[0],-1f,1f);
        float yaw=Mathf.Clamp(actions.ContinuousActions[1],-1f,1f);
        float thr01=Mathf.Clamp01((actions.ContinuousActions[2]+1f)*0.5f);
        rb.AddRelativeTorque(new Vector3(pitch*turnTorque,yaw*turnTorque,0f),ForceMode.Acceleration);
        rb.AddForce(transform.forward*(thr01*maxThrust),ForceMode.Acceleration);
        float altErr=desiredAltitude-transform.position.y;
        rb.AddForce(Vector3.up*(altErr*altitudeGain),ForceMode.Acceleration);
        Vector3 delta=target.position-transform.position;
        float dd=(prevDelta.magnitude-delta.magnitude); AddReward(dd*closeRateCoeff);
        float facing=Vector3.Dot(transform.forward,delta.normalized); AddReward(facing*facingCoeff);
        Vector3 vTar=Vector3.zero; var trb=target.GetComponent<Rigidbody>(); if(trb) vTar=trb.velocity;
        float velAlign=Vector3.Dot((rb.velocity.normalized+Vector3.one*1e-6f),(vTar.normalized+Vector3.one*1e-6f));
        AddReward(velAlign*velMatchCoeff);
        Vector3 dang=rb.angularVelocity-prevAngVel; AddReward(dang.magnitude*smoothnessPenaltyCoeff); prevAngVel=rb.angularVelocity;
        AddReward(stepPenalty);
        prevDelta=delta;
    }
    public override void Heuristic(in ActionBuffers actionsOut){
        var ca=actionsOut.ContinuousActions;
        float pitch=Input.GetKey(KeyCode.UpArrow)?1f:(Input.GetKey(KeyCode.DownArrow)?-1f:0f);
        float yaw=Input.GetKey(KeyCode.LeftArrow)?-1f:(Input.GetKey(KeyCode.RightArrow)?1f:0f);
        float thr=(Input.GetKey(KeyCode.W)?1f:0f)-(Input.GetKey(KeyCode.S)?1f:0f);
        ca[0]=pitch; ca[1]=yaw; ca[2]=Mathf.Clamp(thr,-1f,1f);
    }
    public void OnInterceptSuccess(float catchDist){ AddReward(interceptBonus+Mathf.Clamp01((1f-catchDist/20f))); EndEpisode(); }
    public void OnDefenderFailed(){ AddReward(failPenalty); EndEpisode(); }
    public void OnTimeExpired(){ AddReward(timePenalty); EndEpisode(); }
}
