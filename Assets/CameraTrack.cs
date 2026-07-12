using UnityEngine;

public class CameraTrack : MonoBehaviour
{
    public Transform target;
    public Transform floor;
    public float smoothTime = 0.1f;
    public float minHeightOffset = 1f; // camera stays this much above floor

    private Vector3 velocity = Vector3.zero;
    private Vector3 offset;

    void Start()
    {
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        // Prevent camera from going below the floor
        float minY = floor.position.y + minHeightOffset;
        targetPos.y = Mathf.Max(targetPos.y, minY);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPos,
            ref velocity,
            smoothTime
        );

        transform.LookAt(target);
    }
}