using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OneButtonSubmission.Components;
using OneButtonSubmission.Art;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// Builds the whole colored, animated, multi-level game at runtime. A persistent
    /// rig (camera, lights, sky, HUD, materials) is built once; per-level content
    /// (player+gun, terrain, pickups, summit, ridges) is torn down and rebuilt as
    /// you progress. Twilight/dusk palette.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World")]
        public int backgroundSeed = 20260712;

        [Header("Camera")]
        public float cameraSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.5f, -12f);

        [Header("Ammo")]
        public int maxShells = 6;
        public int startShells = 6;

        [Header("Pickups")]
        public int pickupRefill = 3;

        [Header("Transitions")]
        public float levelBannerSeconds = 2f;

        LevelConfig[] levels;
        int currentLevel;

        // persistent materials
        Material rockMat, groundMat, bodyMat, headMat, limbMat;
        Material gunMetalMat, gunWoodMat, muzzleMat, ammoMat, summitMat;

        // persistent rig
        Camera builtCamera;
        CameraFollow follow;
        Material skyMat;
        HudController hud;
        GameManager manager;

        // per-level
        GameObject levelRoot;
        AmmoSystemBehaviour ammo;
        PlayerJuice playerJuice;

        void Awake()
        {
            levels = new[] { LevelConfig.Foothills(), LevelConfig.TheSpire() };

            BuildMaterials();
            BuildLights();
            BuildCamera();
            BuildSky(builtCamera);
            hud = BuildHud();
            manager = gameObject.AddComponent<GameManager>();
            manager.OnWin += OnLevelComplete;

            BuildLevel(0);
        }

        // ---------- persistent build ----------

        void BuildMaterials()
        {
            rockMat     = MaterialFactory.Lit(Palette.Rock, 0.12f);
            groundMat   = MaterialFactory.Lit(Palette.Ground, 0.10f);
            bodyMat     = MaterialFactory.Lit(Palette.Body, 0.25f);
            headMat     = MaterialFactory.Lit(Palette.Head, 0.30f);
            limbMat     = MaterialFactory.Lit(Palette.Limb, 0.20f);
            gunMetalMat = MaterialFactory.Lit(Palette.GunMetal, 0.45f, 0.6f);
            gunWoodMat  = MaterialFactory.Lit(Palette.GunWood, 0.25f);
            muzzleMat   = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.0f);
            ammoMat     = MaterialFactory.Emissive(Palette.Ammo, Palette.Ammo, 1.6f);
            summitMat   = MaterialFactory.Emissive(Palette.Summit, Palette.Summit, 1.2f);
        }

        void BuildLights()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) Destroy(l.gameObject);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            var key = new GameObject("KeyLight").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = Palette.KeyLight;
            key.intensity = 1.1f;
            key.shadows = LightShadows.Soft;
            key.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            var rim = new GameObject("RimLight").AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = Palette.RimLight;
            rim.intensity = 0.5f;
            rim.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);
        }

        void BuildCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
            }
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.farClipPlane = 150f;

            follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.smoothTime = cameraSmoothTime;
            follow.offset = cameraOffset;

            builtCamera = cam;
        }

        void BuildSky(Camera cam)
        {
            skyMat = MaterialFactory.UnlitTexture(MakeVerticalGradient(Palette.SkyBottom, Palette.SkyTop, 256));
            var sky = new GameObject("Sky");
            var mf = sky.AddComponent<MeshFilter>();
            var mr = sky.AddComponent<MeshRenderer>();
            mr.sharedMaterial = skyMat;

            float d = cam.farClipPlane * 0.85f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float w = h * aspect;
            mf.sharedMesh = MakeQuadMesh(w * 1.3f, h * 1.3f);
            sky.transform.SetParent(cam.transform, false);
            sky.transform.localPosition = new Vector3(0f, 0f, d);
            sky.transform.localRotation = Quaternion.identity;
        }

        HudController BuildHud()
        {
            var go = new GameObject("HUD");
            return go.AddComponent<HudController>();
        }

        // ---------- per-level build ----------

        void BuildLevel(int index)
        {
            Time.timeScale = 1f; // safety: never carry slow-mo across a rebuild
            currentLevel = index;
            var lv = levels[index];

            if (levelRoot != null) Destroy(levelRoot);
            levelRoot = new GameObject($"Level_{index}");

            Physics.gravity = new Vector3(0f, lv.gravityY, 0f);
            RenderSettings.ambientLight = lv.ambient;
            builtCamera.backgroundColor = lv.skyBottom;
            skyMat.mainTexture = MakeVerticalGradient(lv.skyBottom, lv.skyTop, 256);

            // player + gun
            var player = BuildPlayer(new Vector3(lv.startPos.x, lv.startPos.y, 0f));
            player.transform.SetParent(levelRoot.transform, true);
            ammo = player.gameObject.AddComponent<AmmoSystemBehaviour>();
            ammo.maxShells = maxShells;
            ammo.startShells = startShells;
            var gun = BuildGun(player, lv.gun);
            BuildMuzzleFlash(gun);

            // terrain
            var ground = BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 4f));
            ground.transform.SetParent(levelRoot.transform, true);
            BuildLedges(lv.ledges, levelRoot.transform);

            // pickups (one near the start plus the route ones)
            Parent(BuildAmmoPickup(new Vector3(lv.startPos.x + 3f, lv.startPos.y - 1f, 0f)));
            if (lv.routePickups != null)
                foreach (var p in lv.routePickups)
                    Parent(BuildAmmoPickup(new Vector3(p.x, p.y, 0f)));

            // summit
            Parent(BuildSummit(new Vector3(lv.summit.x, lv.summit.y, 0f), manager));

            // parallax ridges
            float[] factors = { 0.60f, 0.75f, 0.88f };
            float[] depths = { 12f, 20f, 30f };
            for (int r = 0; r < 3; r++)
            {
                var ridge = BuildRidge(r, depths[r], MaterialFactory.Unlit(Palette.Ridges[r]));
                var pl = ridge.AddComponent<ParallaxLayer>();
                pl.cam = builtCamera.transform;
                pl.factor = factors[r];
                Parent(ridge);
            }

            // retarget persistent systems onto the new content
            follow.target = player.transform;
            hud.ammo = ammo;
            hud.gun = gun;
            hud.levelName = lv.name;
            hud.bannerText = "";
            manager.ResetWin();
        }

        void Parent(GameObject go) => go.transform.SetParent(levelRoot.transform, true);

        void OnLevelComplete()
        {
            if (currentLevel + 1 < levels.Length)
                StartCoroutine(NextLevelRoutine());
            else
                hud.bannerText = "YOU CONQUERED THE MOUNTAIN";
        }

        IEnumerator NextLevelRoutine()
        {
            hud.bannerText = "LEVEL COMPLETE";
            yield return new WaitForSecondsRealtime(levelBannerSeconds);
            BuildLevel(currentLevel + 1);
        }

        // ---------- shared builders ----------

        GameObject Visual(PrimitiveType type, Transform parent, Vector3 lpos, Vector3 lscale,
            Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = lscale;
            var r = go.GetComponent<MeshRenderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            return go;
        }

        PlayerBody BuildPlayer(Vector3 pos)
        {
            var root = new GameObject("Player");
            root.transform.position = pos;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 1.5f;

            var col = root.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.direction = 1; // Y axis

            var body = root.AddComponent<PlayerBody>();

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            Visual(PrimitiveType.Capsule, visual.transform, Vector3.zero, Vector3.one, bodyMat, "BodyMesh");
            Visual(PrimitiveType.Sphere, visual.transform, new Vector3(0f, 0.95f, 0f), Vector3.one * 0.62f, headMat, "Head");

            playerJuice = root.AddComponent<PlayerJuice>();
            playerJuice.visual = visual.transform;

            return body;
        }

        GameObject BuildGround(Vector3 pos, Vector3 size)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = pos;
            ground.transform.localScale = size;
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            return ground;
        }

        GunController BuildGun(PlayerBody body, GunConfig cfg)
        {
            var pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            var arm = Visual(PrimitiveType.Cylinder, pivot.transform,
                new Vector3(0.45f, 0f, 0f), new Vector3(0.12f, 0.45f, 0.12f), limbMat, "Arm");
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            var parts = new GameObject("GunParts");
            parts.transform.SetParent(pivot.transform, false);

            Visual(PrimitiveType.Cube, parts.transform, new Vector3(0.9f, 0f, 0f), new Vector3(0.9f, 0.22f, 0.24f), gunMetalMat, "Receiver");
            Visual(PrimitiveType.Cube, parts.transform, new Vector3(0.55f, -0.12f, 0f), new Vector3(0.35f, 0.28f, 0.22f), gunWoodMat, "Stock");
            var barrel = Visual(PrimitiveType.Cylinder, parts.transform, new Vector3(1.6f, 0.03f, 0f), new Vector3(0.09f, 0.55f, 0.09f), gunMetalMat, "Barrel");
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Visual(PrimitiveType.Cube, parts.transform, new Vector3(1.15f, -0.14f, 0f), new Vector3(0.3f, 0.14f, 0.18f), gunMetalMat, "Pump");
            Visual(PrimitiveType.Sphere, parts.transform, new Vector3(2.15f, 0.03f, 0f), Vector3.one * 0.14f, muzzleMat, "MuzzleTip");

            var recoil = parts.AddComponent<GunRecoilAnim>();

            var gun = body.gameObject.AddComponent<GunController>();
            gun.gunPivot = pivot.transform;
            gun.playerBody = body;
            gun.ammo = ammo;
            gun.Configure(cfg);

            recoil.gun = gun;
            if (playerJuice != null) playerJuice.gun = gun;
            return gun;
        }

        GameObject BuildRidge(int index, float depth, Material mat)
        {
            var go = new GameObject($"Ridge_{index}");
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var rng = new System.Random(backgroundSeed + index * 7);
            int cols = 16;
            float width = 120f + index * 40f;
            float baseY = -12f - index * 4f;
            float maxH = 18f - index * 3f;
            float step = width / cols;
            float x0 = -width / 2f;

            float[] topY = new float[cols + 1];
            for (int c = 0; c <= cols; c++)
                topY[c] = baseY + maxH * (0.4f + 0.6f * (float)rng.NextDouble());

            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int c = 0; c < cols; c++)
            {
                float xa = x0 + c * step, xb = x0 + (c + 1) * step;
                int vi = verts.Count;
                verts.Add(new Vector3(xa, baseY, 0f));
                verts.Add(new Vector3(xb, baseY, 0f));
                verts.Add(new Vector3(xa, topY[c], 0f));
                verts.Add(new Vector3(xb, topY[c + 1], 0f));
                // double-sided so the silhouette is visible regardless of culling
                tris.Add(vi + 0); tris.Add(vi + 2); tris.Add(vi + 1);
                tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
                tris.Add(vi + 0); tris.Add(vi + 1); tris.Add(vi + 2);
                tris.Add(vi + 1); tris.Add(vi + 3); tris.Add(vi + 2);
            }

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mf.sharedMesh = mesh;

            go.transform.position = new Vector3(0f, 6f, depth);
            return go;
        }

        Mesh MakeQuadMesh(float width, float height)
        {
            float hw = width / 2f, hh = height / 2f;
            var mesh = new Mesh();
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-hw, -hh, 0f), new Vector3(hw, -hh, 0f),
                new Vector3(-hw,  hh, 0f), new Vector3(hw,  hh, 0f),
            });
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
            });
            mesh.SetTriangles(new List<int> { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 }, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        Texture2D MakeVerticalGradient(Color bottom, Color top, int height)
        {
            var tex = new Texture2D(1, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                float t = height > 1 ? (float)y / (height - 1) : 0f;
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            tex.Apply();
            return tex;
        }

        GameObject BuildAmmoPickup(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AmmoPickup";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.6f;
            go.GetComponent<Collider>().isTrigger = true;
            go.GetComponent<MeshRenderer>().sharedMaterial = ammoMat;
            var pickup = go.AddComponent<AmmoPickup>();
            pickup.amount = pickupRefill;
            go.AddComponent<PickupSpin>();
            return go;
        }

        void BuildLedges(Vector2[] ledges, Transform parent)
        {
            if (ledges == null) return;
            foreach (var l in ledges)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Ledge";
                ledge.transform.position = new Vector3(l.x, l.y, 0f);
                ledge.transform.localScale = new Vector3(2.5f, 0.5f, 4f);
                ledge.transform.SetParent(parent, true);
                ledge.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            }
        }

        GameObject BuildSummit(Vector3 pos, GameManager gm)
        {
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Summit";
            flag.transform.position = pos;
            flag.transform.localScale = new Vector3(1.5f, 3f, 4f);
            flag.GetComponent<Collider>().isTrigger = true;
            var mr = flag.GetComponent<MeshRenderer>();
            mr.sharedMaterial = summitMat;

            var trigger = flag.AddComponent<SummitTrigger>();
            trigger.gameManager = gm;

            var pulse = flag.AddComponent<SummitPulse>();
            pulse.target = mr;
            pulse.emission = Palette.Summit;
            return flag;
        }

        void BuildMuzzleFlash(GunController gun)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(gun.gunPivot, false);
            go.transform.localPosition = new Vector3(2.15f, 0.03f, 0f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.15f;
            main.startSpeed = 6f;
            main.startSize = 0.3f;
            main.startColor = Palette.Muzzle;
            var emission = ps.emission;
            emission.enabled = false;
            ps.Stop();

            gun.OnFired += () => ps.Emit(12);
        }
    }
}
