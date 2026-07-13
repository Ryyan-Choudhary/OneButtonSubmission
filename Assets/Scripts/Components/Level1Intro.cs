using System.Collections;
using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Audio;

namespace OneButtonSubmission.Components
{
    /// Level 1 opening cutscene, played once after the prologue. Inside a
    /// long office floor high in the right tower, Q walks in, sets a suitcase
    /// down, and steps back. A beat later the gun blows the case apart,
    /// knocks Q off his feet, and dashes the length of the building — a solid
    /// ~3 seconds of spinning flight past columns and ceiling lights, camera
    /// in pursuit, speed lines streaming — then smashes through the window
    /// wall and flash-cuts into the level. Chunky stepped poses throughout.
    public class Level1Intro : MonoBehaviour
    {
        public GunController gun;
        public CameraFollow follow;
        public Camera cam;
        public float wallRight;
        public float burstHeight;
        public int seed;
        public System.Action onLaunch;

        const float Tick = 0.18f;
        const float RoomW = 112f;

        float lx, rx;    // window wall (left) and far wall (right) of the office
        float floorTop;
        SummitAgent q;
        Transform suitcase;
        Material lineMat;
        Texture2D whiteTex;
        float flash;

        enum CamMode { Scene, Dash, Off }
        CamMode camMode = CamMode.Scene;
        Vector3 camSmooth;
        Vector3 camScenePos;
        float sceneTime;
        float shake;

        public void BuildAndPlay()
        {
            lx = wallRight + 16f;
            rx = lx + RoomW;
            floorTop = burstHeight - 2.2f;
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
            lineMat = MaterialFactory.Emissive(Color.white, Color.white, 1.6f);

            BuildRoom();
            BuildSuitcaseAndQ();

            gun.gameObject.SetActive(false); // still packed

            follow.target = null;
            camScenePos = new Vector3(rx - 10f, floorTop + 4.4f, -21f);
            camSmooth = camScenePos + new Vector3(1.5f, 0.8f, -3f); // start wide, drift in
            cam.transform.position = camSmooth;
            cam.transform.rotation = Quaternion.identity;

            StartCoroutine(Play());
        }

        // ---------- living camera ----------

        void Update()
        {
            if (camMode == CamMode.Scene)
            {
                sceneTime += Time.deltaTime;
                // slow push-in with a gentle handheld sway
                Vector3 target = camScenePos
                    + new Vector3(-0.5f, -0.4f, 3.5f) * Mathf.Clamp01(sceneTime / 7f)
                    + new Vector3(Mathf.Sin(sceneTime * 0.5f) * 0.3f,
                                  Mathf.Sin(sceneTime * 0.73f) * 0.18f, 0f);
                camSmooth = Vector3.Lerp(camSmooth, target, 2f * Time.deltaTime);
            }
            else if (camMode == CamMode.Dash)
            {
                // chase with a lead so the screen shows where the gun is going
                Vector3 target = new Vector3(
                    gun.transform.position.x - 4.5f, burstHeight + 1.4f, -19f);
                camSmooth = Vector3.Lerp(camSmooth, target, 7f * Time.deltaTime);
            }
            else return;

            cam.transform.position = camSmooth + (Vector3)(Random.insideUnitCircle * shake);
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.4f);
        }

        // ---------- set construction ----------

        void BuildRoom()
        {
            var interior = MaterialFactory.Lit(new Color(0.16f, 0.15f, 0.21f), 0.2f);
            var darker = MaterialFactory.Lit(new Color(0.11f, 0.10f, 0.14f), 0.35f);
            var paneMat = MaterialFactory.Lit(new Color(0.16f, 0.28f, 0.38f), 0.9f, 0.7f);
            var frameMat = MaterialFactory.Lit(new Color(0.07f, 0.07f, 0.10f), 0.4f, 0.4f);
            var lightMat = MaterialFactory.Emissive(new Color(1f, 0.92f, 0.75f), new Color(1f, 0.92f, 0.75f), 1.1f);

            float midX = (lx + rx) * 0.5f;
            float ceilY = floorTop + 9.4f;
            Prop(new Vector3(midX, floorTop - 0.4f, 0.5f), new Vector3(RoomW, 0.8f, 8f), darker, "Floor");
            Prop(new Vector3(midX, ceilY, 0.5f), new Vector3(RoomW, 0.6f, 8f), interior, "Ceiling");
            Prop(new Vector3(midX, floorTop + 4.5f, 3.8f), new Vector3(RoomW, 9.6f, 0.6f), interior, "BackWall");
            Prop(new Vector3(rx + 0.2f, floorTop + 4.5f, 0.5f), new Vector3(0.6f, 9.6f, 8f), interior, "FarWall");

            // the window wall the gun will smash through
            Prop(new Vector3(lx, floorTop + 4.5f, 0f), new Vector3(0.18f, 8.2f, 6.5f), paneMat, "WindowPane");
            Prop(new Vector3(lx, floorTop + 0.25f, 0f), new Vector3(0.4f, 0.5f, 7f), frameMat, "SillBar");
            Prop(new Vector3(lx, floorTop + 8.8f, 0f), new Vector3(0.4f, 0.5f, 7f), frameMat, "HeadBar");
            Prop(new Vector3(lx, floorTop + 4.5f, 0f), new Vector3(0.42f, 8.2f, 0.3f), frameMat, "Mullion");

            // interior rhythm: columns behind the flight path + ceiling lights
            // + back-wall glass, so the dash has things rushing past
            for (float x = lx + 8f; x <= rx - 14f; x += 9f)
                Prop(new Vector3(x, floorTop + 4.5f, 2.6f), new Vector3(1.2f, 9.6f, 1.2f), darker, "Column");
            for (float x = lx + 6f; x <= rx - 6f; x += 7f)
                Prop(new Vector3(x, ceilY - 0.45f, 0.2f), new Vector3(2.6f, 0.14f, 0.6f), lightMat, "CeilLight");
            for (float x = lx + 12f; x <= rx - 16f; x += 11f)
                Prop(new Vector3(x, floorTop + 4.6f, 3.45f), new Vector3(4f, 3f, 0.15f), paneMat, "WallGlass");
        }

        void BuildSuitcaseAndQ()
        {
            var leather = MaterialFactory.Lit(new Color(0.35f, 0.22f, 0.12f), 0.3f);
            var latchMat = MaterialFactory.Lit(new Color(0.75f, 0.65f, 0.35f), 0.6f, 0.8f);

            suitcase = new GameObject("Suitcase").transform;
            suitcase.SetParent(transform, false);
            Prop(Vector3.zero, new Vector3(1.7f, 1.1f, 0.55f), leather, "Case", suitcase);
            Prop(new Vector3(0f, 0.65f, 0f), new Vector3(0.55f, 0.16f, 0.14f), leather, "Handle", suitcase);
            Prop(new Vector3(-0.45f, 0.2f, -0.29f), new Vector3(0.16f, 0.12f, 0.06f), latchMat, "LatchL", suitcase);
            Prop(new Vector3(0.45f, 0.2f, -0.29f), new Vector3(0.16f, 0.12f, 0.06f), latchMat, "LatchR", suitcase);

            var qGo = new GameObject("Q");
            qGo.transform.SetParent(transform, false);
            qGo.transform.position = new Vector3(rx - 4f, floorTop, 0f);
            q = qGo.AddComponent<SummitAgent>();
            q.facing = -1f; // walks in leftward
            q.sidekick = true;
            q.Build();
            var label = qGo.AddComponent<CharacterLabel>();
            label.labelText = "Q";
            label.worldYOffset = 6.5f;

            HoldCase();
        }

        void Prop(Vector3 pos, Vector3 scale, Material mat, string name, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent != null ? parent : transform, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void HoldCase()
            => suitcase.position = q.transform.TransformPoint(0.55f, 0.5f, 0f);

        // ---------- the show ----------

        IEnumerator Play()
        {
            var tick = new WaitForSeconds(Tick);
            yield return new WaitForSeconds(0.9f); // settle in from the fade

            // walk in, case in hand
            q.ArmRPivot.localRotation = Quaternion.Euler(0f, 0f, 25f);
            for (int i = 0; i < 6; i++)
            {
                q.transform.position += new Vector3(-1.05f, 0f, 0f);
                float swing = i % 2 == 0 ? 24f : -18f;
                q.LegLPivot.localRotation = Quaternion.Euler(0f, 0f, swing);
                q.LegRPivot.localRotation = Quaternion.Euler(0f, 0f, -swing);
                HoldCase();
                yield return tick;
            }
            q.LegLPivot.localRotation = Quaternion.identity;
            q.LegRPivot.localRotation = Quaternion.identity;
            yield return tick;

            // set the case down in two chunky steps
            Vector3 caseRest = new Vector3(q.transform.position.x - 2f, floorTop + 0.55f, 0f);
            q.TorsoPivot.localRotation = Quaternion.Euler(0f, 0f, 14f);
            q.ArmRPivot.localRotation = Quaternion.Euler(0f, 0f, 55f);
            suitcase.position = Vector3.Lerp(suitcase.position, caseRest, 0.55f);
            yield return tick;
            suitcase.position = caseRest;
            yield return tick;
            q.TorsoPivot.localRotation = Quaternion.identity;
            q.ArmRPivot.localRotation = Quaternion.identity;
            yield return tick;

            // two steps back, then the promised quiet beat
            for (int i = 0; i < 2; i++)
            {
                q.transform.position += new Vector3(1.1f, 0f, 0f);
                float swing = i % 2 == 0 ? -18f : 14f;
                q.LegLPivot.localRotation = Quaternion.Euler(0f, 0f, swing);
                q.LegRPivot.localRotation = Quaternion.Euler(0f, 0f, -swing);
                yield return tick;
            }
            q.LegLPivot.localRotation = Quaternion.identity;
            q.LegRPivot.localRotation = Quaternion.identity;
            yield return new WaitForSeconds(1.2f);

            // BANG — the gun does not wait to be unpacked
            Vector3 casePos = suitcase.position;
            GlassBurst.Spawn(casePos + Vector3.up * 0.4f, Vector3.up, seed ^ 0x51,
                new Color(0.35f, 0.22f, 0.12f), new Color(0.75f, 0.65f, 0.35f), 20);
            AudioManager.Play(AudioManager.Sfx.Explosion);
            Destroy(suitcase.gameObject);
            shake = 0.55f;

            gun.gameObject.SetActive(true);
            gun.transform.SetPositionAndRotation(
                casePos + Vector3.up * 0.5f, Quaternion.Euler(0f, 180f, 0f));

            // Q gets blown off his feet, tips over on his heels
            q.ArmLPivot.localRotation = Quaternion.Euler(0f, 0f, -140f);
            q.ArmRPivot.localRotation = Quaternion.Euler(0f, 0f, -150f);
            q.transform.rotation = Quaternion.Euler(0f, 0f, -16f);
            q.transform.position += new Vector3(0.5f, 0f, 0f);
            gun.transform.position = casePos + Vector3.up * 1.4f;
            yield return tick;
            q.transform.rotation = Quaternion.Euler(0f, 0f, -38f);
            q.transform.position += new Vector3(0.6f, 0f, 0f);
            gun.transform.position = casePos + new Vector3(-0.3f, 2.3f, 0f);
            yield return tick;
            yield return tick; // hang in the air for a breath...

            // ...then TEAR down the whole office, spinning, camera in pursuit,
            // Q shrinking into the background
            camMode = CamMode.Dash;
            AudioManager.PlayLoop(AudioManager.Sfx.GunFlying);
            float speed = 6f;
            float spin = 0f;
            Vector3 pos = new Vector3(casePos.x, burstHeight, 0f);
            while (pos.x > lx + 0.8f)
            {
                float dt = Time.deltaTime;
                speed = Mathf.Min(speed + 90f * dt, 36f);
                spin -= 700f * dt;
                pos.x -= speed * dt;
                gun.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 0f, spin));
                shake = Mathf.Max(shake, Mathf.Lerp(0.02f, 0.13f, speed / 36f));
                SpawnSpeedLine(pos);
                SpawnSpeedLine(pos);
                SpawnSpeedLine(pos);
                yield return null;
            }

            // through the glass — flash-cut to the outside of the tower
            GlassBurst.Spawn(new Vector3(lx - 0.4f, burstHeight, 0f), Vector3.left, seed ^ 0x7);
            AudioManager.StopLoop();
            // window-break SFX comes from LaunchEntry (via onLaunch below),
            // which plays it for every level's entry — including this one
            AudioManager.PlayThemeLoop();
            flash = 1f;
            camMode = CamMode.Off;
            onLaunch?.Invoke();

            while (flash > 0f)
            {
                flash -= Time.deltaTime * 2.2f;
                yield return null;
            }
            Destroy(gameObject); // set, Q, and all props go with it
        }

        void SpawnSpeedLine(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SpeedLine";
            Destroy(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = lineMat;
            go.transform.position = pos + new Vector3(
                Random.Range(1.2f, 4.5f), Random.Range(-1.4f, 1.4f), Random.Range(-0.5f, 0.5f));
            go.transform.localScale = new Vector3(Random.Range(2.2f, 4.5f), 0.07f, 0.07f);
            go.AddComponent<SpeedLine>();
        }

        class SpeedLine : MonoBehaviour
        {
            float life = 0.28f;

            void Update()
            {
                life -= Time.deltaTime;
                if (life <= 0f) { Destroy(gameObject); return; }
                var s = transform.localScale;
                s.x *= 1f - 6f * Time.deltaTime;
                transform.localScale = s;
                transform.position += Vector3.right * (7f * Time.deltaTime);
            }
        }

        void OnGUI()
        {
            if (flash <= 0f) return;
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, flash * 0.9f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTex);
            GUI.color = prev;
        }

        void OnDestroy()
        {
            if (whiteTex != null) Destroy(whiteTex);
        }
    }
}