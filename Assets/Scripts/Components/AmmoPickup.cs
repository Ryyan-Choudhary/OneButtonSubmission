using UnityEngine;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// A trigger volume that tops up shells when the player touches it, then
    /// hides and respawns a few seconds later. Respawning matters: the gun
    /// cannot move at all with zero shells, so a permanently spent route
    /// would softlock the climb.
    public class AmmoPickup : MonoBehaviour
    {
        public int amount = 3;
        public float respawnSeconds = 6f;

        Renderer[] rends; // the crate is built from child pieces
        Collider col;

        void Awake()
        {
            rends = GetComponentsInChildren<Renderer>();
            col = GetComponent<Collider>();
        }

        void OnTriggerEnter(Collider other)
        {
            var ammo = other.GetComponentInParent<AmmoSystemBehaviour>();
            if (ammo == null) return;
            ammo.System.Refill(amount);
            AudioManager.Play(AudioManager.Sfx.Reload);
            SetVisible(false);
            Invoke(nameof(Respawn), respawnSeconds);
        }

        void Respawn() => SetVisible(true);

        void SetVisible(bool on)
        {
            foreach (var r in rends)
                if (r != null) r.enabled = on;
            col.enabled = on;
        }
    }
}
