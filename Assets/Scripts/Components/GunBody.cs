using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The gun IS the player: a plane-locked rigidbody that tumbles freely.
    /// Every shot adds linear momentum plus an angular kick, so shots fired
    /// while similarly oriented stack into faster flight and faster spin.
    /// No self-righting — landings bounce and skitter until physics settles.
    [RequireComponent(typeof(Rigidbody))]
    public class GunBody : MonoBehaviour
    {
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.maxAngularVelocity = 7f; // Unity default: the tumble governor that keeps aim manageable
        }

        /// Barrel points along local +X; the physics rotation IS the aim.
        public float BarrelAngleDeg => rb.rotation.eulerAngles.z;

        /// Linear kick opposite the barrel + angular kick (muzzle flips toward
        /// the gun's own top side, like real recoil). Velocity fighting the new
        /// shot is cancelled first so the recoil direction always reads clearly.
        public void ApplyRecoil(Vector2 impulse, float spinImpulse, float opposingCancel)
        {
            rb.maxAngularVelocity = 7f; // the entry pinwheel ends the moment you take control
            Vector2 v = RecoilCalculator.CancelOpposing(
                new Vector2(rb.linearVelocity.x, rb.linearVelocity.y), impulse, opposingCancel);
            rb.linearVelocity = new Vector3(v.x, v.y, 0f);
            rb.AddForce(new Vector3(impulse.x, impulse.y, 0f), ForceMode.Impulse);
            rb.AddTorque(0f, 0f, spinImpulse, ForceMode.Impulse);
        }
    }
}
