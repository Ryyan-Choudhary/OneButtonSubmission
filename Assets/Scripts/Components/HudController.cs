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
            GUI.skin.label.fontSize = 22;

            if (ammo != null && ammo.System != null)
                GUI.Label(new Rect(20, 20, 320, 30),
                    $"SHELLS  {ammo.System.Current}/{ammo.System.Max}");

            if (gun != null)
            {
                // pump / reload bar
                GUI.Box(new Rect(20, 55, 200f, 16f), GUIContent.none);
                GUI.Box(new Rect(20, 55, 200f * Mathf.Clamp01(gun.ReloadProgress), 16f),
                    GUIContent.none);

                // bullet-time meter (green=ready, cyan=aiming, grey=cooling)
                Color btCol = gun.BulletActive ? new Color(0.35f, 0.9f, 1f)
                            : gun.BulletReady ? new Color(0.4f, 1f, 0.6f)
                            : new Color(0.55f, 0.55f, 0.55f);
                GUI.Box(new Rect(20, 76, 200f, 16f), GUIContent.none);
                Color prev = GUI.color;
                GUI.color = btCol;
                GUI.Box(new Rect(20, 76, 200f * Mathf.Clamp01(gun.BulletMeter), 16f), GUIContent.none);
                GUI.color = prev;
            }

            if (levelNumber > 0)
            {
                var right = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = 22
                };
                GUI.Label(new Rect(Screen.width - 200, 20, 180, 30), $"Level-{levelNumber}", right);
            }

            if (!string.IsNullOrEmpty(bannerText))
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 46,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(0, Screen.height / 2f - 40f, Screen.width, 80f), bannerText, style);
            }
        }
    }
}
