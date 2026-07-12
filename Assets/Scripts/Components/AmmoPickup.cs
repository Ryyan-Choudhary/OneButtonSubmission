using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// A trigger volume that tops up shells when the player touches it, then disappears.
    public class AmmoPickup : MonoBehaviour
    {
        public int amount = 3;

        void OnTriggerEnter(Collider other)
        {
            var ammo = other.GetComponentInParent<AmmoSystemBehaviour>();
            if (ammo == null) return;
            ammo.System.Refill(amount);
            gameObject.SetActive(false);
        }
    }
}
