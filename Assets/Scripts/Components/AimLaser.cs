using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Thin red laser sight from the muzzle along the barrel, clipped where it
    /// hits terrain. It just visualizes the gun's physical rotation, so it
    /// doubles as the aim readout while tumbling or in bullet time.
    public class AimLaser : MonoBehaviour
    {
        public Transform gunRoot;
        public Vector3 localOrigin = new Vector3(0.9f, 0.12f, 0f);
        public float maxLength = 8f; // a sight stub, not a searchlight
        public float width = 0.06f;
        public Material material;

        LineRenderer line;

        void Start()
        {
            line = gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = width;
            line.endWidth = width * 0.35f; // tapers so it reads as a beam
            line.material = material;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }

        void LateUpdate()
        {
            if (gunRoot == null || line == null) return;
            Vector3 origin = gunRoot.TransformPoint(localOrigin);
            Vector3 dir = gunRoot.right;
            Vector3 end = Physics.Raycast(origin, dir, out RaycastHit hit, maxLength,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                ? hit.point
                : origin + dir * maxLength;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
        }
    }
}
