using UnityEngine;
using OneButtonSubmission.Core;

namespace OneButtonSubmission.Bootstrap
{
    /// A level as data: which gun, gravity, sky tint, and the climb layout.
    [System.Serializable]
    public class LevelConfig
    {
        public string name = "Level";
        public GunConfig gun = GunConfig.Blaster();
        public float gravityY = -20f;
        public Color skyBottom = new Color(0.09f, 0.08f, 0.18f);
        public Color skyTop = new Color(0.34f, 0.26f, 0.46f);
        public Color ambient = new Color(0.12f, 0.12f, 0.20f);
        public Vector2 startPos = new Vector2(0f, 2f);
        public Vector2[] ledges;
        public Vector2[] routePickups;
        public Vector2 summit = new Vector2(2f, 17f);

        /// Level 1: the Blaster, short twilight climb.
        public static LevelConfig Foothills() => new LevelConfig
        {
            name = "1 — FOOTHILLS",
            gun = GunConfig.Blaster(),
            gravityY = -20f,
            skyBottom = new Color(0.09f, 0.08f, 0.18f),
            skyTop = new Color(0.34f, 0.26f, 0.46f),
            ambient = new Color(0.12f, 0.12f, 0.20f),
            startPos = new Vector2(0f, 2f),
            ledges = new[]
            {
                new Vector2(2f, 3f), new Vector2(-2f, 6f), new Vector2(3f, 9f),
                new Vector2(-1f, 12f), new Vector2(2f, 15f),
            },
            routePickups = new[] { new Vector2(-2f, 7f), new Vector2(2f, 13f) },
            summit = new Vector2(2f, 17f),
        };

        /// Level 2: the Sniper, taller & wider — precise long jumps, colder sky.
        public static LevelConfig TheSpire() => new LevelConfig
        {
            name = "2 — THE SPIRE",
            gun = GunConfig.Sniper(),
            gravityY = -20f,
            skyBottom = new Color(0.05f, 0.08f, 0.20f),
            skyTop = new Color(0.20f, 0.34f, 0.52f),
            ambient = new Color(0.10f, 0.13f, 0.20f),
            startPos = new Vector2(0f, 2f),
            ledges = new[]
            {
                new Vector2(3f, 4f), new Vector2(-3f, 8.5f), new Vector2(3.5f, 13f),
                new Vector2(-3f, 18f), new Vector2(3f, 22.5f),
            },
            routePickups = new[] { new Vector2(-3f, 6f), new Vector2(3.5f, 15f), new Vector2(-3f, 20f) },
            summit = new Vector2(3f, 26f),
        };
    }
}
