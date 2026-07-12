using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Pure state machine for the bullet-time meter: Ready -> Active -> Cooldown -> Ready.
    /// Driven by unscaled delta time so slow-motion never distorts its own timing.
    public class BulletTimeState
    {
        public enum Phase { Ready, Active, Cooldown }

        readonly float duration;
        readonly float cooldown;
        float timer;

        public Phase Current { get; private set; } = Phase.Ready;

        public BulletTimeState(float duration, float cooldown)
        {
            this.duration = Mathf.Max(0.01f, duration);
            this.cooldown = Mathf.Max(0f, cooldown);
        }

        public bool CanActivate => Current == Phase.Ready;
        public bool IsActive => Current == Phase.Active;

        /// Full when Ready, drains while Active, refills while Cooling. Always 0..1.
        public float MeterFill
        {
            get
            {
                switch (Current)
                {
                    case Phase.Active:   return Mathf.Clamp01(1f - timer / duration);
                    case Phase.Cooldown: return cooldown <= 0f ? 1f : Mathf.Clamp01(timer / cooldown);
                    default:             return 1f;
                }
            }
        }

        public void Activate()
        {
            if (Current == Phase.Ready)
            {
                Current = Phase.Active;
                timer = 0f;
            }
        }

        /// Player released (or fired) while aiming -> spend it, begin cooldown.
        public void Deactivate()
        {
            if (Current == Phase.Active)
            {
                Current = Phase.Cooldown;
                timer = 0f;
            }
        }

        /// Advance the meter. Returns true on the tick where Active ran out (auto-fire).
        public bool Tick(float dt)
        {
            if (Current == Phase.Active)
            {
                timer += dt;
                if (timer >= duration)
                {
                    Current = Phase.Cooldown;
                    timer = 0f;
                    return true;
                }
            }
            else if (Current == Phase.Cooldown)
            {
                timer += dt;
                if (timer >= cooldown)
                {
                    Current = Phase.Ready;
                    timer = 0f;
                }
            }
            return false;
        }
    }
}
