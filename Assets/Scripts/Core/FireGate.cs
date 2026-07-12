using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Enforces the pump-action reload window between shots.
    public class FireGate
    {
        readonly float cooldown;
        float lastFireTime;
        bool hasFired;

        public FireGate(float cooldown)
        {
            this.cooldown = Mathf.Max(0f, cooldown);
            hasFired = false;
            lastFireTime = 0f;
        }

        public bool CanFire(float now)
            => !hasFired || (now - lastFireTime) >= cooldown;

        public void RegisterFire(float now)
        {
            hasFired = true;
            lastFireTime = now;
        }

        public float ReloadProgress(float now)
        {
            if (!hasFired || cooldown <= 0f) return 1f;
            return Mathf.Clamp01((now - lastFireTime) / cooldown);
        }
    }
}
