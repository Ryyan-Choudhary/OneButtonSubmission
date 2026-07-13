using UnityEngine;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Components
{
    /// The black helicopter hovering over the final climb: chunky fuselage,
    /// spinning main and tail rotors, blinking tail light, and a flat rear
    /// deck (solid — the gun can land on it) where the boss stands and Bond's
    /// cage is bolted. The whole rig bobs gently on the hover.
    public class FinaleHelicopter : MonoBehaviour
    {
        Transform mainRotor;
        Transform tailRotor;
        MeshRenderer tailLight;
        Material lightOn, lightOff;
        Vector3 basePos;
        float t;

        /// Where the boss stands on the rear deck (world space, at build time).
        public Vector3 BossAnchor => transform.position + new Vector3(1.4f, 0.95f, 0f);
        /// Where the cage sits, further back on the deck.
        public Vector3 CageAnchor => transform.position + new Vector3(5.4f, 0.95f, 0f);

        public void Build()
        {
            basePos = transform.position;
            var black = MaterialFactory.Lit(new Color(0.06f, 0.06f, 0.08f), 0.5f, 0.4f);
            var glass = MaterialFactory.Lit(new Color(0.10f, 0.16f, 0.24f), 0.9f, 0.6f);
            lightOn = MaterialFactory.Emissive(new Color(1f, 0.15f, 0.10f), new Color(1f, 0.15f, 0.10f), 2.4f);
            lightOff = MaterialFactory.Lit(new Color(0.25f, 0.06f, 0.05f), 0.4f);

            Piece(new Vector3(0f, 0.2f, 0f), new Vector3(5.5f, 2.4f, 2.4f), black, "Fuselage", false);
            Piece(new Vector3(-2.9f, 0.4f, 0f), new Vector3(1.4f, 1.3f, 2.0f), glass, "Cockpit", false);
            Piece(new Vector3(4.3f, 0.5f, 0f), new Vector3(4.4f, 0.6f, 0.6f), black, "TailBoom", false);
            Piece(new Vector3(6.4f, 1.3f, 0f), new Vector3(0.3f, 1.5f, 0.3f), black, "TailFin", false);

            // the rear deck the boss stands on — the only solid part
            Piece(new Vector3(3.6f, 0.75f, 0f), new Vector3(6.6f, 0.35f, 3.4f), black, "RearDeck", true);

            // skids
            Piece(new Vector3(-0.6f, -1.45f, 0.9f), new Vector3(4.4f, 0.12f, 0.16f), black, "SkidF", false);
            Piece(new Vector3(-0.6f, -1.45f, -0.9f), new Vector3(4.4f, 0.12f, 0.16f), black, "SkidB", false);
            Piece(new Vector3(-1.6f, -0.9f, 0.9f), new Vector3(0.12f, 1.0f, 0.12f), black, "StrutFL", false);
            Piece(new Vector3(0.6f, -0.9f, 0.9f), new Vector3(0.12f, 1.0f, 0.12f), black, "StrutFR", false);
            Piece(new Vector3(-1.6f, -0.9f, -0.9f), new Vector3(0.12f, 1.0f, 0.12f), black, "StrutBL", false);
            Piece(new Vector3(0.6f, -0.9f, -0.9f), new Vector3(0.12f, 1.0f, 0.12f), black, "StrutBR", false);

            // main rotor: mast + two crossed blades spinning about Y
            Piece(new Vector3(0f, 1.55f, 0f), new Vector3(0.2f, 0.5f, 0.2f), black, "Mast", false);
            mainRotor = new GameObject("MainRotor").transform;
            mainRotor.SetParent(transform, false);
            mainRotor.localPosition = new Vector3(0f, 1.9f, 0f);
            var bladeA = Piece(Vector3.zero, new Vector3(11.5f, 0.09f, 0.4f), black, "BladeA", false);
            bladeA.transform.SetParent(mainRotor, false);
            var bladeB = Piece(Vector3.zero, new Vector3(11.5f, 0.09f, 0.4f), black, "BladeB", false);
            bladeB.transform.SetParent(mainRotor, false);
            bladeB.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            // tail rotor spinning in the play plane
            tailRotor = new GameObject("TailRotor").transform;
            tailRotor.SetParent(transform, false);
            tailRotor.localPosition = new Vector3(6.5f, 1.6f, 0.4f);
            var tBlade = Piece(Vector3.zero, new Vector3(1.7f, 0.12f, 0.07f), black, "TailBlade", false);
            tBlade.transform.SetParent(tailRotor, false);

            var lightGo = Piece(new Vector3(6.5f, 2.1f, 0f), new Vector3(0.22f, 0.22f, 0.22f), lightOn, "TailLight", false);
            tailLight = lightGo.GetComponent<MeshRenderer>();
        }

        GameObject Piece(Vector3 lpos, Vector3 scale, Material mat, string name, bool solid)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!solid) Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        void Update()
        {
            t += Time.deltaTime;
            transform.position = basePos + Vector3.up * (Mathf.Sin(t * 0.9f) * 0.45f);
            if (mainRotor != null) mainRotor.Rotate(0f, 1100f * Time.deltaTime, 0f, Space.Self);
            if (tailRotor != null) tailRotor.Rotate(0f, 0f, 1600f * Time.deltaTime, Space.Self);
            if (tailLight != null)
                tailLight.sharedMaterial = Mathf.FloorToInt(t * 2f) % 2 == 0 ? lightOn : lightOff;
        }
    }
}
