namespace OneButtonSubmission.Core
{
    /// Data-driven gun definition. Guns differ only by these numbers, so new guns
    /// (and per-level guns) are just new configs.
    [System.Serializable]
    public class GunConfig
    {
        public string displayName = "Blaster";
        public float sweepSpeed = 120f;          // deg/sec the gun flips
        public float recoilForce = 10f;          // launch impulse per shot
        public float fireCooldown = 0.45f;       // min seconds between shots (pump)
        public float bulletTimeScale = 0.30f;    // Time.timeScale while aiming
        public float bulletTimeDuration = 1.5f;  // real seconds of slo-mo available
        public float bulletTimeCooldown = 3.0f;  // real seconds to recharge slo-mo
        public float preciseMultiplier = 1.1f;   // force bonus for a bullet-time shot

        public static GunConfig Blaster() => new GunConfig
        {
            displayName = "Blaster",
            sweepSpeed = 120f, recoilForce = 10f, fireCooldown = 0.45f,
            bulletTimeScale = 0.30f, bulletTimeDuration = 1.5f,
            bulletTimeCooldown = 3.0f, preciseMultiplier = 1.1f,
        };

        public static GunConfig Sniper() => new GunConfig
        {
            displayName = "Sniper",
            sweepSpeed = 60f, recoilForce = 16f, fireCooldown = 0.8f,
            bulletTimeScale = 0.20f, bulletTimeDuration = 2.5f,
            bulletTimeCooldown = 4.0f, preciseMultiplier = 1.25f,
        };
    }
}
