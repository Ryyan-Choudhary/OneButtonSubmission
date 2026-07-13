using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Minimal IMGUI HUD: shells, pump bar, bullet-time meter, level number,
    /// and a centered banner for level transitions.
    public class HudController : MonoBehaviour
    {
        public AmmoSystemBehaviour ammo;
        public GunController gun;
        public int levelNumber;
        public string bannerText;

        void OnGUI()
        {
            // resolution-relative, but never below the readable floor
            int f = Mathf.Max(32, Mathf.RoundToInt(Screen.height * 0.034f));
            float x = 24f, y = 20f;
            float barW = Mathf.Max(280f, Screen.width * 0.16f);
            float barH = f * 0.72f;

            GUI.skin.label.fontSize = f;

            if (ammo != null && ammo.System != null)
            {
                GUI.Label(new Rect(x, y, 500, f + 10),
                    $"SHELLS  {ammo.System.Current}/{ammo.System.Max}");
                y += f + 14f;
            }

            if (gun != null)
            {
                // pump / reload bar
                GUI.Box(new Rect(x, y, barW, barH), GUIContent.none);
                GUI.Box(new Rect(x, y, barW * Mathf.Clamp01(gun.ReloadProgress), barH),
                    GUIContent.none);
                y += barH + 8f;

                // bullet-time meter (green=ready, cyan=aiming, grey=cooling)
                Color btCol = gun.BulletActive ? new Color(0.35f, 0.9f, 1f)
                            : gun.BulletReady ? new Color(0.4f, 1f, 0.6f)
                            : new Color(0.55f, 0.55f, 0.55f);
                GUI.Box(new Rect(x, y, barW, barH), GUIContent.none);
                Color prev = GUI.color;
                GUI.color = btCol;
                GUI.Box(new Rect(x, y, barW * Mathf.Clamp01(gun.BulletMeter), barH), GUIContent.none);
                GUI.color = prev;
            }

            if (levelNumber > 0)
            {
                var right = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = f
                };
                GUI.Label(new Rect(Screen.width - 420, 20, 396, f + 10), $"Level-{levelNumber}", right);
            }

            if (!string.IsNullOrEmpty(bannerText))
            {
                int bf = Mathf.Max(56, Mathf.RoundToInt(Screen.height * 0.062f));
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = bf,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(0, Screen.height / 2f - bf, Screen.width, bf * 2f), bannerText, style);
            }
        }
    }
}
