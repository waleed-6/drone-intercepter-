using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0,-2.5f,-8f);
    public float posLerp = 6f;
    public float rotLerp = 6f;

    void LateUpdate()
    {
        if (!target) return;
        Vector3 desired = target.TransformPoint(offset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime * posLerp);
        Quaternion look = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * rotLerp);
    }
}
