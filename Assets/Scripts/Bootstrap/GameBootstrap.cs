using System.Collections.Generic;
using UnityEngine;
using OneButtonSubmission.Components;
using OneButtonSubmission.Art;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// Builds the whole playable, colored, animated scene at runtime so pressing
    /// Play is reproducible with no manual wiring. Twilight/dusk palette.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World")]
        public float gravityY = -20f;
        public int backgroundSeed = 20260712;

        [Header("Camera")]
        public float cameraSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.5f, -12f);

        [Header("Ammo")]
        public int maxShells = 6;
        public int startShells = 6;

        [Header("Pickups")]
        public int pickupRefill = 3;

        [Header("Level")]
        public Vector2[] ledges = new Vector2[]
        {
            new Vector2(2f, 3f),
            new Vector2(-2f, 6f),
            new Vector2(3f, 9f),
            new Vector2(-1f, 12f),
            new Vector2(2f, 15f),
        };
        public Vector2[] routePickups = new Vector2[]
        {
            new Vector2(-2f, 7f),
            new Vector2(2f, 13f),
        };
        public Vector2 summit = new Vector2(2f, 17f);

        public PlayerBody Player { get; private set; }
        public GunController Gun { get; private set; }
        public AmmoSystemBehaviour Ammo { get; private set; }
        public HudController Hud { get; private set; }
        public GameManager Manager { get; private set; }

        // materials
        Material rockMat, groundMat, bodyMat, headMat, limbMat;
        Material gunMetalMat, gunWoodMat, muzzleMat, ammoMat, summitMat;

        PlayerJuice playerJuice;
        Camera builtCamera;

        void Awake()
        {
            Physics.gravity = new Vector3(0f, gravityY, 0f);
            BuildMaterials();
            BuildLights();

            Player = BuildPlayer(new Vector3(0f, 2f, 0f));
            BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 4f));

            Ammo = Player.gameObject.AddComponent<AmmoSystemBehaviour>();
            Ammo.maxShells = maxShells;
            Ammo.startShells = startShells;

            Gun = BuildGun(Player);
            BuildCamera(Player.transform);
            BuildBackground(builtCamera);

            BuildAmmoPickup(new Vector3(3f, 1f, 0f));
            Hud = BuildHud(Ammo, Gun);

            Manager = gameObject.AddComponent<GameManager>();
            BuildLedges();
            foreach (var p in routePickups) BuildAmmoPickup(new Vector3(p.x, p.y, 0f));
            BuildSummit(new Vector3(summit.x, summit.y, 0f), Manager);
            Hud.gameManager = Manager;

            BuildMuzzleFlash(Gun);
        }

        // ---------- materials ----------

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

        // ---------- shared helper ----------

        /// Creates a collider-less, colored primitive as a visual child.
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

        // ---------- lighting ----------

        void BuildLights()
        {
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) Destroy(l.gameObject);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.Ambient;

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

        // ---------- player ----------

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

        // ---------- shotgun ----------

        GunController BuildGun(PlayerBody body)
        {
            var pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.1f, 0f);

            // Arm sweeps with the gun, reaching from the body out to the receiver.
            var arm = Visual(PrimitiveType.Cylinder, pivot.transform,
                new Vector3(0.45f, 0f, 0f), new Vector3(0.12f, 0.45f, 0.12f), limbMat, "Arm");
            arm.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Gun parts live under a container that recoils on fire.
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
            gun.ammo = Ammo;
            gun.Configure(GunConfig.Blaster());

            recoil.gun = gun;
            if (playerJuice != null) playerJuice.gun = gun;
            return gun;
        }

        // ---------- camera ----------

        void BuildCamera(Transform target)
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
            cam.backgroundColor = Palette.SkyBottom;
            cam.farClipPlane = 150f;

            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = target;
            follow.smoothTime = cameraSmoothTime;
            follow.offset = cameraOffset;

            builtCamera = cam;
        }

        // ---------- background ----------

        void BuildBackground(Camera cam)
        {
            if (cam == null) return;

            // Gradient sky as a double-sided camera child so it always fills the view.
            var tex = MakeVerticalGradient(Palette.SkyBottom, Palette.SkyTop, 256);
            var skyMat = MaterialFactory.UnlitTexture(tex);
            var sky = new GameObject("Sky");
            var skyMf = sky.AddComponent<MeshFilter>();
            var skyMr = sky.AddComponent<MeshRenderer>();
            skyMr.sharedMaterial = skyMat;

            float d = cam.farClipPlane * 0.85f;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            float h = 2f * d * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float w = h * aspect;
            skyMf.sharedMesh = MakeQuadMesh(w * 1.3f, h * 1.3f);
            sky.transform.SetParent(cam.transform, false);
            sky.transform.localPosition = new Vector3(0f, 0f, d);
            sky.transform.localRotation = Quaternion.identity;

            // Parallax mountain ridges behind the play plane.
            float[] factors = { 0.60f, 0.75f, 0.88f };
            float[] depths = { 12f, 20f, 30f };
            for (int i = 0; i < 3; i++)
            {
                var ridge = BuildRidge(i, depths[i], MaterialFactory.Unlit(Palette.Ridges[i]));
                var pl = ridge.AddComponent<ParallaxLayer>();
                pl.cam = cam.transform;
                pl.factor = factors[i];
            }
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
            // double-sided
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

        // ---------- pickups / ledges / summit ----------

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

        void BuildLedges()
        {
            foreach (var l in ledges)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Ledge";
                ledge.transform.position = new Vector3(l.x, l.y, 0f);
                ledge.transform.localScale = new Vector3(2.5f, 0.5f, 4f);
                ledge.GetComponent<MeshRenderer>().sharedMaterial = rockMat;
            }
        }

        GameObject BuildSummit(Vector3 pos, GameManager manager)
        {
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Summit";
            flag.transform.position = pos;
            flag.transform.localScale = new Vector3(1.5f, 3f, 4f);
            flag.GetComponent<Collider>().isTrigger = true;
            var mr = flag.GetComponent<MeshRenderer>();
            mr.sharedMaterial = summitMat;

            var trigger = flag.AddComponent<SummitTrigger>();
            trigger.gameManager = manager;

            var pulse = flag.AddComponent<SummitPulse>();
            pulse.target = mr;
            pulse.emission = Palette.Summit;
            return flag;
        }

        HudController BuildHud(AmmoSystemBehaviour ammo, GunController gun)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudController>();
            hud.ammo = ammo;
            hud.gun = gun;
            return hud;
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
            emission.enabled = false; // emitted manually on fire
            ps.Stop();

            gun.OnFired += () => ps.Emit(12);
        }
    }
}
