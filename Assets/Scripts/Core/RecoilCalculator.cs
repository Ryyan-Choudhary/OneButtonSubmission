using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Turns a barrel angle + force into a recoil impulse opposite the barrel.
    public static class RecoilCalculator
    {
        public static Vector2 Impulse(float barrelAngleDeg, float force)
        {
            float rad = barrelAngleDeg * Mathf.Deg2Rad;
            Vector2 forward = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            return -forward * force;
        }
    }
}
