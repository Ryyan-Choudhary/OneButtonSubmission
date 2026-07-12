using UnityEngine;

namespace OneButtonSubmission.Bootstrap
{
    /// Auto-spawns the GameBootstrap when entering Play mode if one isn't already
    /// present in the scene, so no manual scene wiring is needed.
    public static class AutoBoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameBootstrap>() != null) return;
            var go = new GameObject("GameBootstrap");
            go.AddComponent<GameBootstrap>();
        }
    }
}
