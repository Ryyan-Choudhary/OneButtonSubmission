using UnityEngine;

namespace OneButtonSubmission.Art
{
    /// Central twilight/dusk palette for the whole game (linear RGB).
    public static class Palette
    {
        // Sky / atmosphere
        public static readonly Color SkyBottom = new Color(0.09f, 0.08f, 0.18f);
        public static readonly Color SkyTop    = new Color(0.34f, 0.26f, 0.46f);
        public static readonly Color Ambient   = new Color(0.12f, 0.12f, 0.20f);

        // Rock / terrain (cool grey-violet)
        public static readonly Color Rock   = new Color(0.30f, 0.28f, 0.34f);
        public static readonly Color Ground = new Color(0.22f, 0.20f, 0.26f);

        // Player (warm amber)
        public static readonly Color Body = new Color(0.95f, 0.55f, 0.18f);
        public static readonly Color Head = new Color(1.00f, 0.70f, 0.34f);
        public static readonly Color Limb = new Color(0.26f, 0.22f, 0.30f);

        // Shotgun
        public static readonly Color GunMetal = new Color(0.20f, 0.21f, 0.25f);
        public static readonly Color GunWood  = new Color(0.35f, 0.22f, 0.14f);
        public static readonly Color Muzzle   = new Color(1.00f, 0.62f, 0.22f);

        // Pickups / summit (emissive)
        public static readonly Color Ammo     = new Color(0.10f, 0.85f, 0.80f);
        public static readonly Color AmmoHaze = new Color(0.66f, 0.42f, 0.98f); // violet, matches the shot haze
        public static readonly Color Summit   = new Color(1.00f, 0.80f, 0.30f);

        // Neon signage
        public static readonly Color Neon = new Color(1.00f, 0.20f, 0.62f);

        // Aim laser
        public static readonly Color Laser = new Color(1.00f, 0.22f, 0.18f);

        // Background ridge layers (near -> far)
        public static readonly Color[] Ridges =
        {
            new Color(0.18f, 0.15f, 0.28f),
            new Color(0.14f, 0.12f, 0.22f),
            new Color(0.10f, 0.09f, 0.17f),
        };

        // Lights
        public static readonly Color KeyLight = new Color(0.70f, 0.78f, 1.00f);
        public static readonly Color RimLight = new Color(1.00f, 0.55f, 0.30f);
    }

    /// Creates Built-in Render Pipeline materials from the palette.
    public static class MaterialFactory
    {
        static Shader _standard;
        static Shader Standard => _standard != null ? _standard : (_standard = Shader.Find("Standard"));

        public static Material Lit(Color color, float smoothness = 0.15f, float metallic = 0f)
        {
            var m = new Material(Standard);
            m.color = color;
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", metallic);
            return m;
        }

        public static Material Emissive(Color color, Color emission, float intensity = 1.5f, float smoothness = 0.3f)
        {
            var m = Lit(color, smoothness, 0f);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            m.SetColor("_EmissionColor", emission * intensity);
            return m;
        }

        public static Material Unlit(Color color)
        {
            var m = new Material(Shader.Find("Unlit/Color"));
            m.color = color;
            return m;
        }

        public static Material UnlitTexture(Texture2D tex)
        {
            var m = new Material(Shader.Find("Unlit/Texture"));
            m.mainTexture = tex;
            return m;
        }

        static Material softParticle;

        /// Alpha-blended particle material with a soft radial-gradient dot,
        /// so smoke/dust renders as round puffs instead of magenta squares.
        public static Material SoftParticle()
        {
            if (softParticle != null) return softParticle;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();

            softParticle = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            softParticle.mainTexture = tex;
            return softParticle;
        }
    }
}
