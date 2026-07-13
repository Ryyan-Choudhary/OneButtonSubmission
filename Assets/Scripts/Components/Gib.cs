using UnityEngine;

namespace OneButtonSubmission.Components
{
    /// One dismembered chunk of a shot rival: a world-space physics copy of
    /// a rig's visual box, blown downrange, tumbling off the shelf and
    /// shrinking away at the end of a short life. Copies are used instead of
    /// re-parenting because the rig mirrors itself with a negative X scale,
    /// which colliders reject.
    public class Gib : MonoBehaviour
    {
        const float Life = 2.6f;

        Vector3 baseScale;
        float age;

        public static void Spawn(MeshRenderer source, Vector3 dir, System.Random rng, bool bleeds)
        {
            var src = source.transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Gib";
            Vector3 s = src.lossyScale;
            go.transform.localScale = new Vector3(
                Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
            go.transform.SetPositionAndRotation(src.position, src.rotation);
            go.GetComponent<MeshRenderer>().sharedMaterial = source.sharedMaterial;

            float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.25f;
            rb.linearVelocity = dir.normalized * R(7f, 14f)
                + new Vector3(R(-1.5f, 1.5f), R(2f, 6.5f), R(-0.8f, 0.8f));
            rb.maxAngularVelocity = 30f;
            rb.angularVelocity = new Vector3(R(-14f, 14f), R(-14f, 14f), R(-14f, 14f));

            var gib = go.AddComponent<Gib>();
            gib.baseScale = go.transform.localScale;
            if (bleeds) BloodFx.Trail(go.transform);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life)
            {
                Destroy(gameObject);
                return;
            }
            // shrink out over the last third so chunks never just blink away
            float fade = 1f - Mathf.Clamp01((age - Life * 0.65f) / (Life * 0.35f));
            transform.localScale = baseScale * fade;
        }
    }

    /// Blood, two ways: a one-shot spray at the wound plus chunky splatter
    /// cubes, and gravity-heavy droplet trails that arc off the bigger gibs
    /// as they fly. Dark arterial reds, pure Happy Wheels slapstick.
    public static class BloodFx
    {
        static readonly Color BloodBright = new Color(0.62f, 0.05f, 0.06f);
        static readonly Color BloodDark   = new Color(0.30f, 0.02f, 0.03f);

        public static void Burst(Vector3 pos, Vector3 dir)
        {
            // chunky splatter, reusing the debris burst in blood colors
            GlassBurst.Spawn(pos, dir, (int)(pos.x * 53f + pos.y * 7f),
                BloodBright, BloodDark, 18);

            var go = new GameObject("BloodBurst");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(BloodBright, BloodDark);
            main.gravityModifier = 1.6f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.enabled = false; // burst only, no ambient dribble
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            ps.Emit(45);
            Object.Destroy(go, 1.4f);
        }

        public static void Trail(Transform gib)
        {
            var go = new GameObject("BloodTrail");
            go.transform.SetParent(gib, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1.1f;
            main.loop = false;
            main.startLifetime = 0.55f;
            main.startSpeed = 0.6f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(BloodBright, BloodDark);
            main.gravityModifier = 1.8f;
            // world space + shape-only scaling: droplets fall away from the
            // chunk instead of riding it, and ignore the gib's box scale
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission;
            em.rateOverTime = 34f;
            ps.Play();
        }
    }
}
