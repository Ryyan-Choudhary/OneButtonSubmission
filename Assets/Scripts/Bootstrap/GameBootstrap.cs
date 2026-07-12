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
        Material rockMat, groundMat;
        Material gunMetalMat, gunWoodMat, muzzleMat, ammoMat, laserMat;
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
            levels = new[] { LevelConfig.Foothills(), LevelConfig.TheSpire(), LevelConfig.TheDoubleCross() };

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

        public void BeginGame() => BuildLevel(0);

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
            rockMat     = MaterialFactory.Lit(Palette.Rock, 0.12f);
            groundMat   = MaterialFactory.Lit(Palette.Ground, 0.10f);
            gunMetalMat = MaterialFactory.Lit(Palette.GunMetal, 0.45f, 0.6f);
            gunWoodMat  = MaterialFactory.Lit(Palette.GunWood, 0.25f);
            muzzleMat   = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.0f);
            ammoMat     = MaterialFactory.Emissive(Palette.Ammo, Palette.Ammo, 1.6f);
            laserMat    = MaterialFactory.Unlit(Palette.Laser);

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

            // the gun IS the player: a free-tumbling rigidbody, no character
            var gun = BuildGunPlayer(new Vector3(lv.startPos.x, lv.startPos.y, 0f), lv.gun);
            gun.transform.SetParent(levelRoot.transform, true);

            // terrain: the ground spans exactly wall-to-wall, so the arena
            // edge is where the world visibly ends
            float midX = (lv.wallLeft + lv.wallRight) * 0.5f;
            var ground = BuildGround(new Vector3(midX, -0.5f, 0f),
                new Vector3(lv.wallRight - lv.wallLeft, 1f, 4f));
            ground.transform.SetParent(levelRoot.transform, true);
            BuildShelves(lv.shelves, levelRoot.transform);
            BuildBounds(lv, levelRoot.transform);

            // pickups (one near the start plus the route ones)
            Parent(BuildAmmoPickup(new Vector3(lv.startPos.x + 3f, lv.startPos.y - 1f, 0f)));
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
            follow.target = gun.transform;
            hud.ammo = ammo;
            hud.gun = gun;
            hud.levelNumber = index + 1;
            hud.bannerText = "";
            manager.ResetWin();

            // softlock watchdog: still + out of shells for 2s -> offer retry
            var retry = gun.gameObject.AddComponent<StuckRetry>();
            retry.body = gun.GetComponent<Rigidbody>();
            retry.ammo = ammo;
            retry.hud = hud;
            retry.onRetry = () => BuildLevel(currentLevel);
        }

        void Parent(GameObject go) => go.transform.SetParent(levelRoot.transform, true);

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

        GameObject BuildGround(Vector3 pos, Vector3 size)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = pos;
            ground.transform.localScale = size;
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            return ground;
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

        void BuildShelves(LevelConfig.Shelf[] shelves, Transform parent)
        {
            if (shelves == null) return;
            foreach (var s in shelves)
            {
                var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelf.name = "Shelf";
                shelf.transform.position = new Vector3(s.cx, s.cy, 0f);
                shelf.transform.localScale = new Vector3(s.w, s.h, 4f);
                shelf.transform.SetParent(parent, true);
                shelf.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            }
        }

        /// Invisible colliders boxing the arena in: left/right walls + roof.
        /// The gun's bouncy material means these double as bounce surfaces,
        /// and the laser visibly stops on them so players can read the edge.
        void BuildBounds(LevelConfig lv, Transform parent)
        {
            float midX = (lv.wallLeft + lv.wallRight) * 0.5f;
            MakeWall("WallLeft", new Vector3(lv.wallLeft - 0.5f, lv.ceiling * 0.5f, 0f),
                new Vector3(1f, lv.ceiling + 40f, 6f), parent);
            MakeWall("WallRight", new Vector3(lv.wallRight + 0.5f, lv.ceiling * 0.5f, 0f),
                new Vector3(1f, lv.ceiling + 40f, 6f), parent);
            MakeWall("Roof", new Vector3(midX, lv.ceiling + 0.5f, 0f),
                new Vector3(lv.wallRight - lv.wallLeft + 2f, 1f, 6f), parent);
        }

        void MakeWall(string name, Vector3 pos, Vector3 size, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
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