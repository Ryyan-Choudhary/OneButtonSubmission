using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Minimal IMGUI HUD: shell count, reload bar, and an optional win banner.
    public class HudController : MonoBehaviour
    {
        public AmmoSystemBehaviour ammo;
        public GunController gun;
        public bool ShowWin;

        void OnGUI()
        {
            GUI.skin.label.fontSize = 22;

            if (ammo != null && ammo.System != null)
                GUI.Label(new Rect(20, 20, 300, 30),
                    $"SHELLS  {ammo.System.Current}/{ammo.System.Max}");

            if (gun != null)
            {
                GUI.Box(new Rect(20, 55, 200f, 18f), GUIContent.none);
                GUI.Box(new Rect(20, 55, 200f * Mathf.Clamp01(gun.ReloadProgress), 18f),
                    GUIContent.none);
            }

            if (ShowWin)
            {
                var style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 48,
                    alignment = TextAnchor.MiddleCenter
                };
                GUI.Label(new Rect(0, Screen.height / 2f - 40f, Screen.width, 80f),
                    "SUMMIT REACHED", style);
            }
        }
    }
}
