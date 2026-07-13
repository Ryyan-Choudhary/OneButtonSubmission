using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Bullet-time camera language: the FOV tightens onto the gun and a soft
    /// dark vignette closes in from the corners. There is no post-processing
    /// stack in this project, so the "defocused edges" are faked with two
    /// layered radial-falloff draws at slightly different scales. Runs on
    /// unscaled time so the transition isn't slowed by the very slow-mo it
    /// is presenting.
    public class SlowMoCameraFx : MonoBehaviour
    {
        public GunController gun; // reassigned by the bootstrap each level
        public float zoomFov = 47f;

        Camera cam;
        float baseFov;
        float strength; // 0 = normal view, 1 = full slow-mo look
        Texture2D vignetteTex;

        void Awake()
        {
            cam = GetComponent<Camera>();
            baseFov = cam.fieldOfView;
            vignetteTex = MakeVignette(256);
        }

        void OnDestroy()
        {
            if (vignetteTex != null) Destroy(vignetteTex);
        }

        void Update()
        {
            bool active = gun != null && gun.BulletActive;
            // snappy in, slightly gentler out
            float rate = active ? 7f : 5f;
            strength = Mathf.MoveTowards(strength, active ? 1f : 0f,
                rate * Time.unscaledDeltaTime);

            float t = strength * strength * (3f - 2f * strength); // smoothstep
            cam.fieldOfView = Mathf.Lerp(baseFov, zoomFov, t);
        }

        void OnGUI()
        {
            if (strength <= 0.01f) return;
            GUI.depth = 100; // behind the HUD's own OnGUI (depth 0)

            Color prev = GUI.color;
            var full = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.color = new Color(1f, 1f, 1f, 0.9f * strength);
            GUI.DrawTexture(full, vignetteTex, ScaleMode.StretchToFill);
            // second, slightly oversized pass softens the band's inner edge
            var wide = new Rect(-Screen.width * 0.06f, -Screen.height * 0.06f,
                Screen.width * 1.12f, Screen.height * 1.12f);
            GUI.color = new Color(1f, 1f, 1f, 0.5f * strength);
            GUI.DrawTexture(wide, vignetteTex, ScaleMode.StretchToFill);
            GUI.color = prev;
        }

        /// Radial falloff: clear center, dark blue-black corners.
        static Texture2D MakeVignette(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var tint = new Color(0.03f, 0.04f, 0.08f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = x / (size - 1f) * 2f - 1f;
                    float ny = y / (size - 1f) * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1.35f, d));
                    tex.SetPixel(x, y, new Color(tint.r, tint.g, tint.b, a * 0.95f));
                }
            }
            tex.Apply();
            return tex;
        }
    }
}
