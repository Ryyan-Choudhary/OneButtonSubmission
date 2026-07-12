using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Pulses a renderer's emission so the summit reads as the goal beacon.
    /// Uses a MaterialPropertyBlock so it never mutates the shared material.
    public class SummitPulse : MonoBehaviour
    {
        public Renderer target;
        public Color emission = Color.white;
        public float baseIntensity = 1.2f;
        public float pulse = 0.6f;
        public float speed = 2.5f;

        MaterialPropertyBlock mpb;

        void Start()
        {
            mpb = new MaterialPropertyBlock();
            if (target == null) target = GetComponent<Renderer>();
        }

        void Update()
        {
            if (target == null) return;
            float i = Mathf.Max(0f, baseIntensity + Mathf.Sin(Time.time * speed) * pulse);
            target.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", emission * i);
            target.SetPropertyBlock(mpb);
        }
    }
}
