using UnityEngine;
using UnityEngine.InputSystem;

namespace OneButtonSubmission.Components
{
    /// Level 3 rival shelf: landing near the villain makes him snatch the gun,
    /// then offers the same Z retry as the stuck watchdog.
    public class VillainTrigger : MonoBehaviour
    {
        public SummitAgent villain;
        public HudController hud;
        public System.Action onRetry;

        bool consumed;
        bool prompting;
        InputAction retryAction;

        void Awake()
        {
            retryAction = new InputAction("Retry", InputActionType.Button);
            retryAction.AddBinding("<Keyboard>/z");
            retryAction.AddBinding("<Gamepad>/rightTrigger");
            retryAction.started += OnRetryPressed;
        }

        void OnDestroy()
        {
            if (retryAction == null) return;
            retryAction.started -= OnRetryPressed;
            retryAction.Dispose();
        }

        void OnTriggerEnter(Collider other)
        {
            if (consumed || villain == null) return;
            var body = other.GetComponentInParent<GunBody>();
            if (body == null) return;

            consumed = true;
            villain.StartCutscene(body, ShowRetry, fullEscape: false);
        }

        /// A bullet found him first: no theft, he blows apart downrange and
        /// this trigger goes dead — the route is clear. Returns false when
        /// there was nothing left to kill (already dead or mid-steal), so
        /// the shooter doesn't collect a bounty twice.
        public bool KillVillain(Vector3 hitDir)
        {
            if (consumed) return false;
            consumed = true;
            var box = GetComponent<Collider>();
            if (box != null) box.enabled = false;
            if (villain != null) villain.Die(hitDir);
            return true;
        }

        void ShowRetry()
        {
            if (hud == null) return;
            prompting = true;
            hud.bannerText = "RETRY ?  — press Z";
            retryAction.Enable();
        }

        void OnRetryPressed(InputAction.CallbackContext ctx)
        {
            if (!prompting) return;
            prompting = false;
            retryAction.Disable();
            if (hud != null) hud.bannerText = "";
            onRetry?.Invoke();
        }
    }

}
