using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Smoothly follows the player in the X-Y plane with an upward look-ahead offset.
    /// The camera never descends below minY: when the gun falls out of the world
    /// it drops off-screen while the view stays put.
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smoothTime = 0.25f;
        public Vector3 offset = new Vector3(0f, 1.5f, -12f);
        public float minY = float.NegativeInfinity;

        Vector3 velocity;

        public void SnapToTarget()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = Desired();
            transform.rotation = Quaternion.identity;
        }

        Vector3 Desired()
        {
            Vector3 d = new Vector3(target.position.x, target.position.y, 0f) + offset;
            d.y = Mathf.Max(d.y, minY);
            return d;
        }

        void LateUpdate()
        {
            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }
    }
}
