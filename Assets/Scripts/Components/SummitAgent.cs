using System.Collections;
using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// The figure waiting at the summit, voxel-built, ~8fps stepped poses.
    /// Agent (black suit): grabs the gun, kneels, fires two shots downrange.
    /// Villain (white suit, red tie): snatches the gun, brandishes it
    /// overhead, then turns his back and bolts with it.
    public class SummitAgent : MonoBehaviour
    {
        /// Every hostile reads differently at a glance: the melee rival in
        /// his white suit, the gunner in a brown trench coat and flat cap
        /// with a drawn pistol, the rocketeer in olive fatigues with a flak
        /// vest, helmet, and shoulder tube. Auto maps the old bool flags.
        public enum Wardrobe { Auto, Suit, Shirtsleeves, WhiteSuit, TrenchCoat, HeavyGear }

        public float facing = 1f; // +1 faces right, -1 faces left
        public float scale = 3.2f; // whole-rig size multiplier (~5.6 units tall)
        public bool villain;  // steals the gun instead of firing it
        public bool sidekick; // shirtsleeves, no hat — the quartermaster look
        public Wardrobe wardrobe = Wardrobe.Auto;

        const float Tick = 0.13f; // one animation "frame"

        Transform torso, head, armL, armR, legL, legR;
        Material suitMat, shirtMat, tieMat, skinMat, glassMat, flashMat;
        bool started;

        // joint access for external puppeteering (intro cutscene)
        public Transform TorsoPivot => torso;
        public Transform HeadPivot => head;
        public Transform ArmLPivot => armL;
        public Transform ArmRPivot => armR;
        public Transform LegLPivot => legL;
        public Transform LegRPivot => legR;

        public void Build()
        {
            var look = wardrobe != Wardrobe.Auto ? wardrobe
                     : sidekick ? Wardrobe.Shirtsleeves
                     : villain ? Wardrobe.WhiteSuit
                     : Wardrobe.Suit;

            Color suitCol, shirtCol, tieCol;
            switch (look)
            {
                case Wardrobe.WhiteSuit:  // the rival: white suit, red tie
                    suitCol  = new Color(0.88f, 0.86f, 0.82f);
                    shirtCol = new Color(0.10f, 0.10f, 0.12f);
                    tieCol   = new Color(0.72f, 0.10f, 0.08f);
                    break;
                case Wardrobe.TrenchCoat: // the gunner: proper brown suit, amber tie
                    suitCol  = new Color(0.45f, 0.30f, 0.16f);
                    shirtCol = new Color(0.10f, 0.10f, 0.12f);
                    tieCol   = new Color(0.85f, 0.55f, 0.18f);
                    break;
                case Wardrobe.HeavyGear:  // the rocketeer: bright white fatigues, hot red vest
                    suitCol  = new Color(0.97f, 0.96f, 0.94f);
                    shirtCol = new Color(0.14f, 0.12f, 0.10f);
                    tieCol   = new Color(0.95f, 0.10f, 0.08f);
                    break;
                default:                  // Bond & the quartermaster: black suit, white shirt
                    suitCol  = new Color(0.07f, 0.07f, 0.09f);
                    shirtCol = new Color(0.90f, 0.90f, 0.92f);
                    tieCol   = new Color(0.07f, 0.07f, 0.09f);
                    break;
            }
            suitMat  = MaterialFactory.Lit(suitCol, 0.35f);
            shirtMat = MaterialFactory.Lit(shirtCol, 0.3f);
            tieMat   = MaterialFactory.Lit(tieCol, 0.3f);
            if (look == Wardrobe.HeavyGear)
            {
                // self-lit so the red-and-white pops against any sky
                suitMat.EnableKeyword("_EMISSION");
                suitMat.SetColor("_EmissionColor", suitCol * 0.35f);
                tieMat.EnableKeyword("_EMISSION");
                tieMat.SetColor("_EmissionColor", tieCol * 0.6f);
            }
            skinMat  = MaterialFactory.Lit(new Color(0.85f, 0.62f, 0.45f), 0.2f);
            glassMat = MaterialFactory.Lit(new Color(0.03f, 0.03f, 0.04f), 0.8f, 0.5f);
            flashMat = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.5f);

            // pivots sit at the joints so poses rotate about hips/shoulders
            legL = Pivot("LegL", new Vector3(-0.11f, 0.5f, 0f));
            Box(legL, new Vector3(0f, -0.25f, 0f), new Vector3(0.18f, 0.5f, 0.18f), suitMat, "Shin");
            legR = Pivot("LegR", new Vector3(0.11f, 0.5f, 0f));
            Box(legR, new Vector3(0f, -0.25f, 0f), new Vector3(0.18f, 0.5f, 0.18f), suitMat, "Shin");

            // shirtsleeves: torso and sleeves in shirt-white (jacket left at home)
            Material topMat = look == Wardrobe.Shirtsleeves ? shirtMat : suitMat;
            torso = Pivot("Torso", new Vector3(0f, 0.83f, 0f));
            Box(torso, Vector3.zero, new Vector3(0.5f, 0.65f, 0.3f), topMat, "Jacket");
            if (look != Wardrobe.Shirtsleeves)
                Box(torso, new Vector3(0f, 0.1f, -0.16f), new Vector3(0.16f, 0.34f, 0.05f), shirtMat, "Shirt");
            Box(torso, new Vector3(0f, 0.06f, -0.19f), new Vector3(0.07f, 0.28f, 0.04f), tieMat, "Tie");

            armL = Pivot("ArmL", new Vector3(-0.33f, 1.06f, 0f));
            Box(armL, new Vector3(0f, -0.25f, 0f), new Vector3(0.13f, 0.5f, 0.13f), topMat, "Sleeve");
            armR = Pivot("ArmR", new Vector3(0.33f, 1.06f, 0f));
            Box(armR, new Vector3(0f, -0.25f, 0f), new Vector3(0.13f, 0.5f, 0.13f), topMat, "Sleeve");

            head = Pivot("Head", new Vector3(0f, 1.32f, 0f));
            Box(head, Vector3.zero, new Vector3(0.3f, 0.3f, 0.28f), skinMat, "Skull");
            // sunglasses: two lenses sitting flush on the front of the face
            // with a bridge between them. Kept inside the skull's own X/Y
            // footprint and only slightly proud of its front face (z), so
            // nothing pokes out the cheeks or the back of the head.
            Box(head, new Vector3( 0.08f, 0.04f, -0.15f), new Vector3(0.11f, 0.08f, 0.05f), glassMat, "LensR");
            Box(head, new Vector3(-0.08f, 0.04f, -0.15f), new Vector3(0.11f, 0.08f, 0.05f), glassMat, "LensL");
            Box(head, new Vector3( 0f,    0.04f, -0.15f), new Vector3(0.05f, 0.03f, 0.05f), glassMat, "Bridge");
            if (look == Wardrobe.Suit || look == Wardrobe.WhiteSuit)
            {
                Box(head, new Vector3(0f, 0.17f, 0f), new Vector3(0.46f, 0.05f, 0.4f), suitMat, "HatBrim");
                Box(head, new Vector3(0f, 0.28f, 0f), new Vector3(0.3f, 0.18f, 0.28f), suitMat, "HatCrown");
            }

            if (look == Wardrobe.TrenchCoat)
            {
                // long coat skirt, flat cap, and a drawn pistol on a level arm
                Box(torso, new Vector3(0f, -0.44f, 0f), new Vector3(0.52f, 0.35f, 0.32f), suitMat, "CoatSkirt");
                Box(head, new Vector3(0f, 0.19f, 0f), new Vector3(0.34f, 0.10f, 0.32f), suitMat, "FlatCap");
                Box(armR, new Vector3(0f, -0.68f, 0f), new Vector3(0.10f, 0.30f, 0.09f), glassMat, "Pistol");
                armR.localRotation = Quaternion.Euler(0f, 0f, 90f); // aiming down his lane
            }
            else if (look == Wardrobe.HeavyGear)
            {
                // flak vest, helmet, and the launcher tube on his shoulder
                Box(torso, new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.50f, 0.36f), tieMat, "FlakVest");
                Box(head, new Vector3(0f, 0.16f, 0f), new Vector3(0.36f, 0.22f, 0.34f), suitMat, "Helmet");
                Box(torso, new Vector3(0.10f, 0.72f, 0.12f), new Vector3(1.15f, 0.16f, 0.16f), glassMat, "LauncherTube");
                Box(torso, new Vector3(0.42f, 0.72f, 0.12f), new Vector3(0.10f, 0.20f, 0.20f), tieMat, "TubeBand");
            }

            // mirror the whole rig to face the canyon, blown up to hero size
            transform.localScale = new Vector3(facing * scale, scale, scale);
        }

        public void StartCutscene(GunBody gunBody, System.Action onDone, bool fullEscape = true)
        {
            if (started) return;
            started = true;

            // take control: the delivery is complete
            var controller = gunBody.GetComponent<GunController>();
            if (controller != null) controller.enabled = false; // also exits any slow-mo
            var laser = gunBody.GetComponentInChildren<AimLaser>();
            if (laser != null) laser.gameObject.SetActive(false);
            var rb = gunBody.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            StartCoroutine(Cutscene(gunBody.transform, onDone, fullEscape));
        }

        IEnumerator Cutscene(Transform gun, System.Action onDone, bool fullEscape)
        {
            var hold = new WaitForSeconds(Tick);
            Vector3 fromPos = gun.position;
            Quaternion fromRot = gun.rotation;
            Quaternion aimRot = facing > 0f
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);

            // frame 1: reach for it
            armR.localRotation = Quaternion.Euler(0f, 0f, 60f);
            armL.localRotation = Quaternion.Euler(0f, 0f, 35f);
            yield return hold;

            // frames 2-3: the gun snaps to his hands in two chunky steps
            Vector3 hand = transform.TransformPoint(0.62f, 0.98f, 0f);
            gun.SetPositionAndRotation(
                Vector3.Lerp(fromPos, hand, 0.5f), Quaternion.Slerp(fromRot, aimRot, 0.5f));
            yield return hold;
            gun.SetPositionAndRotation(hand, aimRot);
            yield return hold;

            if (villain)
                yield return fullEscape ? StealAndRun(gun) : StealAndBrandish(gun);
            else
                yield return KneelAndFire(gun, aimRot);

            yield return new WaitForSeconds(Tick * 3f);
            onDone?.Invoke();
        }

        IEnumerator KneelAndFire(Transform gun, Quaternion aimRot)
        {
            AudioManager.Play(AudioManager.Sfx.Victory);
            var hold = new WaitForSeconds(Tick);

            // drop to one knee, two-hand aim
            KneelPose();
            Vector3 aimPos = transform.TransformPoint(0.62f, 0.85f, 0f);
            gun.SetPositionAndRotation(aimPos, aimRot);
            yield return hold;
            yield return hold;

            // two shots, each: kick frame, then settle back to aim
            for (int shot = 0; shot < 2; shot++)
            {
                gun.SetPositionAndRotation(
                    aimPos - Vector3.right * (facing * 0.14f),
                    aimRot * Quaternion.Euler(0f, 0f, 14f));
                armR.localRotation = Quaternion.Euler(0f, 0f, 78f);
                armL.localRotation = Quaternion.Euler(0f, 0f, 82f);
                SpawnFlash(gun);
                SpawnTracer(gun);
                AudioManager.Play(AudioManager.Sfx.Gunshot);
                yield return hold;

                gun.SetPositionAndRotation(aimPos, aimRot);
                armR.localRotation = Quaternion.Euler(0f, 0f, 90f);
                armL.localRotation = Quaternion.Euler(0f, 0f, 90f);
                yield return hold;
                yield return hold;
            }
        }

        /// Shot by the player's bullet: the whole rig blows apart into
        /// ragdoll gibs carried by the shot's direction, with a blood spray
        /// at the wound and droplet trails off the bigger chunks — Happy
        /// Wheels slapstick. Does nothing if a cutscene already owns him
        /// (mid-steal he's untouchable).
        public void Die(Vector3 hitDir)
        {
            if (started) return;
            started = true;
            AudioManager.Play(AudioManager.Sfx.OhNo);

            var rng = new System.Random(GetInstanceID());
            var parts = GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < parts.Length; i++)
                Gib.Spawn(parts[i], hitDir, rng, bleeds: i % 3 == 0);
            BloodFx.Burst(transform.position + Vector3.up * (scale * 0.8f), hitDir);
            Destroy(gameObject);
        }

        IEnumerator StealAndBrandish(Transform gun)
        {
            AudioManager.Play(AudioManager.Sfx.OhNo);
            var hold = new WaitForSeconds(Tick);

            armR.localRotation = Quaternion.Euler(0f, 0f, 180f);
            armL.localRotation = Quaternion.Euler(0f, 0f, 180f);
            Vector3 overhead = transform.TransformPoint(0.05f, 2.05f, 0f);
            float[] waggle = { 60f, 110f, 60f };
            foreach (float tilt in waggle)
            {
                gun.SetPositionAndRotation(overhead, RotFor(facing) * Quaternion.Euler(0f, 0f, tilt));
                yield return hold;
            }
        }

        IEnumerator StealAndRun(Transform gun)
        {
            yield return StealAndBrandish(gun);
            var hold = new WaitForSeconds(Tick);
            facing = -facing;
            transform.localScale = new Vector3(facing * scale, scale, scale);
            armR.localRotation = Quaternion.Euler(0f, 0f, 45f);
            armL.localRotation = Quaternion.Euler(0f, 0f, 45f);
            float baseY = transform.position.y;
            for (int step = 0; step < 5; step++)
            {
                transform.position = new Vector3(
                    transform.position.x + facing * 1.1f,
                    baseY + (step % 2 == 0 ? 0.25f : 0f),
                    0f);
                gun.SetPositionAndRotation(
                    transform.TransformPoint(0.55f, 0.95f, 0f), RotFor(facing));
                yield return hold;
            }
            transform.position = new Vector3(transform.position.x, baseY, 0f);
            gun.SetPositionAndRotation(transform.TransformPoint(0.55f, 0.95f, 0f), RotFor(facing));
        }

        static Quaternion RotFor(float f)
            => f > 0f ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

        void KneelPose()
        {
            torso.localPosition = new Vector3(0.02f, 0.55f, 0f);
            torso.localRotation = Quaternion.Euler(0f, 0f, -6f);
            head.localPosition = new Vector3(0.05f, 1.04f, 0f);
            legL.localPosition = new Vector3(-0.13f, 0.18f, 0f);
            legL.localRotation = Quaternion.Euler(0f, 0f, -90f); // rear shin flat on the ground
            legR.localPosition = new Vector3(0.22f, 0.22f, 0f);  // front shin planted upright
            armL.localPosition = new Vector3(0.12f, 0.92f, 0f);
            armR.localPosition = new Vector3(0.18f, 1.0f, 0f);
            armL.localRotation = Quaternion.Euler(0f, 0f, 90f);  // arms level, two-hand grip
            armR.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        void SpawnFlash(Transform gun)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Flash";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = gun.TransformPoint(0.9f, 0.12f, 0f);
            go.transform.localScale = Vector3.one * 0.45f;
            go.GetComponent<MeshRenderer>().sharedMaterial = flashMat;
            Destroy(go, Tick);
        }

        void SpawnTracer(Transform gun)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Tracer";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = gun.TransformPoint(0.9f, 0.12f, 0f);
            go.transform.localScale = Vector3.one * 0.14f;
            go.GetComponent<MeshRenderer>().sharedMaterial = flashMat;
            StartCoroutine(FlyTracer(go.transform));
        }

        IEnumerator FlyTracer(Transform tracer)
        {
            float life = 0.8f;
            while (life > 0f && tracer != null)
            {
                tracer.position += Vector3.right * (facing * 30f * Time.deltaTime);
                life -= Time.deltaTime;
                yield return null;
            }
            if (tracer != null) Destroy(tracer.gameObject);
        }

        Transform Pivot(string name, Vector3 pos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            t.localPosition = pos;
            return t;
        }

        void Box(Transform pivot, Vector3 lpos, Vector3 size, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(pivot, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}