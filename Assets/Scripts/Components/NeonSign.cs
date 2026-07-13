using UnityEngine;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Components
{
    /// Club-style neon arrow on a dark backboard, pointing down at Bond's
    /// balcony. Breathes like a real sign and occasionally stutters out for a
    /// frame or two, because neon.
    public class NeonSign : MonoBehaviour
    {
        Material neonMat;
        Color baseColor;
        float flickerT;

        public void Build()
        {
            baseColor = new Color(1f, 0.85f, 0.15f); // signage yellow
            neonMat = MaterialFactory.Emissive(baseColor, baseColor, 2.0f, 0.4f);
            var boardMat = MaterialFactory.Lit(new Color(0.05f, 0.05f, 0.08f), 0.3f);

            Piece(new Vector3(0f, -0.1f, 0.3f), new Vector3(4.0f, 6.4f, 0.2f), Quaternion.identity, boardMat, "Board");

            // thick FILLED arrow pointing along local -Y: chunky shaft +
            // a solid triangle head built from overlapping tapered slats
            Piece(new Vector3(0f, 1.15f, 0f), new Vector3(1.2f, 2.9f, 0.34f), Quaternion.identity, neonMat, "Shaft");
            for (int i = 0; i < 8; i++)
            {
                float w = 3.1f - i * 0.37f;
                Piece(new Vector3(0f, -0.45f - i * 0.26f, 0f), new Vector3(w, 0.3f, 0.34f),
                    Quaternion.identity, neonMat, $"Head{i}");
            }
        }

        void Piece(Vector3 lpos, Vector3 scale, Quaternion lrot, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            go.transform.localRotation = lrot;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void Update()
        {
            float intensity;
            if (flickerT > 0f)
            {
                flickerT -= Time.deltaTime;
                intensity = 0.15f; // neon stutter
            }
            else
            {
                intensity = 1.7f + 0.5f * Mathf.Sin(Time.time * 2.4f);
                if (Random.value < 0.008f) flickerT = Random.Range(0.04f, 0.13f);
            }
            neonMat.SetColor("_EmissionColor", baseColor * intensity);
        }
    }
}
