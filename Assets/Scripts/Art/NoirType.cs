using UnityEngine;

namespace OneButtonSubmission.Art
{
    /// The HUD's film-noir type: a tall condensed poster face pulled from
    /// the OS (Impact and friends), falling back silently to the engine's
    /// default (kept bold) where OS fonts aren't available, e.g. WebGL.
    /// Labels are drawn twice — a hard black offset under the fill —
    /// because noir is nothing without its shadows.
    public static class NoirType
    {
        static Font font;
        static bool searched;

        public static Font Font
        {
            get
            {
                if (!searched)
                {
                    searched = true;
#if !UNITY_WEBGL || UNITY_EDITOR
                    // WebGL players have no OS fonts at all; elsewhere a
                    // missing face can still come back as a non-null Font
                    // that rasterizes nothing, so only trust one that can
                    // actually produce a glyph.
                    foreach (var name in new[] { "Impact", "Haettenschweiler", "Arial Narrow" })
                    {
                        var candidate = Font.CreateDynamicFontFromOSFont(name, 32);
                        if (candidate != null && candidate.HasCharacter('A'))
                        {
                            font = candidate;
                            break;
                        }
                    }
#endif
                }
                return font;
            }
        }

        public static GUIStyle Style(int size, Color color,
            TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                alignment = anchor,
                // Impact carries its own weight; only the fallback needs bold
                fontStyle = Font != null ? FontStyle.Normal : FontStyle.Bold,
            };
            if (Font != null) s.font = Font;
            s.normal.textColor = color;
            return s;
        }

        public static void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            var shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
            float off = Mathf.Max(2f, style.fontSize * 0.06f);
            GUI.Label(new Rect(r.x + off, r.y + off, r.width, r.height), text, shadow);
            GUI.Label(r, text, style);
        }
    }
}
