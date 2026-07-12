using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Tracks the single win condition.
    public class GameManager : MonoBehaviour
    {
        public bool HasWon { get; private set; }

        public void TriggerWin()
        {
            if (HasWon) return;
            HasWon = true;
            Debug.Log("SUMMIT REACHED — YOU WIN");
        }
    }
}
