using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// Scene-facing wrapper around the pure AmmoSystem.
    public class AmmoSystemBehaviour : MonoBehaviour
    {
        public int maxShells = 6;
        public int startShells = 6;

        public AmmoSystem System { get; private set; }

        void Awake()
        {
            System = new AmmoSystem(maxShells, startShells);
        }
    }
}
