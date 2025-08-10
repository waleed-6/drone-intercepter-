using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TargetDrone : MonoBehaviour
{
    [Header("References")]
    public Transform baseTransform;

    [Header("Nominal Flight")]
    public float forwardSpeed = 14f;
    public float turnSpeed = 3.2f;
    public float altitude = 28f;

    [Header("Evasive Maneuvers")]
    public float evasiveAmplitude = 5f;     // lateral sway meters
    public float evasiveFrequency = 0.9f;   // Hz
    public float verticalBobAmp = 1.2f;     // small vertical oscillation

    [Header("Spawn")]
    public Vector3 spawnCenter = Vector3.zero;
    public Vector3 spawnRangeXZ = new Vector3(160f, 0f, 160f);

    [Header("Domain Randomization (per episode)")]
    public Vector2 speedRange = new Vector2(11f, 18f);
    public Vector2 turnRange = new Vector2(2.4f, 3.8f);
    public Vector2 ampRange = new Vector2(2f, 7f);
    public Vector2 freqRange = new Vector2(0.6f, 1.4f);
    public Vector2 altitudeRange = new Vector2(22f, 34f);

    Rigidbody rb;
    float t0;
    Vector3 baseDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 0.18f;
        rb.angularDrag = 0.05f;
    }

    public void RandomizeParameters()
    {
        forwardSpeed      = Random.Range(speedRange.x, speedRange.y);
        turnSpeed         = Random.Range(turnRange.x, turnRange.y);
        evasiveAmplitude  = Random.Range(ampRange.x, ampRange.y);
        evasiveFrequency  = Random.Range(freqRange.x, freqRange.y);
        altitude          = Random.Range(altitudeRange.x, altitudeRange.y);
    }

    public void ResetTarget()
    {
        RandomizeParameters();

        var posXZ = spawnCenter + new Vector3(
            Random.Range(-spawnRangeXZ.x, spawnRangeXZ.x),
            0f,
            Random.Range(-spawnRangeXZ.z, spawnRangeXZ.z)
        );

        transform.position = new Vector3(posXZ.x, altitude, posXZ.z);
        baseDir = (baseTransform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(baseDir, Vector3.up);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        t0 = Random.value * 1000f;
    }

    void FixedUpdate()
    {
        if (!baseTransform) return;

        // Desired direction to base with lateral sway (S-curve) and slight vertical bob
        float t = Time.time + t0;
        Vector3 toBase = (baseTransform.position - transform.position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toBase).normalized;
        Vector3 sway = right * (Mathf.Sin(t * 2f * Mathf.PI * evasiveFrequency) * evasiveAmplitude * 0.02f);
        Vector3 dir = (toBase + sway).normalized;

        // Smooth turn toward dir
        Vector3 newFwd = Vector3.RotateTowards(transform.forward, dir, turnSpeed * Time.fixedDeltaTime, 0f);
        transform.rotation = Quaternion.LookRotation(newFwd, Vector3.up);

        // Forward thrust
        rb.AddForce(transform.forward * forwardSpeed, ForceMode.Acceleration);

        // Altitude hold + gentle bobbing
        float altErr = (altitude + Mathf.Sin(t * 2f * Mathf.PI * evasiveFrequency) * verticalBobAmp) - transform.position.y;
        rb.AddForce(Vector3.up * (altErr * 0.7f), ForceMode.Acceleration);
    }
}
