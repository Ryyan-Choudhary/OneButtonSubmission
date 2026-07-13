using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// A wall-mounted neon sign that becomes a falling physics hazard when
    /// shot: it tears loose, tumbles down the play plane, crushes any enemy
    /// it touches (their trigger volumes fire against its rigidbody), and
    /// smashes the player's gun on a hard direct hit — so dropping one is a
    /// tool AND a risk. Settles as debris, then shrinks away.
    public class HazardSign : MonoBehaviour
    {
        public HudController hud;       // for the retry prompt on a gun kill
        public System.Action onRetry;

        Rigidbody rb;
        bool falling;
        float fallAge;
        Vector3 baseScale;

        public void Build()
        {
            var panelMat = MaterialFactory.Lit(new Color(0.10f, 0.10f, 0.14f), 0.3f);
            var tubeMat = MaterialFactory.Emissive(Palette.Neon, Palette.Neon, 2.2f);
            var strutMat = MaterialFactory.Lit(new Color(0.08f, 0.08f, 0.11f), 0.5f, 0.5f);

            Piece(new Vector3(0f, 0f, 0f), new Vector3(2.8f, 2.0f, 0.4f), panelMat, "Panel");
            // neon border tubes
            Piece(new Vector3(0f, 1.0f, -0.22f), new Vector3(2.9f, 0.10f, 0.10f), tubeMat, "TubeTop");
            Piece(new Vector3(0f, -1.0f, -0.22f), new Vector3(2.9f, 0.10f, 0.10f), tubeMat, "TubeBottom");
            Piece(new Vector3(-1.4f, 0f, -0.22f), new Vector3(0.10f, 2.1f, 0.10f), tubeMat, "TubeLeft");
            Piece(new Vector3(1.4f, 0f, -0.22f), new Vector3(0.10f, 2.1f, 0.10f), tubeMat, "TubeRight");
            // crossed neon slashes — the demolition glyph
            var slashA = Piece(new Vector3(0f, 0f, -0.24f), new Vector3(2.2f, 0.12f, 0.08f), tubeMat, "SlashA");
            slashA.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);
            var slashB = Piece(new Vector3(0f, 0f, -0.24f), new Vector3(2.2f, 0.12f, 0.08f), tubeMat, "SlashB");
            slashB.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            // hanging struts
            Piece(new Vector3(-0.9f, 1.55f, 0f), new Vector3(0.10f, 1.1f, 0.10f), strutMat, "StrutL");
            Piece(new Vector3(0.9f, 1.55f, 0f), new Vector3(0.10f, 1.1f, 0.10f), strutMat, "StrutR");

            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(2.9f, 2.1f, 0.5f);

            baseScale = transform.localScale;
        }

        GameObject Piece(Vector3 lpos, Vector3 lscale, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = lscale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// A bullet (or blast) tears it off the wall.
        public void Drop(Vector3 dir)
        {
            if (falling) return;
            falling = true;
            AudioManager.Play(AudioManager.Sfx.WindowBreak);
            Vector3 pos = transform.position;
            GlassBurst.Spawn(pos, dir, (int)(pos.x * 19f + pos.y * 3f),
                Palette.Neon, new Color(0.10f, 0.10f, 0.14f), 8);

            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 3f;
            // stays in the play plane and tumbles like everything else
            rb.constraints = RigidbodyConstraints.FreezePositionZ
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationY;
            rb.linearVelocity = new Vector3(dir.x * 1.5f, 0.5f, 0f);
            rb.angularVelocity = new Vector3(0f, 0f, dir.x > 0f ? -3f : 3f);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!falling) return;
            var enemyHit = other.GetComponent<EnemyHitBox>();
            if (enemyHit != null && enemyHit.owner != null
                && enemyHit.owner.Kill(Vector3.down))
            {
                GameStats.Kills++; // crushed — an engineered kill for the tally
                return;
            }
            var rival = other.GetComponent<VillainTrigger>();
            if (rival != null && rival.KillVillain(Vector3.down))
                GameStats.Kills++;
        }

        void OnCollisionEnter(Collision c)
        {
            if (!falling) return;
            var body = c.collider.GetComponentInParent<GunBody>();
            if (body != null && c.relativeVelocity.magnitude > 6f)
                GunDeath.TryKill(body, hud, onRetry);
        }

        void Update()
        {
            if (!falling) return;
            fallAge += Time.deltaTime;
            // linger as debris, then shrink out
            if (fallAge > 9f)
            {
                float fade = 1f - Mathf.Clamp01((fallAge - 9f) / 0.8f);
                transform.localScale = baseScale * fade;
                if (fade <= 0f) Destroy(gameObject);
            }
        }
    }
}
