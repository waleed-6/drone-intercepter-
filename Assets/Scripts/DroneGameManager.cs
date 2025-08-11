using UnityEngine;

public class DroneGameManager : MonoBehaviour
{
    public DefenderDroneAgent defender;
    public TargetDrone target;
    public Transform baseTransform;
    public float captureRadius = 4.0f;
    public float baseRadius = 12.0f;
    public float episodeTimeLimit = 55f;
    public float initialCaptureRadius = 6f;
    public float minCaptureRadius = 3.0f;
    public float difficulty = 0f; // 0..1

    float timer;
    void Start(){ HardReset(); }
    public void HardReset(){
        captureRadius = Mathf.Lerp(initialCaptureRadius, minCaptureRadius, difficulty);
        timer=0f;
        target.baseTransform = baseTransform; target.ResetTarget();
        defender.baseTransform = baseTransform; defender.target = target.transform; defender.ResetAgentState();
    }
    void Update(){
        timer += Time.deltaTime;
        float dIT = Vector3.Distance(defender.transform.position,target.transform.position);
        float dTB = Vector3.Distance(target.transform.position,baseTransform.position);
        if(dIT <= captureRadius){ defender.OnInterceptSuccess(dIT); HardReset(); }
        else if(dTB <= baseRadius){ defender.OnDefenderFailed(); HardReset(); }
        else if(timer >= episodeTimeLimit){ defender.OnTimeExpired(); HardReset(); }
    }
}
