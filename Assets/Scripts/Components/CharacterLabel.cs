using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Football / sports-game style floating name label.
    /// Just white bold text with a drop shadow and a solid downward-pointing
    /// triangle underneath — no background box.
    public class CharacterLabel : MonoBehaviour
    {
        public string labelText = "James Bond";

        /// World-space vertical offset above the transform's position.
        public float worldYOffset = 5.2f;

        /// How quickly the label fades in (seconds).
        public float fadeInDuration = 0.5f;

        Camera cam;
        float spawnTime;
        Texture2D whiteTex;

        void Awake()
        {
            spawnTime = Time.unscaledTime;
            whiteTex = MakeSolid(4, Color.white);
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }

        void Start()
        {
            cam = Camera.main;
        }

        void OnGUI()
        {
            if (cam == null)
            {
                cam = Camera.main;
                if (cam == null) return;
            }

            float alpha = Mathf.Clamp01((Time.unscaledTime - spawnTime) / Mathf.Max(fadeInDuration, 0.01f));
            if (alpha <= 0f) return;

            // World anchor point above the character
            Vector3 worldPos = transform.position + Vector3.up * worldYOffset;
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            if (screen.z < 0f) return;

            // Convert to GUI space (y-flip)
            float sx = screen.x;
            float sy = Screen.height - screen.y;

            // Scale font with screen height, clamp for readability
            int fontSize = Mathf.Max(26, Mathf.RoundToInt(Screen.height * 0.036f));

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                normal = { textColor = Color.white }
            };

            GUIContent content = new GUIContent(labelText);
            Vector2 textSize = style.CalcSize(content);
            float tw = textSize.x;
            float th = textSize.y;

            // Text is centered at sx, with its bottom at sy
            float tx = sx - tw * 0.5f;
            float ty = sy - th;

            var shadowStyle = new GUIStyle(style)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.7f * alpha) }
            };

            var prev = GUI.color;

            // Drop shadow (1-2px offset)
            GUI.color = Color.white; // must be white for alpha to work correctly
            GUI.Label(new Rect(tx + 2f, ty + 2f, tw, th), content, shadowStyle);

            // Main text
            var mainStyle = new GUIStyle(style)
            {
                normal = { textColor = new Color(1f, 1f, 1f, alpha) }
            };
            GUI.Label(new Rect(tx, ty, tw, th), content, mainStyle);

            // Downward-pointing solid triangle drawn as a row of horizontal
            // strips, each one pixel narrower on each side — a clean V shape.
            float triW = fontSize * 1.1f;
            float triH = fontSize * 0.65f;
            float triX = sx - triW * 0.5f;
            float triY = sy;          // sits right below the text bottom
            int rows = Mathf.Max(1, Mathf.RoundToInt(triH));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            for (int row = 0; row < rows; row++)
            {
                float frac = (float)row / rows;          // 0 = top (widest), 1 = bottom (point)
                float rowW = triW * (1f - frac);
                float rowX = triX + (triW - rowW) * 0.5f;
                GUI.DrawTexture(new Rect(rowX, triY + row, rowW, 1f), whiteTex);
            }

            GUI.color = prev;
        }

        static Texture2D MakeSolid(int size, Color c)
        {
            var tex = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
