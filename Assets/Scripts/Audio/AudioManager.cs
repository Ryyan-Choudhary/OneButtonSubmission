using System.Collections.Generic;
using UnityEngine;

namespace OneButtonSubmission.Audio
{
    /// Fire-and-forget SFX plus the looping theme, self-initializing on first
    /// use so nothing needs to be wired up in the bootstrap or the inspector.
    /// Clips are loaded from Resources/Sfx/<name> — drop the mp3s in
    /// Assets/Resources/Sfx/ using the exact names below.
    public static class AudioManager
    {
        public enum Sfx { Gunshot, WindowBreak, Explosion, Reload, OhNo, Victory, GunFlying }

        static readonly Dictionary<Sfx, string> ClipNames = new Dictionary<Sfx, string>
        {
            { Sfx.Gunshot,     "gunshot" },
            { Sfx.WindowBreak, "window_break" },
            { Sfx.Explosion,   "explosion" },
            { Sfx.Reload,      "reload" },
            { Sfx.OhNo,        "oh_no" },
            { Sfx.Victory,     "victory" },
            { Sfx.GunFlying,   "gun_flying" },
        };
        const string ThemeClipName = "theme";

        public static float SfxVolume = 1f;
        public static float ThemeVolume = 0.7f;

        static Dictionary<Sfx, AudioClip> clips;
        static AudioClip themeClip;

        static GameObject root;
        static AudioSource oneShotSource; // stacked one-shots: gunshot, explosion, etc.
        static AudioSource loopSource;    // single looping channel — used for the gun-flying dash
        static AudioSource themeSource;   // dedicated channel so nothing else can stop the theme

        static void EnsureInit()
        {
            if (root != null) return;

            root = new GameObject("AudioManager");
            Object.DontDestroyOnLoad(root);

            oneShotSource = root.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;

            loopSource = root.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;

            themeSource = root.AddComponent<AudioSource>();
            themeSource.playOnAwake = false;
            themeSource.loop = true;

            clips = new Dictionary<Sfx, AudioClip>();
            foreach (var kv in ClipNames)
            {
                var clip = Resources.Load<AudioClip>("Sfx/" + kv.Value);
                if (clip == null)
                    Debug.LogWarning($"AudioManager: missing clip 'Sfx/{kv.Value}' — " +
                        "put the mp3 under Assets/Resources/Sfx/ with that exact name.");
                clips[kv.Key] = clip;
            }

            themeClip = Resources.Load<AudioClip>("Sfx/" + ThemeClipName);
            if (themeClip == null)
                Debug.LogWarning("AudioManager: missing clip 'Sfx/theme' — " +
                    "put theme.mp3 under Assets/Resources/Sfx/.");
        }

        /// One-shot SFX. Safe to call rapidly — PlayOneShot stacks overlapping calls.
        public static void Play(Sfx id)
        {
            EnsureInit();
            if (clips.TryGetValue(id, out var clip) && clip != null)
                oneShotSource.PlayOneShot(clip, SfxVolume);
        }

        /// Starts looping a clip on the dedicated loop channel (one at a time —
        /// used for the gun-flying dash). Call StopLoop() when it should end.
        public static void PlayLoop(Sfx id)
        {
            EnsureInit();
            if (!clips.TryGetValue(id, out var clip) || clip == null) return;
            loopSource.clip = clip;
            loopSource.volume = SfxVolume;
            loopSource.Play();
        }

        public static void StopLoop()
        {
            EnsureInit();
            loopSource.Stop();
        }

        /// Starts the main theme looping. Safe to call more than once —
        /// does nothing if it's already playing.
        public static void PlayThemeLoop()
        {
            EnsureInit();
            if (themeClip == null || themeSource.isPlaying) return;
            themeSource.clip = themeClip;
            themeSource.volume = ThemeVolume;
            themeSource.Play();
        }

        public static void StopTheme()
        {
            EnsureInit();
            themeSource.Stop();
        }
    }
}