using UnityEngine;

namespace OneButtonSubmission.Core
{
    /// Finite shotgun shells. Pure logic, no Unity scene dependency.
    public class AmmoSystem
    {
        public int Max { get; }
        public int Current { get; private set; }
        public bool IsEmpty => Current <= 0;

        public AmmoSystem(int max, int start)
        {
            Max = Mathf.Max(0, max);
            Current = Mathf.Clamp(start, 0, Max);
        }

        public bool TryConsume()
        {
            if (Current <= 0) return false;
            Current--;
            return true;
        }

        public void Refill(int amount)
        {
            if (amount <= 0) return;
            Current = Mathf.Min(Current + amount, Max);
        }
    }
}
