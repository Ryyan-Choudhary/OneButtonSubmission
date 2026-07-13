using UnityEngine;
using UnityEngine.InputSystem;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Components
{
    /// A gunner's round: a red tracer flying a fixed lane. It destroys the
    /// player's gun on contact, ignores its own shooter, passes through
    /// harmless triggers (and other enemies — only the missile does friendly
    /// fire), and puffs out on solid surfaces. Raycast-stepped like the
    /// player's bullet so it can't tunnel.
    public class EnemyBullet : MonoBehaviour
    {
        const float Speed = 26f;
        const float Life = 4f;

        static Material tracerMat;

        Transform shooterRoot;
        HudController hud;
        System.Action onRetry;
        Vector3 dir;
        float age;

        public static void Spawn(Vector3 pos, Vector3 dir, ShooterEnemy shooter)
        {
            if (tracerMat == null)
                tracerMat = MaterialFactory.Emissive(Palette.Laser, Palette.Laser, 3.0f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EnemyBullet";
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = tracerMat;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
            go.transform.localScale = new Vector3(1.5f, 0.3f, 0.3f); // fat and readable
            go.transform.SetParent(shooter.transform.parent, true); // dies with the level

            var b = go.AddComponent<EnemyBullet>();
            b.dir = dir.normalized;
            b.shooterRoot = shooter.transform;
            b.hud = shooter.hud;
            b.onRetry = shooter.onRetry;
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
                if (shooterRoot != null && hit.collider.transform.IsChildOf(shooterRoot)) continue;

                var body = hit.collider.GetComponentInParent<GunBody>();
                if (body != null)
                {
                    GunDeath.TryKill(body, hud, onRetry);
                    Destroy(gameObject);
                    return;
                }
                if (hit.collider.isTrigger) continue;

                GlassBurst.Spawn(hit.point, -dir, (int)(hit.point.x * 91f + hit.point.y * 13f),
                    new Color(0.75f, 0.72f, 0.66f), new Color(0.35f, 0.34f, 0.38f), 5);
                Destroy(gameObject);
                return;
            }
            transform.position += dir * step;
        }
    }

    /// The rocketeer's missile: launched upward, then it steers toward the
    /// player's gun with a limited turn rate — outfly it, or curve it into
    /// a wall or another enemy. It explodes on ANYTHING solid, gibs any
    /// enemy it touches (friendly fire is the smart play), and destroys the
    /// player's gun on contact.
    public class HomingMissile : MonoBehaviour
    {
        const float Speed = 11f;
        const float TurnDegPerSec = 100f;
        const float Life = 7f;

        Transform shooterRoot;
        HudController hud;
        System.Action onRetry;
        GunBody target;
        Vector3 dir;
        float age;

        public static void Spawn(Vector3 pos, Vector3 dir, ShooterEnemy shooter)
        {
            var go = new GameObject("HomingMissile");
            go.transform.position = pos;
            go.transform.SetParent(shooter.transform.parent, true); // dies with the level

            // red-and-white paint job, self-lit, sized to be unmissable
            var bodyMat = MaterialFactory.Lit(new Color(0.92f, 0.90f, 0.88f), 0.4f, 0.2f);
            bodyMat.EnableKeyword("_EMISSION");
            bodyMat.SetColor("_EmissionColor", new Color(0.92f, 0.90f, 0.88f) * 0.4f);
            var redMat = MaterialFactory.Emissive(
                new Color(0.95f, 0.10f, 0.08f), new Color(0.95f, 0.10f, 0.08f), 1.8f);
            var burnMat = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.8f);
            Piece(go.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0f),
                new Vector3(2.0f, 0.5f, 0.5f), bodyMat, "Body");
            Piece(go.transform, PrimitiveType.Cube, new Vector3(1.15f, 0f, 0f),
                new Vector3(0.45f, 0.42f, 0.42f), redMat, "Nose");
            Piece(go.transform, PrimitiveType.Cube, new Vector3(0.1f, 0f, 0f),
                new Vector3(0.35f, 0.56f, 0.56f), redMat, "Band");
            Piece(go.transform, PrimitiveType.Cube, new Vector3(-0.85f, 0.35f, 0f),
                new Vector3(0.5f, 0.22f, 0.16f), redMat, "FinTop");
            Piece(go.transform, PrimitiveType.Cube, new Vector3(-0.85f, -0.35f, 0f),
                new Vector3(0.5f, 0.22f, 0.16f), redMat, "FinBottom");
            Piece(go.transform, PrimitiveType.Cube, new Vector3(-1.15f, 0f, 0f),
                new Vector3(0.3f, 0.3f, 0.3f), burnMat, "Exhaust");

            // smoke trail: proper soft-particle material so it renders as grey
            // puffs (an unassigned material would show magenta squares)
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.startLifetime = 1.0f;
            main.startSpeed = 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.66f, 0.66f, 0.68f, 0.55f), new Color(0.42f, 0.42f, 0.45f, 0.45f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;
            var em = ps.emission;
            em.rateOverTime = 32f;
            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.SoftParticle();
            ps.Play();

            var m = go.AddComponent<HomingMissile>();
            m.dir = dir.normalized;
            m.shooterRoot = shooter.transform;
            m.hud = shooter.hud;
            m.onRetry = shooter.onRetry;
            m.target = Object.FindFirstObjectByType<GunBody>();
            go.transform.rotation = Quaternion.FromToRotation(Vector3.right, m.dir);
        }

        static void Piece(Transform parent, PrimitiveType type, Vector3 lpos,
            Vector3 lscale, Material mat, string name)
        {
            var p = GameObject.CreatePrimitive(type);
            p.name = name;
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(parent, false);
            p.transform.localPosition = lpos;
            p.transform.localScale = lscale;
            p.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life)
            {
                Explode(transform.position);
                return;
            }

            // steer toward the gun; a lost target means it just flies straight
            if (target != null)
            {
                Vector3 want = (target.transform.position - transform.position).normalized;
                dir = Vector3.RotateTowards(dir, want,
                    TurnDegPerSec * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
            }
            transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);

            float step = Speed * Time.deltaTime;
            var hits = Physics.RaycastAll(transform.position, dir, step,
                ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (shooterRoot != null && hit.collider.transform.IsChildOf(shooterRoot)) continue;

                var body = hit.collider.GetComponentInParent<GunBody>();
                if (body != null)
                {
                    GunDeath.TryKill(body, hud, onRetry);
                    Explode(hit.point);
                    return;
                }
                var enemyHit = hit.collider.GetComponent<EnemyHitBox>();
                if (enemyHit != null)
                {
                    if (enemyHit.owner != null && enemyHit.owner.Kill(dir))
                        GameStats.Kills++; // engineered kills count on the tally
                    Explode(hit.point);
                    return;
                }
                var rival = hit.collider.GetComponent<VillainTrigger>();
                if (rival != null)
                {
                    if (rival.KillVillain(dir))
                        GameStats.Kills++;
                    Explode(hit.point);
                    return;
                }
                if (hit.collider.isTrigger) continue;

                // the blast takes glass with it
                var glass = hit.collider.GetComponent<GlassBarrier>();
                if (glass != null) glass.Shatter(dir);

                Explode(hit.point); // walls, balconies — anything solid
                return;
            }
            transform.position += dir * step;
        }

        void Explode(Vector3 point)
        {
            AudioManager.Play(AudioManager.Sfx.Explosion);
            GlassBurst.Spawn(point, -dir, (int)(point.x * 47f + point.y * 29f),
                Palette.Muzzle, new Color(0.25f, 0.22f, 0.20f), 16);
            Destroy(gameObject);
        }
    }

    /// The player's gun getting destroyed by enemy fire: one big amber
    /// burst, the gun is gone, and the same Z-to-retry prompt as every
    /// other death. Refuses while a cutscene owns the gun (its controller
    /// is disabled there) so a stray round can't ruin a win or a steal.
    public static class GunDeath
    {
        public static bool TryKill(GunBody body, HudController hud, System.Action onRetry)
        {
            if (body == null) return false;
            var gun = body.GetComponent<GunController>();
            if (gun == null || !gun.enabled) return false;

            AudioManager.Play(AudioManager.Sfx.Explosion);
            Vector3 pos = body.transform.position;
            GlassBurst.Spawn(pos, Vector3.up, (int)(pos.x * 71f + pos.y * 11f),
                Palette.Muzzle, Palette.GunMetal, 22);
            Object.Destroy(body.gameObject); // OnDisable exits any slow-mo
            RetryPrompt.Show(hud, onRetry);
            return true;
        }
    }

    /// Free-standing "RETRY ? — press Z" prompt, for deaths that happen
    /// outside any trigger's cutscene (the gun being shot out of the air).
    public class RetryPrompt : MonoBehaviour
    {
        HudController hud;
        System.Action onRetry;
        InputAction retryAction;

        public static void Show(HudController hud, System.Action onRetry)
        {
            var go = new GameObject("RetryPrompt");
            var p = go.AddComponent<RetryPrompt>();
            p.hud = hud;
            p.onRetry = onRetry;
        }

        void Start()
        {
            if (hud != null) hud.bannerText = "RETRY ?  — press Z";
            retryAction = new InputAction("Retry", InputActionType.Button);
            retryAction.AddBinding("<Keyboard>/z");
            retryAction.AddBinding("<Gamepad>/rightTrigger");
            retryAction.started += OnRetryPressed;
            retryAction.Enable();
        }

        void OnDestroy()
        {
            if (retryAction == null) return;
            retryAction.started -= OnRetryPressed;
            retryAction.Dispose();
        }

        void OnRetryPressed(InputAction.CallbackContext ctx)
        {
            if (hud != null) hud.bannerText = "";
            var retry = onRetry;
            Destroy(gameObject);
            retry?.Invoke();
        }
    }
}
