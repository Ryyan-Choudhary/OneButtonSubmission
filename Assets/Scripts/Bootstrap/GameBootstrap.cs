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
    /// (the player-gun, terrain, pickups, summit, ridges) is torn down and rebuilt
    /// as you progress. Twilight/dusk palette.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World")]
        public int backgroundSeed = 20260712;

        [Header("Camera")]
        public float cameraSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 2.5f, -20f); // pulled back: canyon crossings need room to read

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
        Material gunMetalMat, gunWoodMat, muzzleMat, ammoMat, laserMat;
        Material facadeMat, windowWarmMat, windowCoolMat, windowGlassMat, brokenMat;
        Material balconyMat, railMat;
        PhysicsMaterial gunPhysMat;

        // persistent rig
        Camera builtCamera;
        CameraFollow follow;
        Material skyMat;
        HudController hud;
        GameManager manager;

        // per-level
        GameObject levelRoot;
        AmmoSystemBehaviour ammo;

        void Awake()
        {
            levels = new[] { LevelConfig.Level1(), LevelConfig.Level2(), LevelConfig.Level3() };

            BuildMaterials();
            BuildLights();
            BuildCamera();
            BuildSky(builtCamera);
            hud = BuildHud();
            manager = gameObject.AddComponent<GameManager>();
            manager.OnWin += OnLevelComplete;

            ApplyAtmosphere(Palette.Ambient, Palette.SkyBottom, Palette.SkyTop);
            TitleFlow.Create(this);
        }

        bool pendingIntro;

        public void BeginGame()
        {
            pendingIntro = true; // the suitcase cutscene plays once, not on retries
            BuildLevel(0);
        }

        public Camera TitleCamera => builtCamera;

        public void ApplyAtmosphere(Color ambient, Color skyBottom, Color skyTop)
        {
            RenderSettings.ambientLight = ambient;
            builtCamera.backgroundColor = skyBottom;
            skyMat.mainTexture = MakeVerticalGradient(skyBottom, skyTop, 256);
        }

        // ---------- persistent build ----------

        void BuildMaterials()
        {
            gunMetalMat = MaterialFactory.Lit(Palette.GunMetal, 0.45f, 0.6f);
            gunWoodMat  = MaterialFactory.Lit(Palette.GunWood, 0.25f);
            muzzleMat   = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.0f);
            ammoMat     = MaterialFactory.Emissive(Palette.Ammo, Palette.Ammo, 1.6f);
            laserMat    = MaterialFactory.Unlit(Palette.Laser);

            // the flanking towers: in-focus facade (unlike the hazy skyline),
            // lit windows in the game's warm/cool accents, dark glass panes
            facadeMat      = MaterialFactory.Lit(new Color(0.13f, 0.13f, 0.19f), 0.25f);
            windowWarmMat  = MaterialFactory.Emissive(new Color(1f, 0.72f, 0.25f), new Color(1f, 0.72f, 0.25f), 1.3f);
            windowCoolMat  = MaterialFactory.Emissive(new Color(0.30f, 0.75f, 1f), new Color(0.30f, 0.75f, 1f), 1.1f);
            windowGlassMat = MaterialFactory.Lit(new Color(0.10f, 0.14f, 0.20f), 0.9f, 0.6f);
            brokenMat      = MaterialFactory.Unlit(new Color(0.02f, 0.02f, 0.03f));
            balconyMat     = MaterialFactory.Lit(new Color(0.20f, 0.20f, 0.26f), 0.2f);  // concrete slab
            railMat        = MaterialFactory.Lit(new Color(0.08f, 0.08f, 0.11f), 0.5f, 0.5f); // dark metal

            // landing feel: bounce + skitter instead of a dead stop
            gunPhysMat = new PhysicsMaterial("GunClatter")
            {
                dynamicFriction = 0.35f,
                staticFriction = 0.45f,
                bounciness = 0.35f,
                bounceCombine = PhysicsMaterialCombine.Maximum,
                frictionCombine = PhysicsMaterialCombine.Average,
            };
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
            ApplyAtmosphere(lv.ambient, lv.skyBottom, lv.skyTop);
            TintArchitecture(lv);

            // the gun IS the player. It enters by bursting out of the right
            // tower's glass, already airborne — there is no floor to rest on.
            var gun = BuildGunPlayer(new Vector3(lv.wallRight - 1.4f, lv.burstHeight, 0f), lv.gun);
            gun.transform.SetParent(levelRoot.transform, true);
            var gunRb = gun.GetComponent<Rigidbody>();
            bool intro = pendingIntro && index == 0;
            pendingIntro = false;

            // terrain: balconies over a fatal drop — no floor
            BuildShelves(lv, levelRoot.transform);
            BuildWallTowers(lv, levelRoot.transform);
            BuildRoof(lv, levelRoot.transform);

            if (lv.routePickups != null)
                foreach (var p in lv.routePickups)
                    Parent(BuildAmmoPickup(new Vector3(p.x, p.y, 0f)));

            // summit: landing anywhere on the final shelf wins; the flag is a beacon
            Parent(BuildSummit(lv, lv.shelves[lv.shelves.Length - 1], manager, hud));

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

            // city skyline — two parallax layers behind the ridges
            // colours shift per-level to stay in the existing sky palette
            BuildCityLayers(index, levelRoot.transform);

            // retarget persistent systems onto the new content
            follow.minY = 3f; // the camera never chases the gun into the drop
            if (intro)
            {
                // the suitcase cutscene owns the gun and camera until it
                // hands over via LaunchEntry. Continuous CD is illegal on
                // kinematic bodies, so drop to speculative for the ride.
                gunRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                gunRb.isKinematic = true;
                gun.enabled = false;
                var cin = new GameObject("Level1Intro").AddComponent<Level1Intro>();
                cin.gun = gun;
                cin.follow = follow;
                cin.cam = builtCamera;
                cin.wallRight = lv.wallRight;
                cin.burstHeight = lv.burstHeight;
                cin.seed = backgroundSeed;
                cin.onLaunch = () => LaunchEntry(gun);
                cin.BuildAndPlay();
            }
            else
            {
                LaunchEntry(gun);
            }
            hud.ammo = ammo;
            hud.gun = gun;
            hud.levelNumber = index + 1;
            hud.bannerText = "";
            manager.ResetWin();

            // retry watchdog: stuck with no shells, or fallen out of the world
            var retry = gun.gameObject.AddComponent<StuckRetry>();
            retry.body = gunRb;
            retry.ammo = ammo;
            retry.hud = hud;
            retry.gun = gun;
            retry.fallY = -12f;
            retry.onRetry = () => BuildLevel(currentLevel);
        }

        void Parent(GameObject go) => go.transform.SetParent(levelRoot.transform, true);

        /// The standard level entry: the gun smashes out of the right tower's
        /// window, already flying. Also the handoff point after the intro, so
        /// the kinematic -> dynamic switch is done strictly in order: wake the
        /// body first, teleport in physics space, THEN write the velocities —
        /// otherwise the launch impulse can be swallowed by the transition and
        /// the gun drops straight into the void.
        void LaunchEntry(GunController gun)
        {
            var lv = levels[currentLevel];
            var rb = gun.GetComponent<Rigidbody>();
            Vector3 spawn = new Vector3(lv.wallRight - 1.4f, lv.burstHeight, 0f);

            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            gun.transform.SetPositionAndRotation(spawn, Quaternion.identity);
            rb.position = spawn;
            rb.rotation = Quaternion.identity;
            rb.WakeUp();
            rb.linearVelocity = new Vector3(-12f, 3.5f, 0f);
            rb.angularVelocity = new Vector3(0f, 0f, 6f);
            gun.enabled = true;
            GlassBurst.Spawn(new Vector3(lv.wallRight - 0.4f, lv.burstHeight, 0f),
                Vector3.left, backgroundSeed ^ currentLevel);
            follow.target = gun.transform;
            follow.SnapToTarget();
        }

        /// Nudges the tower/balcony palette toward the level's sky so the
        /// architecture sits in the same light as the backdrop.
        void TintArchitecture(LevelConfig lv)
        {
            facadeMat.color      = Color.Lerp(new Color(0.13f, 0.13f, 0.19f), lv.skyBottom, 0.35f);
            balconyMat.color     = Color.Lerp(new Color(0.20f, 0.20f, 0.26f), lv.skyBottom, 0.30f);
            windowGlassMat.color = Color.Lerp(new Color(0.10f, 0.14f, 0.20f), lv.skyBottom, 0.40f);
            railMat.color        = Color.Lerp(new Color(0.08f, 0.08f, 0.11f), lv.skyBottom, 0.25f);
        }

        /// Adds two procedural city skyline layers (far + mid) behind the mountain ridges.
        void BuildCityLayers(int levelIndex, Transform parent)
        {
            var lv = levels[levelIndex];
            Color skyB = lv.skyBottom;

            // Per-level window colour pairs  [warmA, coolB]
            // Level 1 — purple dusk: gold windows + lilac-violet accent
            // Level 2 — midnight blue: teal windows + cobalt accent
            // Level 3 — blood dusk:  amber windows + deep red accent
            Color[][] winColors =
            {
                new[] { new Color(1.00f, 0.80f, 0.28f), new Color(0.60f, 0.35f, 0.90f) }, // L1
                new[] { new Color(0.28f, 0.90f, 0.95f), new Color(0.25f, 0.45f, 1.00f) }, // L2
                new[] { new Color(1.00f, 0.65f, 0.20f), new Color(0.90f, 0.22f, 0.22f) }, // L3
            };
            
            int li = Mathf.Clamp(levelIndex, 0, winColors.Length - 1);
            
            // Atmospheric perspective: Blend window colors with the sky color so they don't pop forward
            Color winA = Color.Lerp(winColors[li][0], skyB, 0.5f);
            Color winB = Color.Lerp(winColors[li][1], skyB, 0.5f);

            // Blend building silhouettes with the sky bottom to simulate atmospheric depth/haze
            // Far layer: 88% sky bottom, 12% silhouette (extremely soft/foggy)
            // Mid layer: 70% sky bottom, 30% silhouette (soft, slightly more contrast)
            Color farSil = Color.Lerp(skyB, new Color(0.05f, 0.04f, 0.09f), 0.12f);
            Color midSil = Color.Lerp(skyB, new Color(0.05f, 0.04f, 0.09f), 0.28f);

            int baseSeed = backgroundSeed ^ (levelIndex * 0x3F17);

            // Far layer — tallest buildings, slowest scroll, deepest Z
            CityBackground.Create(
                parent          : parent,
                parallaxCam     : builtCamera,
                seed            : baseSeed,
                depth           : 48f,
                parallaxFactor  : 0.18f,
                halfWidth       : 150f,
                groundY         : -20f,         // sits deep to cover the starting camera view
                heightRange     : new Vector2(35f, 75f), // Massive skyscrapers
                buildingCount   : 14,
                silhouette      : farSil,
                windowA         : winA,
                windowB         : winB,
                windowDensity   : 0.20f);       // Very sparse lights to prevent busy/noisy backgrounds

            // Mid layer — shorter, denser, slightly faster scroll
            CityBackground.Create(
                parent          : parent,
                parallaxCam     : builtCamera,
                seed            : baseSeed ^ 0x7A3C,
                depth           : 38f,
                parallaxFactor  : 0.28f,
                halfWidth       : 120f,
                groundY         : -16f,
                heightRange     : new Vector2(18f, 48f), // Solid mid-size towers
                buildingCount   : 18,
                silhouette      : midSil,
                windowA         : winA,
                windowB         : winB,
                windowDensity   : 0.28f);       // Sparse lights
        }

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

        GunController BuildGunPlayer(Vector3 pos, GunConfig cfg)
        {
            var root = new GameObject("Gun");
            root.transform.position = pos;

            var rb = root.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.2f; // spin keeps its momentum in the air

            // compound collider: frame box + angled grip box. The grip props up
            // the rear, so at rest the pistol tips nose-down (~20°) onto grip +
            // muzzle — which points the barrel downward and makes the first
            // shot launch UP instead of sliding along the ground.
            var frameCol = root.AddComponent<BoxCollider>();
            frameCol.center = new Vector3(0.11f, 0.07f, 0f);
            frameCol.size = new Vector3(1.34f, 0.34f, 0.22f);
            frameCol.material = gunPhysMat;

            var gripColGo = new GameObject("GripCollider");
            gripColGo.transform.SetParent(root.transform, false);
            gripColGo.transform.localPosition = new Vector3(-0.38f, -0.3f, 0f);
            gripColGo.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            var gripCol = gripColGo.AddComponent<BoxCollider>();
            gripCol.size = new Vector3(0.28f, 0.6f, 0.2f);
            gripCol.material = gunPhysMat;

            var body = root.AddComponent<GunBody>();

            ammo = root.AddComponent<AmmoSystemBehaviour>();
            ammo.maxShells = maxShells;
            ammo.startShells = startShells;

            // pistol visuals, centered on the rigidbody so the tumble spins about the middle
            var parts = new GameObject("GunParts");
            parts.transform.SetParent(root.transform, false);

            Visual(PrimitiveType.Cube, parts.transform, new Vector3(0.05f, 0.14f, 0f), new Vector3(1.0f, 0.2f, 0.24f), gunMetalMat, "Slide");
            Visual(PrimitiveType.Cube, parts.transform, new Vector3(0.05f, -0.01f, 0f), new Vector3(0.95f, 0.12f, 0.22f), gunMetalMat, "Frame");
            var barrel = Visual(PrimitiveType.Cylinder, parts.transform, new Vector3(0.62f, 0.12f, 0f), new Vector3(0.07f, 0.18f, 0.07f), gunMetalMat, "Barrel");
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Visual(PrimitiveType.Sphere, parts.transform, new Vector3(0.82f, 0.12f, 0f), Vector3.one * 0.12f, muzzleMat, "MuzzleTip");
            var grip = Visual(PrimitiveType.Cube, parts.transform, new Vector3(-0.38f, -0.3f, 0f), new Vector3(0.26f, 0.55f, 0.2f), gunWoodMat, "Grip");
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Visual(PrimitiveType.Cube, parts.transform, new Vector3(-0.1f, -0.16f, 0f), new Vector3(0.3f, 0.08f, 0.12f), gunMetalMat, "TriggerGuard");
            Visual(PrimitiveType.Cube, parts.transform, new Vector3(-0.48f, 0.2f, 0f), new Vector3(0.12f, 0.14f, 0.16f), gunMetalMat, "Hammer");

            var recoil = parts.AddComponent<GunRecoilAnim>();

            // laser sight: anchored to the root (not parts) so the cosmetic
            // recoil kick doesn't wobble the beam
            var laserGo = new GameObject("AimLaser");
            laserGo.transform.SetParent(root.transform, false);
            var laser = laserGo.AddComponent<AimLaser>();
            laser.gunRoot = root.transform;
            laser.material = laserMat;

            var gun = root.AddComponent<GunController>();
            gun.body = body;
            gun.ammo = ammo;
            gun.Configure(cfg);
            recoil.gun = gun;

            BuildMuzzleFlash(gun, parts.transform);
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

        /// Every landing surface is a balcony now. Wall-touching shelves jut
        /// sideways from the flanking towers (rails on the open end + front);
        /// floating shelves get rails on both ends and a backing building
        /// behind the play plane, so they read as balconies ON that building.
        /// The physics slab is unchanged — rails and towers are cosmetic.
        void BuildShelves(LevelConfig lv, Transform parent)
        {
            if (lv.shelves == null) return;
            var floaters = new List<LevelConfig.Shelf>();
            foreach (var s in lv.shelves)
            {
                var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slab.name = "Balcony";
                slab.transform.position = new Vector3(s.cx, s.cy, 0f);
                slab.transform.localScale = new Vector3(s.w, s.h, 4f);
                slab.transform.SetParent(parent, true);
                slab.GetComponent<MeshRenderer>().sharedMaterial = balconyMat;

                int attach = s.cx - s.w / 2f <= lv.wallLeft + 0.6f ? -1
                           : s.cx + s.w / 2f >= lv.wallRight - 0.6f ? 1 : 0;
                BuildRails(s, attach, parent);
                if (attach == 0) floaters.Add(s);
            }
            BuildBackingTowers(lv, floaters, parent);
        }

        void BuildRails(LevelConfig.Shelf s, int attach, Transform parent)
        {
            float top = s.cy + s.h * 0.5f;
            float xL = s.cx - s.w / 2f, xR = s.cx + s.w / 2f;

            // front edge (camera side) always has a rail
            RailRun(new Vector3(xL, top, -1.86f), s.w, true, parent);
            // open ends get rails; the tower-attached end does not
            if (attach != -1) RailRun(new Vector3(xL + 0.1f, top, -1.86f), 3.72f, false, parent);
            if (attach != 1) RailRun(new Vector3(xR - 0.1f, top, -1.86f), 3.72f, false, parent);
        }

        /// A straight railing: top bar, mid bar, and evenly spaced posts.
        void RailRun(Vector3 start, float length, bool alongX, Transform parent)
        {
            const float railH = 1.0f;
            Vector3 dir = alongX ? Vector3.right : Vector3.forward;
            Vector3 mid = start + dir * (length * 0.5f);
            Vector3 barScale = alongX
                ? new Vector3(length, 0.09f, 0.09f)
                : new Vector3(0.09f, 0.09f, length);

            Visual(PrimitiveType.Cube, parent, mid + Vector3.up * railH, barScale, railMat, "RailTop");
            Visual(PrimitiveType.Cube, parent, mid + Vector3.up * (railH * 0.55f), barScale, railMat, "RailMid");

            int posts = Mathf.Max(2, Mathf.RoundToInt(length / 1.6f) + 1);
            float step = length / (posts - 1);
            for (int i = 0; i < posts; i++)
                Visual(PrimitiveType.Cube, parent,
                    start + dir * (i * step) + Vector3.up * (railH * 0.5f),
                    new Vector3(0.07f, railH, 0.07f), railMat, "RailPost");
        }

        /// One building behind each cluster of floating balconies. Neighboring
        /// balconies share a building (the center stone ladder becomes one
        /// mid-rise with a balcony per floor); a lone low pad becomes the
        /// rooftop terrace of a short building rising from the drop.
        void BuildBackingTowers(LevelConfig lv, List<LevelConfig.Shelf> floaters, Transform parent)
        {
            if (floaters.Count == 0) return;
            floaters.Sort((a, b) => (a.cx - a.w / 2f).CompareTo(b.cx - b.w / 2f));

            var cluster = new List<LevelConfig.Shelf> { floaters[0] };
            float clusterRight = floaters[0].cx + floaters[0].w / 2f;
            for (int i = 1; i <= floaters.Count; i++)
            {
                bool flush = i == floaters.Count
                    || floaters[i].cx - floaters[i].w / 2f > clusterRight + 3f;
                if (!flush)
                {
                    cluster.Add(floaters[i]);
                    clusterRight = Mathf.Max(clusterRight, floaters[i].cx + floaters[i].w / 2f);
                    continue;
                }

                BuildBackingTower(lv, cluster, parent);
                if (i < floaters.Count)
                {
                    cluster = new List<LevelConfig.Shelf> { floaters[i] };
                    clusterRight = floaters[i].cx + floaters[i].w / 2f;
                }
            }
        }

        void BuildBackingTower(LevelConfig lv, List<LevelConfig.Shelf> cluster, Transform parent)
        {
            float xMin = float.MaxValue, xMax = float.MinValue, topShelf = float.MinValue;
            foreach (var s in cluster)
            {
                xMin = Mathf.Min(xMin, s.cx - s.w / 2f);
                xMax = Mathf.Max(xMax, s.cx + s.w / 2f);
                topShelf = Mathf.Max(topShelf, s.cy + s.h / 2f);
            }
            xMin -= 2f; xMax += 2f;
            float bottom = -34f, top = topShelf + 2.5f; // parapet just above the highest balcony

            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "BackTower";
            slab.transform.position = new Vector3((xMin + xMax) * 0.5f, (bottom + top) * 0.5f, 6.2f);
            slab.transform.localScale = new Vector3(xMax - xMin, top - bottom, 8f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = facadeMat;
            slab.transform.SetParent(parent, true);

            // a lit door behind each balcony, window grid everywhere else
            foreach (var s in cluster)
                Visual(PrimitiveType.Cube, parent,
                    new Vector3(s.cx, s.cy + s.h / 2f + 1.15f, 2.16f),
                    new Vector3(1.6f, 2.3f, 0.08f), windowWarmMat, "BalconyDoor");

            var rng = new System.Random(backgroundSeed ^ (int)(xMin * 17f));
            for (float y = -6f; y <= top - 2.5f; y += 4f)
            {
                for (float x = xMin + 2f; x <= xMax - 2f; x += 3.5f)
                {
                    bool nearDoor = false;
                    foreach (var s in cluster)
                        if (Mathf.Abs(x - s.cx) < 2f && Mathf.Abs(y - (s.cy + s.h / 2f + 1.15f)) < 2.4f)
                            nearDoor = true;
                    if (nearDoor) continue;
                    PlaceWindow(new Vector3(x, y, 2.14f), new Vector3(1.5f, 1.6f, 0.08f), rng, parent);
                }
            }
        }

        /// The arena's side walls are real skyscrapers now: solid, in-focus
        /// towers whose inner faces sit exactly where the old invisible walls
        /// were. The gun bounces off them, the laser lands on them, and the
        /// balcony shelves visually jut out of their facades. They flank the
        /// play space, so they never cover the action.
        void BuildWallTowers(LevelConfig lv, Transform parent)
        {
            BuildTower(lv, lv.wallLeft, -1f, parent, false);
            BuildTower(lv, lv.wallRight, +1f, parent, true); // entry side: broken window
        }

        void BuildTower(LevelConfig lv, float wallX, float side, Transform parent, bool entrySide)
        {
            float bottom = -34f, top = lv.ceiling + 16f;
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = side > 0f ? "TowerRight" : "TowerLeft";
            slab.transform.position = new Vector3(wallX + side * 4f, (bottom + top) * 0.5f, 2f);
            slab.transform.localScale = new Vector3(8f, top - bottom, 8f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = facadeMat;
            slab.transform.SetParent(parent, true);

            // windows over the play band: a mix of lit (warm/cool) and dark
            // glass, on the inner face and the camera-facing face
            var rng = new System.Random(backgroundSeed ^ (int)(wallX * 31f));
            for (float y = -6f; y <= lv.ceiling + 4f; y += 4f)
            {
                PlaceWindow(new Vector3(wallX - side * 0.09f, y, -0.4f),
                    new Vector3(0.08f, 1.6f, 1.5f), rng, parent);
                PlaceWindow(new Vector3(wallX - side * 0.09f, y, 2.4f),
                    new Vector3(0.08f, 1.6f, 1.5f), rng, parent);
                PlaceWindow(new Vector3(wallX + side * 2f, y, -2.05f),
                    new Vector3(1.5f, 1.6f, 0.08f), rng, parent);
                PlaceWindow(new Vector3(wallX + side * 5.6f, y, -2.05f),
                    new Vector3(1.5f, 1.6f, 0.08f), rng, parent);
            }

            // the hole the gun smashed out of
            if (entrySide)
            {
                var hole = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hole.name = "BrokenWindow";
                Destroy(hole.GetComponent<Collider>());
                hole.transform.position = new Vector3(wallX - side * 0.06f, lv.burstHeight, 0f);
                hole.transform.localScale = new Vector3(0.12f, 2.3f, 2.3f);
                hole.GetComponent<MeshRenderer>().sharedMaterial = brokenMat;
                hole.transform.SetParent(parent, true);
            }
        }

        void PlaceWindow(Vector3 pos, Vector3 size, System.Random rng, Transform parent)
        {
            double roll = rng.NextDouble();
            Material mat = roll < 0.28 ? windowWarmMat
                         : roll < 0.48 ? windowCoolMat
                         : windowGlassMat;
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = "Window";
            Destroy(w.GetComponent<Collider>());
            w.transform.position = pos;
            w.transform.localScale = size;
            w.GetComponent<MeshRenderer>().sharedMaterial = mat;
            w.transform.SetParent(parent, true);
        }

        /// Only the roof stays invisible — the sides are towers, the bottom is a drop.
        void BuildRoof(LevelConfig lv, Transform parent)
        {
            var go = new GameObject("Roof");
            go.transform.position = new Vector3(
                (lv.wallLeft + lv.wallRight) * 0.5f, lv.ceiling + 0.5f, 0f);
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(lv.wallRight - lv.wallLeft + 18f, 1f, 8f);
            go.transform.SetParent(parent, true);
        }

        GameObject BuildSummit(LevelConfig lv, LevelConfig.Shelf finalShelf, GameManager gm, HudController hud)
        {
            var root = new GameObject("Summit");
            float shelfTop = finalShelf.cy + finalShelf.h * 0.5f;
            float faceIn = lv.summit.x > 0.5f ? -1f : 1f; // face into the canyon

            // the real agent — his kneel-and-fire cutscene is what actually
            // grants the win
            var agentGo = new GameObject("Agent");
            agentGo.transform.position = new Vector3(lv.summit.x, shelfTop, 0f);
            var agent = agentGo.AddComponent<SummitAgent>();
            agent.facing = faceIn;
            agent.villain = false;
            agent.Build();
            // Floating name tag above James Bond
            var agentLabel = agentGo.AddComponent<CharacterLabel>();
            agentLabel.labelText = "James Bond";
            agentLabel.worldYOffset = 7.5f;
            agentGo.transform.SetParent(root.transform, true);

            // the rival: posted on the platform just before the final one —
            // an obstacle you pass on the way up. He's decorative — reaching
            // the final shelf always resolves through the real agent below,
            // never through him.
            if (lv.villainAtSummit && lv.shelves != null && lv.shelves.Length >= 2)
            {
                var villainShelf = lv.shelves[lv.shelves.Length - 2];
                float villainTop = villainShelf.cy + villainShelf.h * 0.5f;
                var villainGo = new GameObject("Villain");
                villainGo.transform.position = new Vector3(villainShelf.cx, villainTop, 0f);
                var villain = villainGo.AddComponent<SummitAgent>();
                villain.facing = villainShelf.cx > lv.summit.x ? -1f : 1f; // face back toward the summit
                villain.villain = true;
                villain.Build();
                // Floating name tag above the villain
                var villainLabel = villainGo.AddComponent<CharacterLabel>();
                villainLabel.labelText = "Bad Guy";
                villainLabel.worldYOffset = 7.5f;

                // The villain root has negative X scale (facing mirror), which makes
                // BoxCollider complain. Put the trigger on a world-space child so it
                // always has a clean positive scale regardless of facing direction.
                var theftGo = new GameObject("VillainBody");
                theftGo.transform.SetParent(root.transform, false); // world-space, no scale inheritance
                theftGo.transform.position = new Vector3(villainShelf.cx, villainTop + 3.0f, 0f);
                var theftBox = theftGo.AddComponent<BoxCollider>();
                theftBox.size = new Vector3(3.5f, 6.5f, 4f); // wide + tall to catch any trajectory
                theftBox.isTrigger = true;
                var theft = theftGo.AddComponent<VillainTrigger>();
                theft.villain = villain;
                theft.hud = hud;
                theft.onRetry = () => BuildLevel(currentLevel);

                villainGo.transform.SetParent(root.transform, true);
            }

            // win volume: the airspace over the entire final shelf, so any
            // landing (or low flyby) on it counts as reaching the goal
            var win = new GameObject("WinVolume");
            win.transform.position = new Vector3(finalShelf.cx, shelfTop + 1.5f, 0f);
            var box = win.AddComponent<BoxCollider>();
            box.size = new Vector3(finalShelf.w, 3f, 4f);
            box.isTrigger = true;
            var trigger = win.AddComponent<SummitTrigger>();
            trigger.gameManager = gm;
            trigger.agent = agent;
            win.transform.SetParent(root.transform, true);

            return root;
        }

        void BuildMuzzleFlash(GunController gun, Transform parent)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.82f, 0.12f, 0f);

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