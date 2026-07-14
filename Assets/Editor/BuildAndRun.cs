using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneButtonSubmission.Editor
{
    /// Reliable Windows Build And Run for Unity 6: pins SampleScene into the
    /// build list, writes to Builds/Windows, and launches the player. Use the
    /// menu item (Ctrl+B) so Build And Run always has a fixed output path and launches.
    public static class BuildAndRun
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string OutputDir = "Builds/Windows";
        const string ExeName = "OneButtonSubmission.exe";

        [MenuItem("File/Build And Run Windows %b", false, 211)]
        public static void BuildAndRunWindows()
        {
            EnsureSceneInBuild();

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string outDir = Path.Combine(projectRoot, OutputDir);
            Directory.CreateDirectory(outDir);

            string exePath = Path.Combine(outDir, ExeName);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.AutoRunPlayer,
            };

            Debug.Log($"Building & running → {exePath}");
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                Debug.LogError($"Build failed: {report.summary.result}");
        }

        /// Headless build for command-line verification: builds a fresh player to
        /// Builds/Verify WITHOUT auto-running, so an external process (CI / agent)
        /// can build from batchmode and then launch the player itself. Exits with
        /// a non-zero code on failure so the caller can detect it.
        public static void BuildHeadless()
        {
            EnsureSceneInBuild();

            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string outDir = Path.Combine(projectRoot, "Builds", "Verify");
            Directory.CreateDirectory(outDir);
            string exePath = Path.Combine(outDir, ExeName);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[BuildHeadless] result={summary.result} errors={summary.totalErrors} out={exePath}");
            if (summary.result != BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        [MenuItem("File/Build And Run Windows %b", true)]
        static bool ValidateBuildAndRunWindows()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode
                && !BuildPipeline.isBuildingPlayer;
        }

        static void EnsureSceneInBuild()
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path.Replace('\\', '/') == ScenePath)
                {
                    if (!scenes[i].enabled)
                    {
                        scenes[i].enabled = true;
                        EditorBuildSettings.scenes = scenes;
                    }
                    return;
                }
            }

            var next = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++)
                next[i] = scenes[i];
            next[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = next;
        }
    }
}
