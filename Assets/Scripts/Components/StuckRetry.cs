using UnityEngine;
using UnityEngine.InputSystem;

namespace OneButtonSubmission.Components
{
    /// Retry watchdog, two fail states:
    ///  1. Stuck — zero shells + sitting still: the gun cannot move at all.
    ///  2. Fallen — dropped below the world (there is no floor); the camera
    ///     stays put while the gun disappears off-screen.
    /// Either way: "RETRY ?" and Z restarts the level. The stuck prompt hides
    /// itself if ammo appears (e.g. a pickup respawns into the gun); a fall is
    /// latched and always ends in a retry.
    public class StuckRetry : MonoBehaviour
    {
        public Rigidbody body;
        public AmmoSystemBehaviour ammo;
        public HudController hud;
        public GunController gun;
        public System.Action onRetry;

        public float stillSeconds = 2f;
        public float speedThreshold = 0.6f;
        public float fallY = -12f;

        float stillTimer;
        bool prompting;
        bool fallen;
        InputAction retryAction;

        void Awake()
        {
            retryAction = new InputAction("Retry", InputActionType.Button);
            retryAction.AddBinding("<Keyboard>/z");
            retryAction.AddBinding("<Gamepad>/rightTrigger");
            retryAction.started += OnRetryPressed;
        }

        void OnDisable()
        {
            retryAction?.Disable();
            if (prompting && hud != null) hud.bannerText = "";
            prompting = false;
        }

        void Update()
        {
            if (body == null || ammo == null || hud == null) return;

            // fell out of the world: latch it, cut the controls, let it drop
            if (!fallen && !body.isKinematic && body.position.y < fallY)
            {
                fallen = true;
                if (gun != null) gun.enabled = false;
            }

            // kinematic = the summit cutscene took over; never prompt there
            bool stuck = !body.isKinematic
                && ammo.System != null && ammo.System.IsEmpty
                && body.linearVelocity.magnitude < speedThreshold
                && Mathf.Abs(body.angularVelocity.z) < 1f;

            stillTimer = stuck ? stillTimer + Time.deltaTime : 0f;
            bool shouldPrompt = fallen || stillTimer >= stillSeconds;
            if (shouldPrompt == prompting) return;

            prompting = shouldPrompt;
            hud.bannerText = prompting ? "RETRY ?  — press Z" : "";
            if (prompting) retryAction.Enable();
            else retryAction.Disable();
        }

        void OnRetryPressed(InputAction.CallbackContext ctx)
        {
            if (!prompting) return;
            prompting = false;
            retryAction.Disable();
            hud.bannerText = "";
            onRetry?.Invoke();
        }
    }
}
