using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The player's physical body: plane-locked rigidbody with a self-righting spring.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerBody : MonoBehaviour
    {
        [Header("Self-righting spring")]
        public float uprightKp = 200f;
        public float uprightKd = 30f;
        public float uprightMaxTorque = 400f;

        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        void FixedUpdate()
        {
            float leanDeg = transform.eulerAngles.z;
            float angVelDeg = rb.angularVelocity.z * Mathf.Rad2Deg;
            float torque = UprightController.ComputeTorque(
                leanDeg, angVelDeg, uprightKp, uprightKd, uprightMaxTorque);
            rb.AddTorque(0f, 0f, torque * Mathf.Deg2Rad, ForceMode.Acceleration);
        }

        public void ApplyRecoil(Vector2 impulse)
        {
            rb.AddForce(new Vector3(impulse.x, impulse.y, 0f), ForceMode.Impulse);
        }
    }
}
