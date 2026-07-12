using UnityEngine;
using OneButtonSubmission.Components;

namespace OneButtonSubmission.Bootstrap
{
    /// Builds the whole playable scene at runtime so pressing Play is reproducible.
    /// Extended in later tasks (camera, gun, pickups, level, HUD).
    public class GameBootstrap : MonoBehaviour
    {
        [Header("World")]
        public float gravityY = -20f;

        [Header("Camera")]
        public float cameraSmoothTime = 0.25f;
        public Vector3 cameraOffset = new Vector3(0f, 1.5f, -12f);

        [Header("Gun")]
        public float gunSweepSpeed = 120f;
        public float recoilForce = 10f;
        public float cooldownSeconds = 0.6f;

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

        void Awake()
        {
            Physics.gravity = new Vector3(0f, gravityY, 0f);
            Player = BuildPlayer(new Vector3(0f, 2f, 0f));
            BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 4f));
            Ammo = Player.gameObject.AddComponent<AmmoSystemBehaviour>();
            Ammo.maxShells = maxShells;
            Ammo.startShells = startShells;
            Gun = BuildGun(Player);
            BuildCamera(Player.transform);
            BuildAmmoPickup(new Vector3(3f, 1f, 0f));
            Hud = BuildHud(Ammo, Gun);
            Manager = gameObject.AddComponent<GameManager>();
            BuildLedges();
            foreach (var p in routePickups) BuildAmmoPickup(new Vector3(p.x, p.y, 0f));
            BuildSummit(new Vector3(summit.x, summit.y, 0f), Manager);
            Hud.gameManager = Manager;
        }

        PlayerBody BuildPlayer(Vector3 pos)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Player";
            body.transform.position = pos;

            var rb = body.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 1.5f;

            return body.AddComponent<PlayerBody>();
        }

        GameObject BuildGround(Vector3 pos, Vector3 size)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = pos;
            ground.transform.localScale = size;
            return ground;
        }

        GunController BuildGun(PlayerBody body)
        {
            // Pivot lives at the player's "hand"; a barrel box sticks out +X from it.
            var pivot = new GameObject("GunPivot");
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 0.3f, 0f);

            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Barrel";
            Destroy(barrel.GetComponent<Collider>()); // visual only
            barrel.transform.SetParent(pivot.transform, false);
            barrel.transform.localScale = new Vector3(1.2f, 0.2f, 0.2f);
            barrel.transform.localPosition = new Vector3(0.7f, 0f, 0f);

            var gun = body.gameObject.AddComponent<GunController>();
            gun.gunPivot = pivot.transform;
            gun.playerBody = body;
            gun.ammo = Ammo;
            gun.rotationSpeedDegPerSec = gunSweepSpeed;
            gun.recoilForce = recoilForce;
            gun.cooldownSeconds = cooldownSeconds;
            return gun;
        }

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
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = target;
            follow.smoothTime = cameraSmoothTime;
            follow.offset = cameraOffset;
        }

        GameObject BuildAmmoPickup(Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AmmoPickup";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.6f;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var pickup = go.AddComponent<AmmoPickup>();
            pickup.amount = pickupRefill;
            return go;
        }

        HudController BuildHud(AmmoSystemBehaviour ammo, GunController gun)
        {
            var go = new GameObject("HUD");
            var hud = go.AddComponent<HudController>();
            hud.ammo = ammo;
            hud.gun = gun;
            return hud;
        }

        void BuildLedges()
        {
            foreach (var l in ledges)
            {
                var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ledge.name = "Ledge";
                ledge.transform.position = new Vector3(l.x, l.y, 0f);
                ledge.transform.localScale = new Vector3(2.5f, 0.5f, 4f);
            }
        }

        GameObject BuildSummit(Vector3 pos, GameManager manager)
        {
            var flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Summit";
            flag.transform.position = pos;
            flag.transform.localScale = new Vector3(1.5f, 3f, 4f);
            var col = flag.GetComponent<Collider>();
            col.isTrigger = true;
            var trigger = flag.AddComponent<SummitTrigger>();
            trigger.gameManager = manager;
            return flag;
        }
    }
}
