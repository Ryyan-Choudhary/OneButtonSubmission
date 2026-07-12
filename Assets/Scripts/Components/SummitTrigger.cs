using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// The flag at the top: entering it wins the game.
    public class SummitTrigger : MonoBehaviour
    {
        public GameManager gameManager;

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerBody>() == null) return;
            if (gameManager != null) gameManager.TriggerWin();
        }
    }
}
