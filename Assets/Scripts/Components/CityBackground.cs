using System.Collections.Generic;
using UnityEngine;
using OneButtonSubmission.Art;

namespace OneButtonSubmission.Components
{
    /// Procedural city skyline layer for the background.
    /// Generates a row of blocky silhouette buildings with tiny emissive
    /// window quads scattered across their faces — Sonic Spring/Starlight Zone
    /// vibes in the game's existing poly-art style.
    public class CityBackground : MonoBehaviour
    {
        // ── public config ────────────────────────────────────────────────

        /// Seed for procedural layout (match backgroundSeed for consistency).
        public int seed = 0;

        /// How far the city spans left/right of world origin.
        public float halfWidth = 90f;

        /// Y position of the ground line the buildings sit on.
        public float groundY = -2f;

        /// Number of distinct building slabs to place.
        public int buildingCount = 22;

        /// Height range [min, max] for buildings (world units).
        public Vector2 heightRange = new Vector2(5f, 18f);

        /// Depth (Z) of the city plane.
        public float depth = 45f;

        /// Silhouette colour of the building bodies.
        public Color silhouetteColor = new Color(0.08f, 0.07f, 0.13f);

        /// Primary window emissive colour (warm, e.g. amber/gold).
        public Color windowColorA = new Color(1.0f, 0.82f, 0.35f);

        /// Secondary window emissive colour (cool, e.g. blue-cyan).
        public Color windowColorB = new Color(0.30f, 0.70f, 1.00f);

        /// Chance [0-1] each window grid cell is lit.
        public float windowDensity = 0.55f;

        // ── private ──────────────────────────────────────────────────────

        Material bodyMat, winMatA, winMatB;

        /// Call once after setting public fields.
        public void Build()
        {
            bodyMat = MaterialFactory.Unlit(silhouetteColor);
            winMatA = MaterialFactory.Emissive(windowColorA, windowColorA, 0.5f, 0.4f);
            winMatB = MaterialFactory.Emissive(windowColorB, windowColorB, 0.4f, 0.4f);

            var rng = new System.Random(seed ^ 0x31759EED);

            // Vary building widths so the skyline looks chunky and irregular
            float x = -halfWidth;
            var gaps = new List<float>();
            for (int i = 0; i < buildingCount; i++)
                gaps.Add((float)(0.4 + rng.NextDouble() * 0.6));

            float totalWeight = 0f;
            foreach (var g in gaps) totalWeight += g;

            for (int i = 0; i < buildingCount; i++)
            {
                float w = (gaps[i] / totalWeight) * halfWidth * 2f;
                w = Mathf.Clamp(w, 6f, 30f);
                float h = heightRange.x + (float)rng.NextDouble() * (heightRange.y - heightRange.x);

                // Taller buildings have an occasional setback (upper tier)
                bool hasTier = h > heightRange.y * 0.65f && rng.NextDouble() > 0.5;

                PlaceBuilding(rng, x, w, h, hasTier);
                x += w + (float)rng.NextDouble() * 0.6f; // tiny random gap
            }
        }

        void PlaceBuilding(System.Random rng, float leftX, float w, float h, bool hasTier)
        {
            float cx = leftX + w * 0.5f;
            float cy = groundY + h * 0.5f;

            // Main body — a flat quad extruded slightly so it catches directional light
            PlaceBox(cx, cy, w - 0.15f, h, 0.6f, bodyMat, "BldgBody");

            // Upper tier (setback)
            if (hasTier)
            {
                float tw = w * ((float)(0.35 + rng.NextDouble() * 0.25));
                float th = (float)(2.5 + rng.NextDouble() * 5.0);
                float tcx = cx + (float)(rng.NextDouble() - 0.5) * (w - tw) * 0.5f;
                float tcy = groundY + h + th * 0.5f;
                PlaceBox(tcx, tcy, tw, th, 0.6f, bodyMat, "BldgTier");
                // Windows on tier
                PlaceWindows(rng, tcx - tw * 0.5f, groundY + h, tw, th);
            }

            // Windows on main body
            PlaceWindows(rng, leftX, groundY, w, h);
        }

        void PlaceBox(float cx, float cy, float w, float h, float d, Material mat, string label)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = label;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(cx, cy, 0f);
            go.transform.localScale    = new Vector3(w, h, d);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            go.GetComponent<MeshRenderer>().receiveShadows = false;
        }

        void PlaceWindows(System.Random rng, float bldgLeft, float bldgBottom, float bldgW, float bldgH)
        {
            // Window grid sizing — chunkier than real life for the retro aesthetic and performance
            float winW  = 0.90f;
            float winH  = 0.80f;
            float padX  = 1.10f;
            float padY  = 1.30f;
            float marginX = 0.90f;
            float marginY = 0.80f;

            int cols = Mathf.Max(1, Mathf.FloorToInt((bldgW - marginX * 2f + padX) / (winW + padX)));
            int rows = Mathf.Max(1, Mathf.FloorToInt((bldgH - marginY * 2f + padY) / (winH + padY)));

            // Re-center the grid within the building
            float gridW = cols * (winW + padX) - padX;
            float gridH = rows * (winH + padY) - padY;
            float startX = bldgLeft + (bldgW - gridW) * 0.5f;
            float startY = bldgBottom + marginY;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (rng.NextDouble() > windowDensity) continue;

                    float wx = startX + col * (winW + padX) + winW * 0.5f;
                    float wy = startY + row * (winH + padY) + winH * 0.5f;

                    // Pick colour — bias toward warm on lower floors, cool on upper
                    float t = (float)row / Mathf.Max(1, rows - 1);
                    Material wm = (rng.NextDouble() < (0.35f + t * 0.4f)) ? winMatB : winMatA;

                    var win = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    win.name = "Win";
                    Object.Destroy(win.GetComponent<Collider>());
                    win.transform.SetParent(transform, false);
                    win.transform.localPosition = new Vector3(wx, wy, -0.32f); // in front of building face
                    win.transform.localScale    = new Vector3(winW, winH, 1f);
                    win.GetComponent<MeshRenderer>().sharedMaterial = wm;
                    win.GetComponent<MeshRenderer>().shadowCastingMode =
                        UnityEngine.Rendering.ShadowCastingMode.Off;
                    win.GetComponent<MeshRenderer>().receiveShadows = false;
                }
            }
        }

        // ── factory ──────────────────────────────────────────────────────

        /// Create and build a city layer, parented to <parent>.
        public static CityBackground Create(
            Transform parent,
            Camera parallaxCam,
            int seed,
            float depth,
            float parallaxFactor,
            float halfWidth,
            float groundY,
            Vector2 heightRange,
            int buildingCount,
            Color silhouette,
            Color windowA,
            Color windowB,
            float windowDensity = 0.55f)
        {
            var go = new GameObject("CityLayer");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, depth);

            var city = go.AddComponent<CityBackground>();
            city.seed          = seed;
            city.halfWidth     = halfWidth;
            city.groundY       = groundY;
            city.buildingCount = buildingCount;
            city.heightRange   = heightRange;
            city.depth         = depth;
            city.silhouetteColor = silhouette;
            city.windowColorA  = windowA;
            city.windowColorB  = windowB;
            city.windowDensity = windowDensity;
            city.Build();

            var pl = go.AddComponent<ParallaxLayer>();
            pl.cam    = parallaxCam.transform;
            pl.factor = parallaxFactor;

            return city;
        }
    }
}
