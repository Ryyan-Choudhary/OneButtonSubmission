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
        public enum Sfx { Gunshot, WindowBreak, Explosion, Reload, OhNo, Victory, GunFlying, LockBeep }

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

            clips[Sfx.LockBeep] = MakeLockBeep(); // procedural: no mp3 needed
        }

        /// A short two-pip missile-lock beep, synthesized so it ships with
        /// the code instead of the Resources folder.
        static AudioClip MakeLockBeep()
        {
            const int rate = 44100;
            const float pip = 0.09f, gap = 0.06f, freq = 1174.7f; // D6
            int n = (int)(rate * (pip * 2f + gap));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / rate;
                float local = t < pip ? t
                            : t >= pip + gap ? t - pip - gap
                            : -1f;
                if (local < 0f) continue;
                float env = Mathf.Sin(Mathf.PI * (local / pip)); // smooth pip
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f * env;
            }
            var clip = AudioClip.Create("lock_beep", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
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