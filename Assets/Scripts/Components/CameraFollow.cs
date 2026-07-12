using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Smoothly follows the player in the X-Y plane with an upward look-ahead offset.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.25f;
        public Vector3 offset = new Vector3(0f, 1.5f, -12f);

        Vector3 velocity;

        void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = new Vector3(target.position.x, target.position.y, 0f) + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
