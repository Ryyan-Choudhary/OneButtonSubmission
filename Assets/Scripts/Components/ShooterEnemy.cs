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
        public enum Kind { Gunner, Rocketeer }

        public Kind kind;
        public float facing = 1f;
        public HudController hud;       // for the retry prompt on a gun kill
        public System.Action onRetry;

        SummitAgent rig;
        Transform fireLane;
        bool dead;
        float cooldown;

        float FireInterval => kind == Kind.Gunner ? 2.4f : 4.6f;

        public void Build()
        {
            rig = new GameObject("Rig").AddComponent<SummitAgent>();
            rig.transform.SetParent(transform, false);
            rig.facing = facing;
            rig.wardrobe = kind == Kind.Gunner
                ? SummitAgent.Wardrobe.TrenchCoat
                : SummitAgent.Wardrobe.HeavyGear;
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
            label.labelText = kind == Kind.Gunner ? "Hired Gun" : "Rocket Man";
            label.worldYOffset = 7.5f;

            if (kind == Kind.Gunner) BuildFireLane();

            // desync shooters so a level never fires in unison
            float phase = Mathf.Abs(GetInstanceID() % 100) / 100f;
            cooldown = FireInterval * (0.5f + 0.6f * phase);
        }

        /// The pistol's muzzle / the launcher tube's mouth, in world space
        /// (the rig transform carries the facing mirror and the 3.2 scale).
        Vector3 MuzzlePos() => kind == Kind.Gunner
            ? rig.transform.TransformPoint(1.05f, 1.06f, 0f)
            : rig.transform.TransformPoint(0.70f, 1.55f, 0.12f);

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
            else
            {
                // up and out of the tube; the homing does the rest
                Vector3 launchDir = new Vector3(facing, 0.9f, 0f).normalized;
                HomingMissile.Spawn(MuzzlePos(), launchDir, this);
                AudioManager.Play(AudioManager.Sfx.Gunshot);
            }
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
}
