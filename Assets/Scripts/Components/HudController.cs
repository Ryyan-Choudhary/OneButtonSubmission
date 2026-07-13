using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// Minimal IMGUI HUD in film-noir type: a magazine of brass cartridges
    /// (fading to dark silhouettes as they're spent), a centered amber
    /// BULLET TIME charge that only appears while slow-mo runs, the level
    /// number over a blood-red kill tally, and a centered banner for level
    /// transitions.
    public class HudController : MonoBehaviour
    {
        public AmmoSystemBehaviour ammo;
        public GunController gun;
        public int levelNumber;
        public string bannerText;

        // Dark-but-not-flat-black track, so the fill actually has something to
        // contrast against.
        static readonly Color TrackColor = new Color(0.08f, 0.09f, 0.13f, 0.85f);
        // A spent shell: multiplies the baked brass colors down to a dark,
        // desaturated silhouette that still reads at a glance.
        static readonly Color SpentColor = new Color(0.30f, 0.31f, 0.38f, 0.65f);
        // Noir ink: warm ivory for type, arterial red for the kill tally.
        static readonly Color Ivory = new Color(0.93f, 0.90f, 0.82f);
        static readonly Color BloodRed = new Color(0.78f, 0.10f, 0.10f);

        Texture2D barTex;
        Texture2D bulletTex;

        void Awake()
        {
            // GUI.Box draws from the skin's box texture, which can render solid
            // black in standalone builds (missing built-in resources) — and
            // tinting a black texture with GUI.color still gives you black.
            // Drawing from our own solid white texture avoids that entirely,
            // same trick used for the flash/fade overlays elsewhere in the game.
            barTex = new Texture2D(1, 1);
            barTex.SetPixel(0, 0, Color.white);
            barTex.Apply();
            bulletTex = MakeBulletTex();
        }

        void OnDestroy()
        {
            if (barTex != null) Destroy(barTex);
            if (bulletTex != null) Destroy(bulletTex);
        }

        void OnGUI()
        {
            // resolution-relative, but never below the readable floor
            int f = Mathf.Max(32, Mathf.RoundToInt(Screen.height * 0.034f));
            float x = 24f, y = 20f;

            GUI.skin.label.fontSize = f;

            if (ammo != null && ammo.System != null)
            {
                // the magazine: brass cartridges on a dark plate, spent ones
                // fading to dark silhouettes as they're fired
                float bw = f * 0.44f, bh = f * 1.05f, gap = f * 0.16f;
                int max = ammo.System.Max;
                Color prev = GUI.color;
                GUI.color = TrackColor;
                GUI.DrawTexture(new Rect(x - 10f, y - 8f,
                    max * (bw + gap) - gap + 20f, bh + 16f), barTex);
                for (int i = 0; i < max; i++)
                {
                    GUI.color = i < ammo.System.Current ? Color.white : SpentColor;
                    GUI.DrawTexture(new Rect(x + i * (bw + gap), y, bw, bh),
                        bulletTex, ScaleMode.StretchToFill);
                }
                GUI.color = prev;
            }

            // bullet time: a cinematic amber charge low on the screen, shown
            // only while slow-mo runs — it collapses toward its center as
            // the meter drains, bright caps chasing each other inward
            if (gun != null && gun.BulletActive)
            {
                float w = Mathf.Max(320f, Screen.width * 0.26f);
                float cx = Screen.width * 0.5f;
                float ty = Screen.height * 0.80f;
                var amber = new Color(1f, 0.72f, 0.25f);

                int lf = Mathf.Max(18, Mathf.RoundToInt(Screen.height * 0.021f));
                NoirType.ShadowLabel(new Rect(cx - w * 0.5f, ty - lf - 14f, w, lf + 8f),
                    "B U L L E T   T I M E",
                    NoirType.Style(lf, amber, TextAnchor.MiddleCenter));

                Color prev = GUI.color;
                const float trackH = 10f;
                GUI.color = TrackColor;
                GUI.DrawTexture(new Rect(cx - w * 0.5f, ty, w, trackH), barTex);
                float fw = w * Mathf.Clamp01(gun.BulletMeter);
                GUI.color = amber;
                GUI.DrawTexture(new Rect(cx - fw * 0.5f, ty, fw, trackH), barTex);
                GUI.color = new Color(1f, 0.92f, 0.62f);
                GUI.DrawTexture(new Rect(cx - fw * 0.5f - 2f, ty - 2f, 3f, trackH + 4f), barTex);
                GUI.DrawTexture(new Rect(cx + fw * 0.5f - 1f, ty - 2f, 3f, trackH + 4f), barTex);
                GUI.color = prev;
            }

            if (levelNumber > 0)
            {
                NoirType.ShadowLabel(new Rect(Screen.width - 420f, 20f, 396f, f + 10f),
                    $"LEVEL {levelNumber}", NoirType.Style(f, Ivory, TextAnchor.UpperRight));
            }

            DrawKillTally(f);

            if (!string.IsNullOrEmpty(bannerText))
            {
                int bf = Mathf.Max(56, Mathf.RoundToInt(Screen.height * 0.062f));
                NoirType.ShadowLabel(new Rect(0f, Screen.height / 2f - bf, Screen.width, bf * 2f),
                    bannerText, NoirType.Style(bf, Ivory, TextAnchor.MiddleCenter));
            }
        }

        /// The body count, scratched under the level number like marks on a
        /// cell wall: blood-red strokes in groups of five (four uprights,
        /// then a slash across), right-aligned with a small caption carrying
        /// the exact number. The wall art caps at six groups; the caption
        /// keeps counting.
        void DrawKillTally(int f)
        {
            int kills = GameStats.Kills;
            if (kills <= 0) return;

            float rightEdge = Screen.width - 28f;
            float y = 20f + f + 18f;

            int cf = Mathf.Max(20, Mathf.RoundToInt(f * 0.55f));
            NoirType.ShadowLabel(new Rect(rightEdge - 396f, y, 396f, cf + 6f),
                $"KILLS  {kills}",
                NoirType.Style(cf, new Color(Ivory.r, Ivory.g, Ivory.b, 0.85f),
                    TextAnchor.UpperRight));
            y += cf + 12f;

            float sh = f * 0.62f;    // stroke height
            const float sw = 3.5f;   // stroke width
            float sgap = sh * 0.30f; // spacing inside a group
            float ggap = sh * 0.55f; // spacing between groups
            const int maxGroups = 6;
            int shown = Mathf.Min(kills, maxGroups * 5);
            int groups = (shown + 4) / 5;
            float groupW = 3f * sgap + sw;
            float x0 = rightEdge - (groups * groupW + (groups - 1) * ggap);

            Color prev = GUI.color;
            for (int i = 0; i < shown; i++)
            {
                int g = i / 5, k = i % 5;
                float gx = x0 + g * (groupW + ggap);
                if (k < 4)
                {
                    DrawStroke(new Rect(gx + k * sgap, y, sw, sh));
                }
                else
                {
                    // the fifth mark slashes across its group
                    Matrix4x4 m = GUI.matrix;
                    GUIUtility.RotateAroundPivot(-62f,
                        new Vector2(gx + groupW * 0.5f, y + sh * 0.5f));
                    DrawStroke(new Rect(gx - sh * 0.18f, y + sh * 0.5f - sw * 0.5f,
                        groupW + sh * 0.36f, sw));
                    GUI.matrix = m;
                }
            }
            GUI.color = prev;
        }

        /// One tally stroke: hard black offset under blood red — same noir
        /// shadow treatment as the type.
        void DrawStroke(Rect r)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), barTex);
            GUI.color = BloodRed;
            GUI.DrawTexture(r, barTex);
        }

        /// A proper two-tone cartridge with its colors baked in: brass casing
        /// with a cylindrical center highlight, copper slug nose, darker
        /// extraction rim at the base. Loaded shells draw untinted; spent
        /// shells multiply down to a dark silhouette via GUI.color.
        static Texture2D MakeBulletTex()
        {
            const int w = 20, h = 44;
            var brass    = new Color(0.80f, 0.58f, 0.20f);
            var brassHi  = new Color(1.00f, 0.82f, 0.42f);
            var copper   = new Color(0.72f, 0.40f, 0.24f);
            var copperHi = new Color(0.94f, 0.60f, 0.36f);
            var rim      = new Color(0.55f, 0.38f, 0.13f);
            var clear    = new Color(0f, 0f, 0f, 0f);

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float cx = (w - 1) * 0.5f;
            float half = w * 0.36f;
            float bodyTop = h * 0.60f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x - cx);
                    float hl = 1f - Mathf.Clamp01(dx / half); // center shine
                    hl *= hl;
                    bool inside;
                    Color c;
                    if (y < 4)
                    {
                        inside = dx <= half + 1.6f;
                        c = Color.Lerp(rim, brass, hl * 0.5f);
                    }
                    else if (y <= bodyTop)
                    {
                        inside = dx <= half;
                        c = Color.Lerp(brass, brassHi, hl);
                    }
                    else
                    {
                        float t = (y - bodyTop) / (h - 1f - bodyTop);
                        inside = dx <= half * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t));
                        c = Color.Lerp(copper, copperHi, hl * (1f - t * 0.6f));
                    }
                    tex.SetPixel(x, y, inside ? c : clear);
                }
            }
            tex.Apply();
            return tex;
        }
    }
}