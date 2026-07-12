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

        public PlayerBody Player { get; private set; }

        void Awake()
        {
            Physics.gravity = new Vector3(0f, gravityY, 0f);
            Player = BuildPlayer(new Vector3(0f, 2f, 0f));
            BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 4f));
            BuildCamera(Player.transform);
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
    }
}
