using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

namespace OneButtonSubmission.Editor
{
    /// The entire game is constructed from code at runtime (see GameBootstrap /
    /// MaterialFactory), so the built-in shaders it uses are never referenced by
    /// any scene or material asset. A player build strips such shaders, and
    /// Shader.Find("Standard") then returns null -> `new Material(null)` throws in
    /// GameBootstrap.Awake -> nothing is built -> blank screen.
    ///
    /// This force-includes those shaders (Project Settings > Graphics > Always
    /// Included Shaders) so they survive stripping. Runs automatically before
    /// every build; also runnable from the Tools menu.
    ///
    /// NOTE on emission: the Standard shader's `_EMISSION` shader_feature variant
    /// cannot be reliably kept in a build for runtime-created materials (Resources
    /// materials, ShaderVariantCollections and Preloaded Shaders were all tried
    /// and none worked). MaterialFactory.Emissive therefore renders glows with the
    /// Unlit/Color shader instead, which ships reliably — see Palette.cs.
    class RuntimeShaderInclusion : IPreprocessBuildWithReport
    {
        // Must match every Shader.Find(...) name used at runtime in MaterialFactory.
        static readonly string[] RequiredShaders =
        {
            "Standard",
            "Unlit/Color",
            "Unlit/Texture",
            "Legacy Shaders/Particles/Alpha Blended",
        };

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => EnsureAlwaysIncludedShaders();

        [MenuItem("Tools/Fix Runtime Shaders (Always Include)")]
        static void EnsureAlwaysIncludedShaders()
        {
            var so = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var arr = so.FindProperty("m_AlwaysIncludedShaders");

            var present = new HashSet<Shader>();
            for (int i = 0; i < arr.arraySize; i++)
                if (arr.GetArrayElementAtIndex(i).objectReferenceValue is Shader s)
                    present.Add(s);

            bool changed = false;
            foreach (var name in RequiredShaders)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"[RuntimeShaderInclusion] Shader '{name}' not found; cannot include it.");
                    continue;
                }
                if (present.Contains(shader)) continue;

                arr.arraySize++;
                arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
                present.Add(shader);
                changed = true;
                Debug.Log($"[RuntimeShaderInclusion] Added '{name}' to Always Included Shaders.");
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
