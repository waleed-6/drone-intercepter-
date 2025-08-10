using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public Transform interceptor;
    public Transform target;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI distText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timerText;

    float t0;

    void OnEnable(){ t0 = Time.time; }

    void Update()
    {
        if (!interceptor || !target) return;

        Vector3 delta = target.position - interceptor.position;
        float dist = delta.magnitude;

        float relSpeed = Vector3.Dot((target.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero) - 
                                     (interceptor.GetComponent<Rigidbody>()?.velocity ?? Vector3.zero),
                                     delta.normalized);

        statusText.text = relSpeed < -0.5f ? "INTERCEPTING" : (relSpeed > 0.5f ? "FALLING BEHIND" : "TRACKING");
        distText.text = $"DIST: {dist:0.0} m";
        speedText.text = $"Δv · r̂: {-relSpeed:0.0} m/s";
        timerText.text = $"T: {Time.time - t0:0.0}s";
    }
}
