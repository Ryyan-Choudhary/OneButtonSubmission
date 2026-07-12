using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Spins and gently bobs a pickup so the glowing shells read as collectible.
    public class PickupSpin : MonoBehaviour
    {
        public float spinSpeed = 90f;
        public float bobHeight = 0.15f;
        public float bobSpeed = 2f;

        Vector3 basePos;
        float phase;

        void Start()
        {
            basePos = transform.position;
            phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            float y = Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
            transform.position = new Vector3(basePos.x, basePos.y + y, basePos.z);
        }
    }
}
