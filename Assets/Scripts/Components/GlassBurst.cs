using UnityEngine;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Components
{
    /// One-shot debris burst: shards spray out, tumble under gravity, shrink,
    /// and vanish, with a bright expanding flash at the origin for punch.
    /// Defaults to glass + facade chunks; colors/count are overridable (the
    /// suitcase uses leather + metal). Pure visuals — no colliders.
    public class GlassBurst : MonoBehaviour
    {
        class Shard
        {
            public Transform t;
            public Vector3 vel;
            public Vector3 spin;
            public Vector3 baseScale;
        }

        const float Life = 1.6f;
        const float FlashLife = 0.28f;
        const float Gravity = -16f;

        Shard[] shards;
        Transform flash;
        float age;

        /// dir = the direction the debris flies (unit-ish).
        public static void Spawn(Vector3 pos, Vector3 dir, int seed)
            => Spawn(pos, dir, seed,
                new Color(0.55f, 0.80f, 0.95f), new Color(0.16f, 0.16f, 0.22f), 26);

        public static void Spawn(Vector3 pos, Vector3 dir, int seed,
            Color primary, Color secondary, int count)
        {
            var go = new GameObject("GlassBurst");
            go.transform.position = pos;
            go.AddComponent<GlassBurst>().Build(dir, seed, primary, secondary, count);
        }

        void Build(Vector3 dir, int seed, Color primary, Color secondary, int count)
        {
            var rng = new System.Random(seed);
            var primaryMat = MaterialFactory.Emissive(primary, primary, 1.4f, 0.8f);
            var secondaryMat = MaterialFactory.Lit(secondary, 0.25f);
            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

            shards = new Shard[count];
            int primaries = Mathf.RoundToInt(count * 0.7f);
            for (int i = 0; i < count; i++)
            {
                bool prim = i < primaries;
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = prim ? "Shard" : "Chunk";
                Destroy(cube.GetComponent<Collider>());
                cube.GetComponent<MeshRenderer>().sharedMaterial = prim ? primaryMat : secondaryMat;
                cube.transform.SetParent(transform, false);

                Vector3 scale;
                if (prim && i % 4 == 0)
                    scale = new Vector3(0.1f, R(0.8f, 1.5f), 0.05f); // long spears
                else if (prim)
                {
                    float s = R(0.18f, 0.42f);
                    scale = new Vector3(s, s * R(0.9f, 1.5f), 0.06f); // flat panes
                }
                else
                    scale = Vector3.one * R(0.3f, 0.55f);

                cube.transform.localScale = scale;
                cube.transform.localRotation = Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360));

                shards[i] = new Shard
                {
                    t = cube.transform,
                    baseScale = scale,
                    vel = dir * R(5f, 20f) + new Vector3(0f, R(1.5f, 9f), R(-2f, 2f)),
                    spin = new Vector3(R(-540, 540), R(-540, 540), R(-540, 540)),
                };
            }

            // the pop: a bright sphere that balloons and dies fast
            var f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f.name = "Flash";
            Destroy(f.GetComponent<Collider>());
            f.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Emissive(primary, primary, 2.6f);
            f.transform.SetParent(transform, false);
            f.transform.localScale = Vector3.one * 0.8f;
            flash = f.transform;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            if (flash != null)
            {
                float ft = age / FlashLife;
                if (ft >= 1f) Destroy(flash.gameObject);
                else flash.localScale = Vector3.one * Mathf.Lerp(0.8f, 3.2f, ft * ft);
            }

            float fade = 1f - Mathf.Clamp01((age - Life * 0.55f) / (Life * 0.45f));
            foreach (var s in shards)
            {
                s.vel.y += Gravity * Time.deltaTime;
                s.t.position += s.vel * Time.deltaTime;
                s.t.Rotate(s.spin * Time.deltaTime, Space.Self);
                s.t.localScale = s.baseScale * fade;
            }
        }
    }
}
