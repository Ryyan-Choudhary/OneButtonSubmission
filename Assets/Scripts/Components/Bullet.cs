using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// The actual slug: a fast emissive tracer, raycast-stepped every frame so
    /// it can never tunnel through a shelf. It ignores the gun that fired it,
    /// flies through harmless triggers (pickups, win volumes), kills a rival
    /// on contact, and pops a small debris puff on any solid surface.
    public class Bullet : MonoBehaviour
    {
        const float Speed = 70f;
        const float Life = 1.6f;

        static Material tracerMat;

        Transform owner; // the gun that fired it — never hit yourself
        Vector3 dir;
        float age;

        public static void Spawn(Vector3 pos, Vector3 dir, Transform owner)
        {
            if (tracerMat == null)
                tracerMat = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.2f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Bullet";
            Destroy(go.GetComponent<Collider>()); // movement is raycast-driven
            go.GetComponent<MeshRenderer>().sharedMaterial = tracerMat;
            go.transform.position = pos;
            // stretched along the flight path so it reads as a tracer
            go.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
            go.transform.localScale = new Vector3(0.55f, 0.09f, 0.09f);

            var b = go.AddComponent<Bullet>();
            b.dir = dir.normalized;
            b.owner = owner;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life)
            {
                Destroy(gameObject);
                return;
            }

            float step = Speed * Time.deltaTime;
            var hits = Physics.RaycastAll(transform.position, dir, step,
                ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (owner != null && hit.collider.transform.IsChildOf(owner)) continue;

                // gibs blow downrange with the shot; a confirmed kill on any
                // enemy type is worth 2 shells back and a mark on the tally
                var rival = hit.collider.GetComponent<VillainTrigger>();
                if (rival != null)
                {
                    if (rival.KillVillain(dir)) CollectBounty();
                    Impact(hit.point);
                    return;
                }
                var shooter = hit.collider.GetComponent<EnemyHitBox>();
                if (shooter != null)
                {
                    if (shooter.owner != null && shooter.owner.Kill(dir)) CollectBounty();
                    Impact(hit.point);
                    return;
                }
                if (hit.collider.isTrigger) continue; // pickups, win volume: pass through

                // demolition: a shell spent on glass opens the route
                var glass = hit.collider.GetComponent<GlassBarrier>();
                if (glass != null)
                {
                    glass.Shatter(dir);
                    Destroy(gameObject);
                    return;
                }
                Impact(hit.point);
                return;
            }
            transform.position += dir * step;
        }

        /// A confirmed kill pays out: tally mark plus 2 shells with a
        /// reload click, so the magazine visibly relights mid-flight.
        void CollectBounty()
        {
            GameStats.Kills++;
            var ammo = owner != null ? owner.GetComponent<AmmoSystemBehaviour>() : null;
            if (ammo != null)
            {
                ammo.System.Refill(2);
                AudioManager.Play(AudioManager.Sfx.Reload);
            }
        }

        /// Small gray puff + flash, reusing the debris burst at low count.
        void Impact(Vector3 point)
        {
            GlassBurst.Spawn(point, -dir, (int)(point.x * 131f + point.y * 17f),
                new Color(0.75f, 0.72f, 0.66f), new Color(0.35f, 0.34f, 0.38f), 7);
            Destroy(gameObject);
        }
    }
}
