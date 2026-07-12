using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Minimal IMGUI HUD: shell count, reload bar, and an optional win banner.
    public class HudController : MonoBehaviour
    {
        public AmmoSystemBehaviour ammo;
        public GunController gun;
        public bool ShowWin;
        public GameManager gameManager;

        void OnGUI()
        {
            GUI.skin.label.fontSize = 22;

            if (ammo != null && ammo.System != null)
                GUI.Label(new Rect(20, 20, 300, 30),
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

                if (!string.IsNullOrEmpty(gun.GunName))
                    GUI.Label(new Rect(20, 98, 300, 28), gun.GunName);
            }

            if (ShowWin || (gameManager != null && gameManager.HasWon))
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
