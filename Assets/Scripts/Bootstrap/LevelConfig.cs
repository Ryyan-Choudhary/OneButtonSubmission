using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// A level as data: gravity, sky tint, arena bounds, and the climb layout.
    ///
    /// Design language: each level is an URBAN CANYON between two skyscrapers.
    /// There is NO floor — the gun bursts out of the right tower's glass just
    /// above where a floor would be, is caught by a wide low pad on its entry
    /// arc, then climbs balcony-to-balcony. Wall balconies jut from the tower
    /// facades (overshoot bounces off the tower onto the balcony); small
    /// floating stones ladder up the canyon's center as the safe-but-slower
    /// route. Falling past the bottom is a fast retry, so bold direct arcs vs
    /// stone-hopping is the core risk choice.
    [System.Serializable]
    public class LevelConfig
    {
        /// A landing surface: balcony or floating stone. Center + size.
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
        public float burstHeight = 4.5f; // where the gun smashes out of the right tower
        public float wallLeft = -32f;    // tower inner faces — the arena bounds
        public float wallRight = 32f;
        public float ceiling = 42f;      // invisible roof: aimed arcs need headroom
        public Shelf[] shelves;          // LAST entry is the goal shelf (Bond's)
        public Vector2[] routePickups;
        public Vector2 summit;
        public Vector2 signPos;          // neon arrow mount point (beside the middle building)
        public bool villainAtSummit;     // posts the rival on the second-to-last shelf

        /// Level 1: gentle rises (+6), wide balconies, a full center ladder of
        /// stones. Entry arc drops the gun onto the catch pad untouched, so
        /// the player can breathe once before the climb.
        public static LevelConfig Level1() => new LevelConfig
        {
            name = "Level 1",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.09f, 0.08f, 0.18f),
            skyTop = new Color(0.34f, 0.26f, 0.46f),
            ambient = new Color(0.12f, 0.12f, 0.20f),
            burstHeight = 4.5f,
            wallLeft = -32f,
            wallRight = 32f,
            ceiling = 42f,
            shelves = new[]
            {
                new Shelf( 18f,  0.75f, 12f, 2.5f), // top  2: catch pad on the entry arc
                new Shelf( -2f,  4f,     7f, 2f),   // top  5: center stone
                new Shelf(-26f,  6.75f, 12f, 2.5f), // top  8: left balcony
                new Shelf( 26f, 12.75f, 12f, 2.5f), // top 14: right balcony (direct 2-shot cross)
                new Shelf( -2f, 16f,     7f, 2f),   // top 17: center stone
                new Shelf(-26f, 18.75f, 12f, 2.5f), // top 20: Bond's balcony (goal)
            },
            routePickups = new[]
            {
                new Vector2( 18f,  4.5f),
                new Vector2( -2f,  7.5f),
                new Vector2(-26f, 10.5f),
                new Vector2( 26f, 16.5f),
                new Vector2( -2f, 19.5f),
            },
            summit = new Vector2(-26f, 20f),
            signPos = new Vector2(-9.5f, 13f), // left flank of the middle building
        };

        /// Level 2: wider canyon, +6.5 rises, and the finale floats at the
        /// canyon's center top — the last hop leaves both towers behind.
        public static LevelConfig Level2() => new LevelConfig
        {
            name = "Level 2",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.05f, 0.08f, 0.20f),
            skyTop = new Color(0.20f, 0.34f, 0.52f),
            ambient = new Color(0.10f, 0.13f, 0.20f),
            burstHeight = 4.5f,
            wallLeft = -38f,
            wallRight = 38f,
            ceiling = 58f,
            shelves = new[]
            {
                new Shelf( 24f,  0.75f, 12f, 2.5f), // top  2: catch pad
                new Shelf( -2f,  5.5f,   7f, 2f),   // top  6.5: center stone
                new Shelf(-32f,  9.75f, 12f, 2.5f), // top 11: left balcony
                new Shelf(  3f, 14.5f,   7f, 2f),   // top 15.5: center stone
                new Shelf( 32f, 18.75f, 12f, 2.5f), // top 20: right balcony
                new Shelf( -3f, 23.5f,   7f, 2f),   // top 24.5: center stone
                new Shelf(-32f, 27.75f, 12f, 2.5f), // top 29: left balcony
                new Shelf(  0f, 32.25f, 12f, 2.5f), // top 33.5: floating goal platform
            },
            routePickups = new[]
            {
                new Vector2( 24f,  4.5f),
                new Vector2( -2f,  9f),
                new Vector2(-32f, 13.5f),
                new Vector2(  3f, 18f),
                new Vector2( 32f, 22.5f),
                new Vector2( -3f, 27f),
                new Vector2(-32f, 31.5f),
            },
            summit = new Vector2(0f, 33.5f),
            signPos = new Vector2(10.5f, 18f), // right flank of the middle building
        };

        /// Level 3: the widest canyon, +9-11 rises, sparser stones — and the
        /// rival posted on the last center stone, squatting on the natural
        /// route. Slip past him (his reach is a ~3.5-wide box) or commit to
        /// the long direct arc from the right balcony to Bond's. Blood-dusk sky.
        public static LevelConfig Level3() => new LevelConfig
        {
            name = "Level 3",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.14f, 0.05f, 0.09f),
            skyTop = new Color(0.38f, 0.14f, 0.22f),
            ambient = new Color(0.16f, 0.09f, 0.11f),
            burstHeight = 4.5f,
            wallLeft = -44f,
            wallRight = 44f,
            ceiling = 60f,
            villainAtSummit = true,
            shelves = new[]
            {
                new Shelf( 28f,  0.75f, 14f, 2.5f), // top  2: catch pad
                new Shelf( -4f,  6.5f,   8f, 2f),   // top  7.5: center stone
                new Shelf(-38f, 11.75f, 14f, 2.5f), // top 13: left balcony
                new Shelf(  4f, 17.5f,   8f, 2f),   // top 18.5: center stone
                new Shelf( 38f, 22.75f, 14f, 2.5f), // top 24: right balcony
                new Shelf(  0f, 28.5f,   9f, 2f),   // top 29.5: the rival's stone (obstacle)
                new Shelf(-38f, 33.75f, 14f, 2.5f), // top 35: Bond's balcony (goal)
            },
            routePickups = new[]
            {
                new Vector2( 28f,  4.5f),
                new Vector2( -4f, 10f),
                new Vector2(-38f, 15.5f),
                new Vector2(  4f, 21f),
                new Vector2( 38f, 26.5f),
                new Vector2(-10f, 33f), // on the arc past the rival, toward Bond
            },
            summit = new Vector2(-38f, 35f),
            signPos = new Vector2(-12.5f, 21f), // left flank of the middle building
        };
    }
}
