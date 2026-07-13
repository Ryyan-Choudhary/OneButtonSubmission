using System.Collections;
using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// A hostile that shoots instead of grabbing:
    ///   Gunner    — trench coat, drawn pistol, fires a red tracer down a
    ///               FIXED lane on a steady cycle. A dim red line shows the
    ///               lane, so the danger reads before the first shot.
    ///   Rocketeer — flak vest and shoulder tube, lobs a homing missile
    ///               that chases the gun; bait it into walls or other
    ///               enemies.
    /// Both die to the player's bullets (and to missiles) exactly like the
    /// melee rival: gibs, blood, a mark on the tally.
    public class ShooterEnemy : MonoBehaviour
    {
        public enum Kind { Gunner, Rocketeer, Boss }

        public Kind kind;
        public float facing = 1f;
        public HudController hud;       // for the retry prompt on a gun kill
        public System.Action onRetry;
        public System.Action onKilled;  // the finale listens for the boss's death

        SummitAgent rig;
        Transform fireLane;
        bool dead;
        float cooldown;

        // the rocketeer's cycle is shorter because each launch now spends
        // ~1s on the lock-on telegraph first — net cadence is unchanged
        float FireInterval => kind == Kind.Gunner ? 2.4f
                            : kind == Kind.Rocketeer ? 3.6f
                            : 3.0f; // boss

        public void Build()
        {
            rig = new GameObject("Rig").AddComponent<SummitAgent>();
            rig.transform.SetParent(transform, false);
            rig.facing = facing;
            rig.wardrobe = kind == Kind.Gunner ? SummitAgent.Wardrobe.TrenchCoat
                         : kind == Kind.Rocketeer ? SummitAgent.Wardrobe.HeavyGear
                         : SummitAgent.Wardrobe.PurpleSuit;
            rig.Build();

            // hit volume on a direct child (positive scale — the rig's
            // facing mirror would upset the collider)
            var hitGo = new GameObject("HitBox");
            hitGo.transform.SetParent(transform, false);
            hitGo.transform.localPosition = new Vector3(0f, 3.0f, 0f);
            var box = hitGo.AddComponent<BoxCollider>();
            box.size = new Vector3(3.5f, 6.5f, 4f);
            box.isTrigger = true;
            hitGo.AddComponent<EnemyHitBox>().owner = this;

            var label = gameObject.AddComponent<CharacterLabel>();
            label.labelText = kind == Kind.Gunner ? "Hired Gun"
                            : kind == Kind.Rocketeer ? "Rocket Man"
                            : "Scar";
            label.worldYOffset = 7.5f;

            if (kind == Kind.Gunner) BuildFireLane();

            // desync shooters so a level never fires in unison
            float phase = Mathf.Abs(GetInstanceID() % 100) / 100f;
            cooldown = FireInterval * (0.5f + 0.6f * phase);
        }

        /// The weapon's muzzle in world space (the rig transform carries the
        /// facing mirror and the 3.2 scale).
        Vector3 MuzzlePos() => kind == Kind.Gunner
            ? rig.transform.TransformPoint(1.05f, 1.06f, 0f)
            : kind == Kind.Rocketeer
            ? rig.transform.TransformPoint(0.70f, 1.55f, 0.12f)
            : rig.transform.TransformPoint(1.25f, 1.06f, 0f); // shotgun tip

        /// A dim red line from the pistol to whatever the lane ends on, so
        /// the player can read the danger zone from across the canyon.
        void BuildFireLane()
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "FireLane";
            Destroy(line.GetComponent<Collider>());
            line.GetComponent<MeshRenderer>().sharedMaterial =
                MaterialFactory.Unlit(new Color(0.45f, 0.08f, 0.07f));
            line.transform.SetParent(transform, true); // dies with its gunner
            fireLane = line.transform;
            RefreshFireLane();
        }

        /// Re-measured before every shot: glass panes and dropped signs can
        /// change what the lane ends on mid-level.
        void RefreshFireLane()
        {
            if (fireLane == null) return;
            Vector3 muzzle = MuzzlePos();
            Vector3 fireDir = Vector3.right * facing;
            float len = 60f;
            if (Physics.Raycast(muzzle, fireDir, out var hit, 200f,
                ~0, QueryTriggerInteraction.Ignore))
                len = hit.distance;
            fireLane.position = muzzle + fireDir * (len * 0.5f);
            fireLane.localScale = new Vector3(len, 0.05f, 0.05f);
        }

        void Update()
        {
            if (dead) return;
            cooldown -= Time.deltaTime;
            if (cooldown > 0f) return;
            cooldown = FireInterval;

            if (kind == Kind.Gunner)
            {
                RefreshFireLane();
                EnemyBullet.Spawn(MuzzlePos() + Vector3.right * (facing * 0.4f),
                    Vector3.right * facing, this);
                AudioManager.Play(AudioManager.Sfx.Gunshot);
            }
            else if (kind == Kind.Rocketeer)
            {
                StartCoroutine(LockOnAndFire());
            }
            else
            {
                FireSpread();
            }
        }

        /// The boss's shotgun: a 5-pellet fan aimed at the player, pellets
        /// sagging slowly under gravity so the spread rains down the canyon.
        /// He holds fire until the player is close enough to matter.
        void FireSpread()
        {
            var gunBody = Object.FindFirstObjectByType<GunBody>();
            if (gunBody == null) return;
            Vector3 toPlayer = gunBody.transform.position - MuzzlePos();
            if (toPlayer.magnitude > 45f) return; // still far below — hold fire

            Vector3 baseDir = toPlayer.normalized;
            for (int i = -2; i <= 2; i++)
            {
                Vector3 d = Quaternion.Euler(0f, 0f, i * 9f) * baseDir;
                EnemyPellet.Spawn(MuzzlePos() + d * 0.8f, d, 13f, this);
            }
            AudioManager.Play(AudioManager.Sfx.Gunshot);
        }

        /// Missile-lock telegraph: a red reticle clamps onto the player's gun,
        /// a beep sounds, and one second later the rocket flies. Fair warning.
        IEnumerator LockOnAndFire()
        {
            var gunBody = Object.FindFirstObjectByType<GunBody>();
            if (gunBody == null) yield break;

            var reticle = LockOnReticle.Attach(gunBody.transform);
            AudioManager.Play(AudioManager.Sfx.LockBeep);
            yield return new WaitForSeconds(1.0f);
            if (reticle != null) Destroy(reticle.gameObject);
            if (dead) yield break;

            // up and out of the tube; the homing does the rest
            Vector3 launchDir = new Vector3(facing, 0.9f, 0f).normalized;
            HomingMissile.Spawn(MuzzlePos(), launchDir, this);
            AudioManager.Play(AudioManager.Sfx.Gunshot);
        }

        /// Same exit as the melee rival: gibs downrange, blood, gone.
        /// Returns false when he's already dead so nobody double-collects.
        public bool Kill(Vector3 hitDir)
        {
            if (dead) return false;
            dead = true;
            AudioManager.Play(AudioManager.Sfx.OhNo);

            var rng = new System.Random(GetInstanceID());
            var parts = rig.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < parts.Length; i++)
                Gib.Spawn(parts[i], hitDir, rng, bleeds: i % 3 == 0);
            BloodFx.Burst(transform.position + Vector3.up * 2.5f, hitDir);
            onKilled?.Invoke();
            Destroy(gameObject);
            return true;
        }
    }

    /// Marker on the shooter's trigger volume so projectiles can find the
    /// owner without walking the hierarchy.
    public class EnemyHitBox : MonoBehaviour
    {
        public ShooterEnemy owner;
    }

    /// The missile-lock warning: a red circle-and-crosshair reticle that
    /// clamps onto the gun, snaps down to size, and slowly spins. Destroyed
    /// by the rocketeer when the missile launches (or times out on its own
    /// if the rocketeer died mid-lock).
    public class LockOnReticle : MonoBehaviour
    {
        Transform target;
        float age;

        public static LockOnReticle Attach(Transform target)
        {
            var go = new GameObject("LockOnReticle");
            var r = go.AddComponent<LockOnReticle>();
            r.target = target;
            r.BuildVisual();
            return r;
        }

        void BuildVisual()
        {
            var mat = MaterialFactory.Emissive(
                new Color(1f, 0.15f, 0.10f), new Color(1f, 0.15f, 0.10f), 2.6f);

            // the circle: a ring of tangent segments
            const int segs = 12;
            const float radius = 1.7f;
            for (int i = 0; i < segs; i++)
            {
                float a = i * Mathf.PI * 2f / segs;
                var p = Piece(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f),
                    new Vector3(0.5f, 0.11f, 0.08f), mat, $"Ring{i}");
                p.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg + 90f);
            }
            // the cross
            Piece(Vector3.zero, new Vector3(2.6f, 0.08f, 0.08f), mat, "CrossH");
            Piece(Vector3.zero, new Vector3(0.08f, 2.6f, 0.08f), mat, "CrossV");
        }

        GameObject Piece(Vector3 lpos, Vector3 scale, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        void LateUpdate()
        {
            age += Time.deltaTime;
            if (target == null || age > 1.6f) // orphan safety
            {
                Destroy(gameObject);
                return;
            }
            transform.position = target.position;
            float snap = Mathf.Lerp(1.9f, 1f, Mathf.Clamp01(age / 0.4f));
            transform.localScale = Vector3.one * snap;
            transform.rotation = Quaternion.Euler(0f, 0f, age * 80f);
        }
    }
}
