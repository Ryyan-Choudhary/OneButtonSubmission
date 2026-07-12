using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// Kicks the visible gun parts backward (along -X of the pivot) on each shot,
    /// then eases them home. Purely cosmetic — physics recoil lives in PlayerBody.
    public class GunRecoilAnim : MonoBehaviour
    {
        public GunController gun;
        public float kickDistance = 0.25f;
        public float returnSpeed = 6f;

        Vector3 basePos;
        float kick; // 1 right after firing, decays to 0

        void Start()
        {
            basePos = transform.localPosition;
            if (gun != null) gun.OnFired += OnFired;
        }

        void OnDestroy()
        {
            if (gun != null) gun.OnFired -= OnFired;
        }

        void OnFired() => kick = 1f;

        void Update()
        {
            kick = Mathf.MoveTowards(kick, 0f, returnSpeed * Time.deltaTime);
            transform.localPosition = basePos + new Vector3(-kickDistance * kick, 0f, 0f);
        }
    }
}
