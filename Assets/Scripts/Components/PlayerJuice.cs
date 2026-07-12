using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Squash-and-stretch juice on a separate visual child, so the physics
    /// collider is never deformed. Stretches tall on launch (fire), squashes
    /// flat on hard landings.
    public class PlayerJuice : MonoBehaviour
    {
        public Transform visual;
        public GunController gun;

        public float squashAmount = 0.25f;   // how flat on land
        public float stretchAmount = 0.20f;  // how tall on launch
        public float recover = 5f;           // how fast it returns to normal
        public float landThreshold = 3f;     // impact speed that triggers squash

        Vector3 baseScale;
        float s; // +1 = full squash, -1 = full stretch, 0 = rest

        void Start()
        {
            if (visual != null) baseScale = visual.localScale;
            if (gun != null) gun.OnFired += OnFired;
        }

        void OnDestroy()
        {
            if (gun != null) gun.OnFired -= OnFired;
        }

        void OnFired() => s = -1f; // stretch on launch

        void OnCollisionEnter(Collision c)
        {
            float impact = c.relativeVelocity.magnitude;
            if (impact > landThreshold)
                s = Mathf.Clamp01(impact / 12f); // squash on hard land
        }

        void Update()
        {
            s = Mathf.MoveTowards(s, 0f, recover * Time.deltaTime);
            if (visual == null) return;

            float m = s >= 0f ? squashAmount : stretchAmount;
            float sy = 1f - s * m;          // s>0 shorter, s<0 taller
            float sxz = 1f + s * m * 0.6f;  // s>0 wider,   s<0 thinner
            visual.localScale = new Vector3(baseScale.x * sxz, baseScale.y * sy, baseScale.z * sxz);
        }
    }
}
