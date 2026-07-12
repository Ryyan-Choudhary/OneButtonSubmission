using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Tracks the single win condition.
    public class GameManager : MonoBehaviour
    {
        public bool HasWon { get; private set; }
        public event System.Action OnWin;

        public void TriggerWin()
        {
            if (HasWon) return;
            HasWon = true;
            OnWin?.Invoke();
        }

        public void ResetWin() => HasWon = false;
    }
}
