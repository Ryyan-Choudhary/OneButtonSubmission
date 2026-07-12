using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Turns a barrel angle + force into a recoil impulse opposite the barrel,
    /// plus an optional flat upward kick so every shot has some hop in it.
    public static class RecoilCalculator
    {
        public static Vector2 Impulse(float barrelAngleDeg, float force, float upBoost = 0f)
        {
            float rad = barrelAngleDeg * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            return -forward * force + Vector2.up * upBoost;
        }

        /// Impulses ADD to velocity, so old momentum can swallow a shot fired
        /// against it. Cancelling (part of) the velocity component that opposes
        /// the new impulse makes every shot's direction read clearly, while
        /// same-direction shots still stack at full strength.
        public static Vector2 CancelOpposing(Vector2 velocity, Vector2 impulse, float cancel01)
        {
            if (impulse.sqrMagnitude < 1e-6f || cancel01 <= 0f) return velocity;
            Vector2 dir = impulse.normalized;
            float along = Vector2.Dot(velocity, dir);
            if (along >= 0f) return velocity;
            return velocity - dir * (along * Mathf.Clamp01(cancel01));
        }
    }
}
