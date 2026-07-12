namespace OneButtonSubmission.Core
{
    /// Data-driven gun definition. Guns differ only by these numbers, so new guns
    /// (and per-level guns) are just new configs.
    [System.Serializable]
    public class GunConfig
    {
        public string displayName = "Blaster";
        public float recoilForce = 16f;          // launch impulse per shot
        public float jumpBoost = 3f;             // flat upward kick added to every shot
        public float opposingCancel = 0.9f;      // 0..1: how much old velocity fighting a shot is cancelled
        public float spinImpulse = 1.2f;         // angular kick per shot (muzzle flips gun-top-ward)
        public float fireCooldown = 0.2f;        // min seconds between snap shots (pump)
        public float bulletTimeScale = 0.12f;    // Time.timeScale while aiming
        public float bulletTimeDuration = 1.5f;  // real seconds of slo-mo available
        public float bulletTimeCooldown = 3.0f;  // real seconds to recharge slo-mo
        public float preciseMultiplier = 1.1f;   // force + spin bonus for a bullet-time shot

        public static GunConfig Blaster() => new GunConfig
        {
            displayName = "Blaster",
            recoilForce = 16f, jumpBoost = 3f, opposingCancel = 0.9f,
            spinImpulse = 1.2f, fireCooldown = 0.2f,
            bulletTimeScale = 0.12f, bulletTimeDuration = 2.0f,
            bulletTimeCooldown = 3.0f, preciseMultiplier = 1.1f,
        };

        public static GunConfig Sniper() => new GunConfig
        {
            displayName = "Sniper",
            recoilForce = 22f, jumpBoost = 4f, opposingCancel = 0.9f,
            spinImpulse = 2.0f, fireCooldown = 0.35f,
            bulletTimeScale = 0.10f, bulletTimeDuration = 3.0f,
            bulletTimeCooldown = 4.0f, preciseMultiplier = 1.25f,
        };

        public static GunConfig HandCannon() => new GunConfig
        {
            displayName = "Hand Cannon",
            recoilForce = 26f, jumpBoost = 5f, opposingCancel = 0.9f,
            spinImpulse = 2.4f, fireCooldown = 0.4f,
            bulletTimeScale = 0.10f, bulletTimeDuration = 3.5f,
            bulletTimeCooldown = 4.0f, preciseMultiplier = 1.15f,
        };
    }
}
