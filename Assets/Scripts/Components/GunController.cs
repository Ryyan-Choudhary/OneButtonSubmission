using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The one-button heart: sweeps the gun, and on the single Shoot button
    /// consumes a shell and blasts the body opposite the barrel (if reloaded).
    public class GunController : MonoBehaviour
    {
        [Header("Sweep")]
        public float rotationSpeedDegPerSec = 120f;

        [Header("Recoil")]
        public float recoilForce = 10f;
        public float cooldownSeconds = 0.6f;

        public Transform gunPivot;
        public PlayerBody playerBody;
        public AmmoSystemBehaviour ammo;

        float angleDeg;
        FireGate fireGate;
        InputAction fireAction;

        public float CurrentAngle => angleDeg;
        public float ReloadProgress => fireGate == null ? 1f : fireGate.ReloadProgress(Time.time);

        void Awake()
        {
            fireGate = new FireGate(cooldownSeconds);

            // THE one button. Only action bound in the entire game.
            fireAction = new InputAction("Shoot", InputActionType.Button);
            fireAction.AddBinding("<Keyboard>/space");
            fireAction.AddBinding("<Mouse>/leftButton");
            fireAction.AddBinding("<Gamepad>/buttonSouth");
            fireAction.performed += OnFire;
        }

        void OnEnable() => fireAction?.Enable();
        void OnDisable() => fireAction?.Disable();

        void Update()
        {
            angleDeg = GunRotator.Advance(angleDeg, rotationSpeedDegPerSec, Time.deltaTime);
            if (gunPivot != null)
                gunPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg); // world-space sweep
        }

        void OnFire(InputAction.CallbackContext ctx)
        {
            float now = Time.time;
            if (!fireGate.CanFire(now)) return;
            if (ammo != null && !ammo.System.TryConsume()) return;

            Vector2 impulse = RecoilCalculator.Impulse(angleDeg, recoilForce);
            playerBody.ApplyRecoil(impulse);
            fireGate.RegisterFire(now);
        }
    }
}
