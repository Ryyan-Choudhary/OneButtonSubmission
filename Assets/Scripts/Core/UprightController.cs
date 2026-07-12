using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// PD controller that keeps the body upright but can be overwhelmed on hard hits.
    public static class UprightController
    {
        public static float ComputeTorque(float currentAngleDeg, float angularVelDeg,
            float kp, float kd, float maxTorque)
        {
            float lean = Mathf.DeltaAngle(0f, currentAngleDeg); // normalize to [-180,180]
            float raw = -(kp * lean + kd * angularVelDeg);
            return Mathf.Clamp(raw, -maxTorque, maxTorque);
        }
    }
}
