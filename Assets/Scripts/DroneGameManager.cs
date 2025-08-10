using UnityEngine;

public class DroneGameManager : MonoBehaviour
{
    [Header("References")]
    public DefenderDroneAgent defender;
    public TargetDrone target;
    public Transform baseTransform;

    [Header("Episode Rules")]
    public float captureRadius = 4.0f;
    public float baseRadius = 12.0f;
    public float episodeTimeLimit = 55f;

    [Header("Curriculum Levers")]
    public float initialCaptureRadius = 6f;
    public float minCaptureRadius = 3.0f;
    public float difficulty = 0f;   // 0..1 set by Python via env params if desired

    float timer;

    void Start() => HardReset();

    public void HardReset()
    {
        // curriculum: shrink capture radius as difficulty increases
        float cap = Mathf.Lerp(initialCaptureRadius, minCaptureRadius, difficulty);
        captureRadius = cap;

        timer = 0f;

        target.baseTransform = baseTransform;
        target.ResetTarget();

        defender.baseTransform = baseTransform;
        defender.target = target.transform;
        defender.ResetAgentState();
    }

    void Update()
    {
        timer += Time.deltaTime;

        float dIT = Vector3.Distance(defender.transform.position, target.transform.position);
        float dTB = Vector3.Distance(target.transform.position, baseTransform.position);

        if (dIT <= captureRadius)           { defender.OnInterceptSuccess(dIT); HardReset(); }
        else if (dTB <= baseRadius)         { defender.OnDefenderFailed();      HardReset(); }
        else if (timer >= episodeTimeLimit) { defender.OnTimeExpired();         HardReset(); }
    }
}
