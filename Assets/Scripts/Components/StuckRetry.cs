using UnityEngine;
using UnityEngine.InputSystem;

namespace OneButtonSubmission.Components
{
    /// Softlock watchdog: with zero shells the gun cannot move, so if it sits
    /// still for a few seconds with an empty tank, offer a retry. Z restarts
    /// the current level. The prompt hides itself if ammo appears (e.g. a
    /// pickup respawns into the gun).
    public class StuckRetry : MonoBehaviour
    {
        public Rigidbody body;
        public AmmoSystemBehaviour ammo;
        public HudController hud;
        public System.Action onRetry;

        public float stillSeconds = 2f;
        public float speedThreshold = 0.6f;

        float stillTimer;
        bool prompting;
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

            // kinematic = the summit cutscene took over; never prompt there
            bool stuck = !body.isKinematic
                && ammo.System != null && ammo.System.IsEmpty
                && body.linearVelocity.magnitude < speedThreshold
                && Mathf.Abs(body.angularVelocity.z) < 1f;

            stillTimer = stuck ? stillTimer + Time.deltaTime : 0f;
            bool shouldPrompt = stillTimer >= stillSeconds;
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
