using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The one-button heart. Z (or gamepad right-trigger) is the only input:
    ///   - quick TAP  -> snap shot at the current flip angle
    ///   - HOLD       -> bullet time (slow-mo) to aim the slow-flipping gun
    ///   - RELEASE    -> precise shot (slightly stronger)
    /// Bullet time drains a metered budget and then must cool down.
    public class GunController : MonoBehaviour
    {
        [Header("Wiring (set by bootstrap)")]
        public Transform gunPivot;
        public PlayerBody playerBody;
        public AmmoSystemBehaviour ammo;

        [Header("Input")]
        public float holdThreshold = 0.18f; // hold longer than this = aim, else = tap

        GunConfig config;
        float angleDeg;
        FireGate fireGate;
        BulletTimeState bulletTime;
        InputAction fireAction;
        float baseFixedDelta;

        bool holding;
        bool enteredBT;
        float pressTime;

        public event System.Action OnFired;

        public float CurrentAngle => angleDeg;
        public float ReloadProgress => fireGate == null ? 1f : fireGate.ReloadProgress(Time.unscaledTime);
        public float BulletMeter => bulletTime == null ? 1f : bulletTime.MeterFill;
        public bool BulletActive => bulletTime != null && bulletTime.IsActive;
        public bool BulletReady => bulletTime != null && bulletTime.CanActivate;
        public string GunName => config != null ? config.displayName : "";

        void Awake()
        {
            baseFixedDelta = Time.fixedDeltaTime;

            // THE one button. Only action bound in the entire game.
            fireAction = new InputAction("Fire", InputActionType.Button);
            fireAction.AddBinding("<Keyboard>/z");
            fireAction.AddBinding("<Gamepad>/rightTrigger");
            fireAction.started += OnPress;
            fireAction.canceled += OnRelease;
        }

        /// Bootstrap calls this after wiring, so config is ready before the first Update.
        public void Configure(GunConfig cfg)
        {
            config = cfg;
            fireGate = new FireGate(cfg.fireCooldown);
            bulletTime = new BulletTimeState(cfg.bulletTimeDuration, cfg.bulletTimeCooldown);
        }

        void OnEnable() => fireAction?.Enable();

        void OnDisable()
        {
            fireAction?.Disable();
            ExitBulletTime(); // never leave the game stuck in slow motion
        }

        void OnPress(InputAction.CallbackContext ctx)
        {
            holding = true;
            enteredBT = false;
            pressTime = Time.unscaledTime;
        }

        void OnRelease(InputAction.CallbackContext ctx)
        {
            if (!holding) return;
            holding = false;
            if (enteredBT)
            {
                FireShot(true);
                ExitBulletTime();
                enteredBT = false;
            }
            else
            {
                FireShot(false); // snap shot on a quick tap
            }
        }

        void Update()
        {
            if (config == null) return;

            // Gun sweep uses SCALED time, so it flips slowly during bullet time.
            angleDeg = GunRotator.Advance(angleDeg, config.sweepSpeed, Time.deltaTime);
            if (gunPivot != null)
                gunPivot.rotation = Quaternion.Euler(0f, 0f, angleDeg);

            // Meter/cooldown run on UNSCALED time.
            bool depleted = bulletTime.Tick(Time.unscaledDeltaTime);
            if (depleted && enteredBT)
            {
                FireShot(true);      // ran out of slo-mo mid-aim -> auto-fire
                ExitBulletTime();
                enteredBT = false;
                holding = false;
            }

            // Held long enough (and able) -> drop into bullet time.
            if (holding && !enteredBT
                && Time.unscaledTime - pressTime >= holdThreshold
                && bulletTime.CanActivate && HasAmmo())
            {
                bulletTime.Activate();
                EnterBulletTime();
                enteredBT = true;
            }
        }

        bool HasAmmo() => ammo == null || ammo.System.Current > 0;

        bool FireShot(bool precise)
        {
            float now = Time.unscaledTime;
            if (!precise && !fireGate.CanFire(now)) return false; // snap respects pump cooldown
            if (!HasAmmo()) return false;
            if (ammo != null) ammo.System.TryConsume();

            float force = config.recoilForce * (precise ? config.preciseMultiplier : 1f);
            playerBody.ApplyRecoil(RecoilCalculator.Impulse(angleDeg, force));
            fireGate.RegisterFire(now);
            OnFired?.Invoke();
            return true;
        }

        void EnterBulletTime()
        {
            Time.timeScale = config.bulletTimeScale;
            Time.fixedDeltaTime = baseFixedDelta * config.bulletTimeScale;
        }

        void ExitBulletTime()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDelta;
        }
    }
}
