using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Advances the gun's sweep angle at a constant rate, wrapped to [0,360).
    public static class GunRotator
    {
        public static float Advance(float angleDeg, float speedDegPerSec, float dt)
            => Mathf.Repeat(angleDeg + speedDegPerSec * dt, 360f);
    }
}
