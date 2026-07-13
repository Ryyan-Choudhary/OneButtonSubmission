using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Core;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// The one-button heart. Z (or gamepad right-trigger) is the only input,
    /// and every shot happens on RELEASE — a press alone never spends a shell:
    ///   - TAP  (release before the hold threshold) -> fire; recoil launches
    ///   - HOLD (past the threshold) -> bullet time (slow-mo) to aim the tumble
    ///   - RELEASE from bullet time -> aimed shot (slightly stronger)
    /// There is no aim arrow: the gun's physical rotation IS the aim, so
    /// shots are timed against the tumble.
    public class GunController : MonoBehaviour
    {
        [Header("Wiring (set by bootstrap)")]
        public GunBody body;
        public AmmoSystemBehaviour ammo;

        [Header("Input")]
        public float holdThreshold = 0.18f; // held longer than this = bullet time

        GunConfig config;
        FireGate fireGate;
        BulletTimeState bulletTime;
        InputAction fireAction;
        float baseFixedDelta;

        bool holding;
        bool enteredBT;
        float pressTime;

        public event System.Action OnFired;

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
            // no shot yet — a press can still become a slow-mo hold, and
            // firing here would waste the shell before we know which it is
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
                FireShot(true); // the aimed shot, timed against the slow tumble
                ExitBulletTime();
                enteredBT = false;
            }
            else
            {
                FireShot(false); // quick tap: the snap shot happens on release
            }
        }

        void Update()
        {
            if (config == null) return;

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
            if (!fireGate.CanFire(now)) return false; // EVERY shot respects the pump cooldown
            if (!HasAmmo()) return false;
            if (ammo != null) ammo.System.TryConsume();

            float mult = precise ? config.preciseMultiplier : 1f;
            body.ApplyRecoil(
                RecoilCalculator.Impulse(body.BarrelAngleDeg, config.recoilForce * mult,
                    config.jumpBoost * mult),
                config.spinImpulse * mult, config.opposingCancel);
            fireGate.RegisterFire(now);

            // the slug itself: flies out of the muzzle opposite the recoil
            float rad = body.BarrelAngleDeg * Mathf.Deg2Rad;
            Vector3 muzzleDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            Bullet.Spawn(transform.TransformPoint(0.82f, 0.12f, 0f), muzzleDir, transform);

            AudioManager.Play(AudioManager.Sfx.Gunshot);
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
            // spend the meter: without this the Active phase kept draining
            // invisibly after an early release (and the HUD bar, now only
            // shown while active, would linger). No-op if already spent.
            bulletTime?.Deactivate();
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDelta;
        }
    }
}