using System.Collections.Generic;
using UnityEngine;
using OneButtonSubmission.Art;
using OneButtonSubmission.Components;

namespace OneButtonSubmission.Bootstrap
{
    /// 3D showcase for the title screens: parallax ridges, a floating gun, laser, embers.
    public class TitleScene : MonoBehaviour
    {
        const float ShotInterval = 2.8f;

        Transform gunRoot;
        Transform gunParts;
        LineRenderer laser;
        ParticleSystem muzzleFlash;
        ParticleSystem embers;
        Vector3 gunBasePos;
        float shotTimer;
        float kick;

        public static TitleScene Build(Camera cam, int seed, Transform parent)
        {
            var root = new GameObject("TitleScene").AddComponent<TitleScene>();
            root.transform.SetParent(parent, false);
            root.BuildScene(cam, seed);
            return root;
        }

        void BuildScene(Camera cam, int seed)
        {
            float[] factors = { 0.55f, 0.72f, 0.88f };
            float[] depths = { 14f, 22f, 32f };
            for (int r = 0; r < 3; r++)
            {
                var ridge = BuildRidge(seed, r, depths[r], MaterialFactory.Unlit(Palette.Ridges[r]));
                var pl = ridge.AddComponent<ParallaxLayer>();
                pl.cam = cam.transform;
                pl.factor = factors[r];
                ridge.transform.SetParent(transform, true);
            }

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "TitleGround";
            ground.transform.SetParent(transform, false);
            ground.transform.position = new Vector3(0f, -1.2f, 0f);
            ground.transform.localScale = new Vector3(14f, 0.4f, 4f);
            Object.Destroy(ground.GetComponent<Collider>());
            ground.GetComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Lit(Palette.Ground, 0.10f);

            gunMetalMat = MaterialFactory.Lit(Palette.GunMetal, 0.45f, 0.6f);
            gunWoodMat = MaterialFactory.Lit(Palette.GunWood, 0.25f);
            muzzleMat = MaterialFactory.Emissive(Palette.Muzzle, Palette.Muzzle, 2.0f);
            laserMat = MaterialFactory.Unlit(Palette.Laser);

            gunRoot = new GameObject("ShowcaseGun").transform;
            gunRoot.SetParent(transform, false);
            gunBasePos = new Vector3(0f, 2.2f, 0f);
            gunRoot.position = gunBasePos;

            gunParts = new GameObject("GunParts").transform;
            gunParts.SetParent(gunRoot, false);
            BuildGunVisuals(gunParts);

            var laserGo = new GameObject("TitleLaser");
            laserGo.transform.SetParent(gunRoot, false);
            laser = laserGo.AddComponent<LineRenderer>();
            laser.useWorldSpace = true;
            laser.positionCount = 2;
            laser.startWidth = 0.07f;
            laser.endWidth = 0.02f;
            laser.material = laserMat;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;

            muzzleFlash = BuildMuzzleFlash(gunParts);
            embers = BuildEmbers();
            embers.transform.SetParent(transform, false);

            shotTimer = ShotInterval * 0.65f;
        }

        Material gunMetalMat, gunWoodMat, muzzleMat, laserMat;

        void BuildGunVisuals(Transform parts)
        {
            Visual(PrimitiveType.Cube, parts, new Vector3(0.05f, 0.14f, 0f), new Vector3(1.0f, 0.2f, 0.24f), gunMetalMat);
            Visual(PrimitiveType.Cube, parts, new Vector3(0.05f, -0.01f, 0f), new Vector3(0.95f, 0.12f, 0.22f), gunMetalMat);
            var barrel = Visual(PrimitiveType.Cylinder, parts, new Vector3(0.62f, 0.12f, 0f), new Vector3(0.07f, 0.18f, 0.07f), gunMetalMat);
            barrel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Visual(PrimitiveType.Sphere, parts, new Vector3(0.82f, 0.12f, 0f), Vector3.one * 0.12f, muzzleMat);
            var grip = Visual(PrimitiveType.Cube, parts, new Vector3(-0.38f, -0.3f, 0f), new Vector3(0.26f, 0.55f, 0.2f), gunWoodMat);
            grip.transform.localRotation = Quaternion.Euler(0f, 0f, -20f);
            Visual(PrimitiveType.Cube, parts, new Vector3(-0.1f, -0.16f, 0f), new Vector3(0.3f, 0.08f, 0.12f), gunMetalMat);
            Visual(PrimitiveType.Cube, parts, new Vector3(-0.48f, 0.2f, 0f), new Vector3(0.12f, 0.14f, 0.16f), gunMetalMat);
        }

        GameObject Visual(PrimitiveType type, Transform parent, Vector3 lpos, Vector3 lscale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = lpos;
            go.transform.localScale = lscale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        ParticleSystem BuildMuzzleFlash(Transform parent)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0.82f, 0.12f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.18f;
            main.startSpeed = 7f;
            main.startSize = 0.28f;
            main.startColor = Palette.Muzzle;
            var emission = ps.emission;
            emission.enabled = false;
            ps.Stop();
            // player builds give runtime particle systems no default material
            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.SoftParticle();
            return ps;
        }

        ParticleSystem BuildEmbers()
        {
            var go = new GameObject("Embers");
            go.transform.position = new Vector3(0f, 0f, 2f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.7f, 0.3f, 0.9f),
                new Color(0.2f, 0.85f, 0.8f, 0.7f));
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(18f, 1f, 6f);

            // all velocity axes must share the same curve mode
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0.6f);
            vel.z = new ParticleSystem.MinMaxCurve(0f);

            var emission = ps.emission;
            emission.rateOverTime = 14f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.15f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.SoftParticle();
            return ps;
        }

        GameObject BuildRidge(int seed, int index, float depth, Material mat)
        {
            var go = new GameObject($"TitleRidge_{index}");
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            var rng = new System.Random(seed + index * 7);
            int cols = 14;
            float width = 100f + index * 35f;
            float baseY = -10f - index * 3f;
            float maxH = 16f - index * 2.5f;
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
            go.transform.position = new Vector3(0f, 5f, depth);
            return go;
        }

        void Update()
        {
            float t = Time.unscaledTime;

            gunRoot.rotation = Quaternion.Euler(
                18f + Mathf.Sin(t * 0.7f) * 8f,
                t * 42f + Mathf.Sin(t * 0.45f) * 12f,
                12f + Mathf.Cos(t * 0.55f) * 10f + kick * 18f);
            gunRoot.position = gunBasePos + new Vector3(
                Mathf.Sin(t * 0.35f) * 0.35f,
                Mathf.Sin(t * 0.9f) * 0.25f,
                0f);

            kick = Mathf.MoveTowards(kick, 0f, Time.unscaledDeltaTime * 5f);
            gunParts.localPosition = new Vector3(-0.22f * kick, 0f, 0f);

            shotTimer += Time.unscaledDeltaTime;
            if (shotTimer >= ShotInterval)
            {
                shotTimer = 0f;
                kick = 1f;
                muzzleFlash.Emit(16);
            }

            UpdateLaser(t);
        }

        void UpdateLaser(float t)
        {
            if (laser == null || gunRoot == null) return;
            Vector3 origin = gunRoot.TransformPoint(new Vector3(0.9f, 0.12f, 0f));
            Vector3 dir = gunRoot.right;
            float pulse = 0.55f + 0.45f * Mathf.Sin(t * 6f);
            laser.startWidth = 0.04f + 0.05f * pulse;
            laser.endWidth = laser.startWidth * 0.3f;
            laser.SetPosition(0, origin);
            laser.SetPosition(1, origin + dir * (22f + pulse * 6f));
        }

        public void SetGunOffset(Vector3 offset)
        {
            gunBasePos = new Vector3(0f, 2.2f, 0f) + offset;
        }
    }
}
