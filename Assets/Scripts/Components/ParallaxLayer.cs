using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Follows the camera by a fraction of its movement so background layers both
    /// parallax against the foreground and never leave frame during a tall climb.
    /// factor 0 = world-fixed, 1 = locked to the camera.
    public class ParallaxLayer : MonoBehaviour
    {
        public Transform cam;
        [Range(0f, 1f)] public float factor = 0.5f;

        Vector3 basePos;
        Vector3 camStart;

        void Start()
        {
            if (cam == null && Camera.main != null) cam = Camera.main.transform;
            basePos = transform.position;
            if (cam != null) camStart = cam.position;
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 d = cam.position - camStart;
            transform.position = new Vector3(basePos.x + d.x * factor, basePos.y + d.y * factor, basePos.z);
        }
    }
}
