using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// A level as data: which gun, gravity, sky tint, arena bounds, and the climb layout.
    ///
    /// Design language: each level is a CANYON. Wide shelf-terraces jut out of
    /// the arena walls, alternating sides as they rise. The walls themselves are
    /// the forgiveness mechanic — overshoot an arc and you bounce off the wall
    /// and settle onto the shelf below it; undershoot and you fall back to the
    /// ground floor and try again. Nothing requires a precision landing.
    [System.Serializable]
    public class LevelConfig
    {
        /// A terrace: a chunky box the gun can land on. Center + size.
        [System.Serializable]
        public struct Shelf
        {
            public float cx, cy, w, h;
            public Shelf(float cx, float cy, float w, float h)
            {
                this.cx = cx; this.cy = cy; this.w = w; this.h = h;
            }
        }

        public string name = "Level";
        public GunConfig gun = GunConfig.Blaster();
        public float gravityY = -12f;
        public Color skyBottom = new Color(0.09f, 0.08f, 0.18f);
        public Color skyTop = new Color(0.34f, 0.26f, 0.46f);
        public Color ambient = new Color(0.12f, 0.12f, 0.20f);
        public Vector2 startPos = new Vector2(0f, 2f);
        public float wallLeft = -25f;   // arena bounds — the ground ends exactly here
        public float wallRight = 25f;
        public float ceiling = 40f;     // high above the summit: aimed arcs need headroom
        public Shelf[] shelves;
        public Vector2[] routePickups;
        public Vector2 summit;
        public bool villainAtSummit; // white-suit thief instead of the agent

        /// Level 1: the Blaster canyon. Four crossings, each the same 2-beat
        /// rhythm the gun teaches itself: the rest-pose shot hops you off the
        /// floor, then one aimed shot arcs you across onto a 15-wide shelf
        /// backed by the wall. Rise per shelf ≈ 6 (one aimed 45° arc ≈ 17
        /// across, 8 up).
        public static LevelConfig Foothills() => new LevelConfig
        {
            name = "1 — FOOTHILLS",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.09f, 0.08f, 0.18f),
            skyTop = new Color(0.34f, 0.26f, 0.46f),
            ambient = new Color(0.12f, 0.12f, 0.20f),
            startPos = new Vector2(0f, 2f),
            wallLeft = -32f,
            wallRight = 32f,
            ceiling = 42f,
            shelves = new[]
            {
                new Shelf( 24.5f,  4f, 15f, 4f), // top  6, spans x 17..32 (right wall)
                new Shelf(-24.5f, 10f, 15f, 4f), // top 12, spans -32..-17 (left wall)
                new Shelf( 24.5f, 16f, 15f, 4f), // top 18 (right wall)
                new Shelf(-24.5f, 22f, 15f, 4f), // top 24: summit plateau (left wall)
            },
            routePickups = new[]
            {
                new Vector2( 24.5f,  8f),  // floats above shelf 1
                new Vector2(-24.5f, 14f),  // above shelf 2
                new Vector2( 24.5f, 20f),  // above shelf 3
                new Vector2( 0f,    16f),  // mid-canyon: grab it on the fly
            },
            summit = new Vector2(-24.5f, 25.5f), // flag standing on the plateau
        };

        /// Level 2: the Sniper canyon. Wider arena (±30), higher steps (+8 per
        /// shelf) tuned to the Sniper's bigger kick — every crossing wants a
        /// bullet-time aim. The finale converges on a floating platform at the
        /// canyon's center top. Roof at 55 because an aimed Sniper arc gains
        /// ~23 units of height.
        public static LevelConfig TheSpire() => new LevelConfig
        {
            name = "2 — THE SPIRE",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.05f, 0.08f, 0.20f),
            skyTop = new Color(0.20f, 0.34f, 0.52f),
            ambient = new Color(0.10f, 0.13f, 0.20f),
            startPos = new Vector2(0f, 2f),
            wallLeft = -38f,
            wallRight = 38f,
            ceiling = 58f,
            shelves = new[]
            {
                new Shelf( 29.5f,  5f, 16f, 4f), // top  7, spans 21.5..37.5 (right wall)
                new Shelf(-29.5f, 13f, 16f, 4f), // top 15 (left wall)
                new Shelf( 29.5f, 21f, 16f, 4f), // top 23 (right wall)
                new Shelf(-29.5f, 29f, 16f, 4f), // top 31 (left wall)
                new Shelf(  0f,  34f, 14f, 3f),  // top 35.5: floating summit platform
            },
            routePickups = new[]
            {
                new Vector2( 29.5f,  9f),  // above shelf 1
                new Vector2(-29.5f, 17f),  // above shelf 2
                new Vector2( 29.5f, 25f),  // above shelf 3
                new Vector2(-29.5f, 33f),  // above shelf 4
                new Vector2( 0f,   20f),   // mid-canyon flyby reward
            },
            summit = new Vector2(0f, 37f), // flag on the floating platform
        };

        /// Level 3: the Hand Cannon. Towering vertical gaps (+12-14 per wall
        /// shelf, vs 6/8 earlier) in the widest arena, with floating mid-canyon
        /// stepping stones on the later rises. At the top, a white-suit rival
        /// stands between you and the real agent who takes the delivery.
        /// Blood-dusk sky.
        public static LevelConfig TheDoubleCross() => new LevelConfig
        {
            name = "3 — THE DOUBLE CROSS",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.14f, 0.05f, 0.09f),
            skyTop = new Color(0.38f, 0.14f, 0.22f),
            ambient = new Color(0.16f, 0.09f, 0.11f),
            startPos = new Vector2(0f, 2f),
            wallLeft = -44f,
            wallRight = 44f,
            ceiling = 80f,
            villainAtSummit = true,
            shelves = new[]
            {
                new Shelf( 35f,    5f, 18f, 4f),   // top  7 (right wall)
                new Shelf(-35f,   17f, 18f, 4f),   // top 19 (left wall)
                new Shelf(  6f, 24.5f,  9f, 2.5f), // top ~25.7: mid stepping stone
                new Shelf( 35f,   31f, 18f, 4f),   // top 33 (right wall)
                new Shelf( -5f, 38.5f,  9f, 2.5f), // top ~39.7: mid stepping stone
                new Shelf(-35f,   45f, 18f, 4f),   // top 47: the rival's shelf (final)
            },
            routePickups = new[]
            {
                new Vector2( 35f,  9f),
                new Vector2(-35f, 21f),
                new Vector2(  6f, 27.5f),
                new Vector2( 35f, 35f),
                new Vector2( -5f, 41.5f),
            },
            summit = new Vector2(-35f, 47f),
        };
    }
}