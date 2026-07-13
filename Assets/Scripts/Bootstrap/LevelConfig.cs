using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// A level as data: gravity, sky tint, arena bounds, and the climb layout.
    ///
    /// Design language: each level is an URBAN CANYON between two skyscrapers.
    /// There is NO floor — the gun bursts out of a tower's glass, is caught by
    /// a wide pad on its entry arc, then travels balcony-to-balcony. Wall
    /// balconies jut from the tower facades (overshoot bounces off the tower
    /// onto the balcony); small floating stones ladder along the canyon's
    /// center as the safe-but-slower route. Falling past the bottom is a fast
    /// retry, so bold direct arcs vs stone-hopping is the core risk choice.
    ///
    /// Levels 1-3 CLIMB: entry low out of the right tower, goal at the top.
    /// Levels 4-6 DESCEND: entry high out of the LEFT tower's roofline, goal
    /// at street level — gravity is an ally now, control is the challenge.
    /// Levels 7-9 DEMOLITION: glass panes wall off arcs (shoot to open)
    /// and rocketeers join the gunners. Shooting the world is now as
    /// important as shooting for thrust.
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
        public float burstHeight = 4.5f; // where the gun smashes out of the entry tower
        public bool enterFromLeft;       // descent levels: burst from the LEFT tower instead
        public float wallLeft = -32f;    // tower inner faces — the arena bounds
        public float wallRight = 32f;
        public float ceiling = 42f;      // invisible roof: aimed arcs need headroom
        public Shelf[] shelves;          // LAST entry is the goal shelf (Bond's)
        public Vector2[] routePickups;
        public Vector2 summit;
        public Vector2 signPos;          // neon arrow mount point (beside the middle building)
        public int[] villainShelves;     // shelf indices with a melee rival squatting on them
        public int[] gunnerShelves;      // shelf indices with a gunner firing a fixed lane (levels 5+)
        public int[] rocketeerShelves;   // shelf indices with a homing-missile rocketeer (levels 7+)
        public Shelf[] glassPanes;       // breakable panes blocking routes (levels 5+)

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
            villainShelves = new[] { 5 }, // the rival's stone below
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

        /// Level 4: the descent begins. The gun bursts out of the LEFT tower
        /// near the roofline and works DOWN the canyon — wider gaps than the
        /// climb levels (crossings of ~34-40), one guard on the last stone
        /// before Bond's street-level balcony. Neon teal night.
        public static LevelConfig Level4() => new LevelConfig
        {
            name = "Level 4",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.03f, 0.11f, 0.11f),
            skyTop = new Color(0.10f, 0.32f, 0.28f),
            ambient = new Color(0.09f, 0.14f, 0.13f),
            enterFromLeft = true,
            burstHeight = 30f,
            wallLeft = -46f,
            wallRight = 46f,
            ceiling = 40f,
            shelves = new[]
            {
                new Shelf(-32f, 26.25f, 12f, 2.5f), // top 27.5: catch pad on the entry arc
                new Shelf(  2f, 20.5f,   8f, 2f),   // top 21.5: center stone
                new Shelf( 40f, 14.75f, 12f, 2.5f), // top 16:   right balcony
                new Shelf(  0f,  8.5f,   9f, 2f),   // top  9.5: guarded stone
                new Shelf(-40f,  0.75f, 12f, 2.5f), // top  2:   Bond's balcony (goal)
            },
            villainShelves = new[] { 3 },
            routePickups = new[]
            {
                new Vector2(-32f, 30f),
                new Vector2(  2f, 24f),
                new Vector2( 40f, 18.5f),
                new Vector2( 20f, 12.5f), // on the arc down from the right balcony
                new Vector2(-20f,  7f),   // on the bypass arc past the guard
            },
            summit = new Vector2(-40f, 2f),
            signPos = new Vector2(-11f, 12f), // left flank of the middle building
        };

        /// Level 5: a wider canyon and longer falls (crossings of ~40-44),
        /// guards on BOTH lower center stones — swing wide onto the balconies
        /// or thread the stones' edges. Deep violet-magenta night.
        public static LevelConfig Level5() => new LevelConfig
        {
            name = "Level 5",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.10f, 0.04f, 0.14f),
            skyTop = new Color(0.30f, 0.12f, 0.38f),
            ambient = new Color(0.13f, 0.09f, 0.15f),
            enterFromLeft = true,
            burstHeight = 38f,
            wallLeft = -54f,
            wallRight = 54f,
            ceiling = 48f,
            shelves = new[]
            {
                new Shelf(-38f, 33.25f, 12f, 2.5f), // top 34.5: catch pad
                new Shelf(  4f, 26.5f,   8f, 2f),   // top 27.5: center stone
                new Shelf( 48f, 19.75f, 12f, 2.5f), // top 21:   right balcony
                new Shelf(  0f, 13.5f,   9f, 2f),   // top 14.5: guarded stone
                new Shelf(-48f,  7.75f, 12f, 2.5f), // top  9:   left balcony
                new Shelf( -6f,  3.5f,  10f, 2f),   // top  4.5: guarded stone
                new Shelf( 34f,  0.75f, 12f, 2.5f), // top  2:   Bond's floating goal
            },
            villainShelves = new[] { 3, 5 },
            gunnerShelves = new[] { 2 }, // first gunner: lane sweeps the canyon off the right balcony
            glassPanes = new[]
            {
                // first taste of demolition: one pane over the arc down to
                // the right balcony — spend a shell or arc high over it
                new Shelf(24f, 21f, 0.5f, 9f),
            },
            routePickups = new[]
            {
                new Vector2(-38f, 37f),
                new Vector2(  4f, 30f),
                new Vector2( 48f, 23.5f),
                new Vector2( 24f, 17f),   // on the arc down from the right balcony
                new Vector2(-48f, 11.5f),
                new Vector2( 14f,  5.5f), // on the arc past the last guard, toward Bond
            },
            summit = new Vector2(34f, 2f),
            signPos = new Vector2(14f, 10f), // right flank of the middle building
        };

        /// Level 6: the longest fall in the game — the widest canyon, drops of
        /// 7-8 between shelves, crossings up to ~50, guards on both center
        /// stones, and Bond waiting at street level across the canyon from
        /// the entry. Pre-dawn amber sky for the finale.
        public static LevelConfig Level6() => new LevelConfig
        {
            name = "Level 6",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.12f, 0.07f, 0.05f),
            skyTop = new Color(0.42f, 0.27f, 0.15f),
            ambient = new Color(0.15f, 0.12f, 0.09f),
            enterFromLeft = true,
            burstHeight = 46f,
            wallLeft = -60f,
            wallRight = 60f,
            ceiling = 56f,
            shelves = new[]
            {
                new Shelf(-44f, 41.25f, 14f, 2.5f), // top 42.5: catch pad
                new Shelf(  6f, 33.5f,   8f, 2f),   // top 34.5: center stone
                new Shelf( 53f, 25.75f, 14f, 2.5f), // top 27:   right balcony
                new Shelf( -8f, 18.5f,   9f, 2f),   // top 19.5: guarded stone
                new Shelf(-53f, 11.75f, 14f, 2.5f), // top 13:   left balcony
                new Shelf(  8f,  6.5f,  10f, 2f),   // top  7.5: guarded stone
                new Shelf( 53f,  0.75f, 14f, 2.5f), // top  2:   Bond's balcony (goal)
            },
            villainShelves = new[] { 3, 5 },
            gunnerShelves = new[] { 2, 4 }, // lanes off both wall balconies
            glassPanes = new[]
            {
                new Shelf(-24f, 16f, 0.5f, 9f), // guards the left-balcony approach
                new Shelf( 30f,  4f, 0.5f, 8f), // guards the final run to Bond
            },
            routePickups = new[]
            {
                new Vector2(-44f, 45f),
                new Vector2(  6f, 37f),
                new Vector2( 53f, 29.5f),
                new Vector2( 24f, 22f),   // on the arc down from the right balcony
                new Vector2(-53f, 15.5f),
                new Vector2(-22f, 10f),   // on the arc down from the left balcony
                new Vector2( 30f,  5f),   // on the arc past the last guard, toward Bond
            },
            summit = new Vector2(53f, 2f),
            signPos = new Vector2(16f, 16f), // right flank of the middle building
        };

        /// Level 7: the DEMOLITION act opens — back to a climb out of the
        /// right tower, but the city fights back: glass panes wall off arcs,
        /// the first rocketeer holds the center stones, and neon signs hang
        /// over every hostile, one bullet away from becoming a guillotine.
        /// Steel teal-grey night.
        public static LevelConfig Level7() => new LevelConfig
        {
            name = "Level 7",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.06f, 0.09f, 0.11f),
            skyTop = new Color(0.22f, 0.30f, 0.34f),
            ambient = new Color(0.10f, 0.13f, 0.14f),
            burstHeight = 4.5f,
            wallLeft = -48f,
            wallRight = 48f,
            ceiling = 52f,
            shelves = new[]
            {
                new Shelf( 30f,  0.75f, 13f, 2.5f), // top  2:   catch pad
                new Shelf( -2f,  6f,     8f, 2f),   // top  7:   center stone
                new Shelf(-42f, 11.75f, 12f, 2.5f), // top 13:   left balcony
                new Shelf(  6f, 17.5f,   9f, 2f),   // top 18.5: rival's stone
                new Shelf( 42f, 23.25f, 12f, 2.5f), // top 24.5: right balcony (gunner)
                new Shelf( -4f, 29f,     9f, 2f),   // top 30:   rocketeer's stone
                new Shelf(-42f, 34.75f, 12f, 2.5f), // top 36:   Bond's balcony (goal)
            },
            villainShelves = new[] { 3 },
            gunnerShelves = new[] { 4 },
            rocketeerShelves = new[] { 5 }, // the first rocketeer, center stage
            glassPanes = new[]
            {
                new Shelf(-16f, 16f, 0.5f, 9f), // blocks the balcony-to-stone arc
                new Shelf(-24f, 33f, 0.5f, 9f), // blocks the final approach
            },
            routePickups = new[]
            {
                new Vector2( 30f,  4.5f),
                new Vector2( -2f,  9.5f),
                new Vector2(-42f, 15.5f),
                new Vector2(-18f, 17.5f), // bypass arc toward the rival
                new Vector2( 42f, 27f),
                new Vector2( -4f, 32.5f),
                new Vector2(-26f, 35f),   // final arc toward Bond
            },
            summit = new Vector2(-42f, 36f),
            signPos = new Vector2(14f, 20f), // right flank of the middle building
        };

        /// Level 8: descent through a pink neon dusk — gunner lanes off both
        /// wall balconies and a rocketeer squatting mid-route. Missiles
        /// chasing you down a canyon full of glass is the level's whole
        /// argument.
        public static LevelConfig Level8() => new LevelConfig
        {
            name = "Level 8",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.12f, 0.05f, 0.10f),
            skyTop = new Color(0.40f, 0.16f, 0.30f),
            ambient = new Color(0.14f, 0.09f, 0.12f),
            enterFromLeft = true,
            burstHeight = 40f,
            wallLeft = -52f,
            wallRight = 52f,
            ceiling = 50f,
            shelves = new[]
            {
                new Shelf(-36f, 35.25f, 12f, 2.5f), // top 36.5: catch pad
                new Shelf(  6f, 28.5f,   8f, 2f),   // top 29.5: center stone
                new Shelf( 46f, 21.75f, 12f, 2.5f), // top 23:   right balcony (gunner)
                new Shelf( -2f, 15.5f,   9f, 2f),   // top 16.5: rocketeer's stone
                new Shelf(-46f,  9.75f, 12f, 2.5f), // top 11:   left balcony (gunner)
                new Shelf( 10f,  4.5f,   9f, 2f),   // top  5.5: rival's stone
                new Shelf( 44f,  0.75f, 13f, 2.5f), // top  2:   Bond's floating goal
            },
            villainShelves = new[] { 5 },
            gunnerShelves = new[] { 2, 4 },
            rocketeerShelves = new[] { 3 },
            glassPanes = new[]
            {
                new Shelf( 24f, 25f,   0.5f, 9f), // over the arc down to the gunner's balcony
                new Shelf(-26f, 12.5f, 0.5f, 9f), // between rocketeer and the left balcony
            },
            routePickups = new[]
            {
                new Vector2(-36f, 39f),
                new Vector2(  6f, 32f),
                new Vector2( 46f, 25.5f),
                new Vector2( -2f, 19f),
                new Vector2(-46f, 13.5f),
                new Vector2( 26f,  9f),   // bypass arc past the rival, toward Bond
            },
            summit = new Vector2(44f, 2f),
            signPos = new Vector2(18f, 18f), // right flank of the middle building
        };

        /// Level 9: the finale — the tallest climb in the game under a storm
        /// violet sky. Two rocketeers, two gunners, a rival, three panes of
        /// glass, and four hanging signs: the city is one big demolition
        /// puzzle, and Bond waits on a floating platform above it all.
        public static LevelConfig Level9() => new LevelConfig
        {
            name = "Level 9",
            gun = GunConfig.Blaster(),
            gravityY = -12f,
            skyBottom = new Color(0.05f, 0.05f, 0.13f),
            skyTop = new Color(0.24f, 0.18f, 0.44f),
            ambient = new Color(0.10f, 0.10f, 0.16f),
            burstHeight = 4.5f,
            wallLeft = -56f,
            wallRight = 56f,
            ceiling = 64f,
            shelves = new[]
            {
                new Shelf( 34f,  0.75f, 14f, 2.5f), // top  2:   catch pad
                new Shelf( -4f,  6.5f,   8f, 2f),   // top  7.5: center stone
                new Shelf(-50f, 12.75f, 12f, 2.5f), // top 14:   left balcony (gunner)
                new Shelf(  8f, 19f,     9f, 2f),   // top 20:   rival's stone
                new Shelf( 50f, 25.25f, 12f, 2.5f), // top 26.5: right balcony (rocketeer)
                new Shelf( -6f, 31.5f,   9f, 2f),   // top 32.5: gunner's stone
                new Shelf(-50f, 37.75f, 12f, 2.5f), // top 39:   left balcony (rocketeer)
                new Shelf(  0f, 44.25f, 13f, 2.5f), // top 45.5: Bond's floating summit
            },
            villainShelves = new[] { 3 },
            gunnerShelves = new[] { 2, 5 },
            rocketeerShelves = new[] { 4, 6 },
            glassPanes = new[]
            {
                new Shelf(-28f, 10f,   0.5f, 9f),  // stone-to-balcony arc
                new Shelf( 28f, 22.5f, 0.5f, 9f),  // rival-to-rocketeer arc
                new Shelf(-24f, 41f,   0.5f, 10f), // the last pane before the summit
            },
            routePickups = new[]
            {
                new Vector2( 34f,  4.5f),
                new Vector2( -4f, 10f),
                new Vector2(-50f, 16.5f),
                new Vector2(-22f, 23f),   // bypass arc toward the rival
                new Vector2( 50f, 29f),
                new Vector2( -6f, 35f),
                new Vector2(-50f, 41.5f),
                new Vector2(-20f, 44f),   // final arc to the summit
            },
            summit = new Vector2(0f, 45.5f),
            signPos = new Vector2(16f, 26f), // right flank of the middle building
        };
    }
}
