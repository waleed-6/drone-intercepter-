using UnityEngine;
using UnityEngine.UI;

public class RadarMinimap : MonoBehaviour
{
    public Transform origin;
    public Transform interceptor;
    public Transform target;
    public RectTransform panel;
    public RectTransform dotInterceptor;
    public RectTransform dotTarget;
    public float rangeMeters = 250f;

    void Update()
    {
        if (!origin || !panel) return;
        Place(interceptor, dotInterceptor);
        Place(target, dotTarget);
    }

    void Place(Transform t, RectTransform dot)
    {
        if (!t || !dot){ if (dot) dot.gameObject.SetActive(false); return; }
        dot.gameObject.SetActive(true);

        Vector3 local = t.position - origin.position;
        Vector2 norm = new Vector2(local.x, local.z) / rangeMeters;
        norm = Vector2.ClampMagnitude(norm, 0.48f);

        Vector2 half = panel.rect.size * 0.5f;
        dot.anchoredPosition = norm * half;
    }
}
