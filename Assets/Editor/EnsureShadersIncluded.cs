using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OneButtonSubmission.EditorTools
{
    /// The game is generated entirely from code, so no built asset references
    /// the shaders MaterialFactory looks up by name. Player builds strip
    /// unreferenced shaders, Shader.Find returns null in the .exe, and
    /// GameBootstrap.Awake dies before building the world (empty scene).
    /// This pins the required shaders in Graphics Settings > Always Included
    /// Shaders — applied on editor load and again before every build.
    public static class EnsureShadersIncluded
    {
        static readonly string[] Required =
        {
            "Standard",                                // MaterialFactory.Lit / Emissive
            "Unlit/Color",                             // MaterialFactory.Unlit
            "Unlit/Texture",                           // sky gradient
            "Legacy Shaders/Particles/Alpha Blended",  // MaterialFactory.SoftParticle
        };

        [InitializeOnLoadMethod]
        static void ApplyOnLoad() => Apply();

        [MenuItem("Tools/Ensure Always Included Shaders")]
        static void ApplyFromMenu() => Apply();

        class PreBuild : IPreprocessBuildWithReport
        {
            public int callbackOrder => -100;
            public void OnPreprocessBuild(BuildReport report) => Apply();
        }

        static void Apply()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset").FirstOrDefault();
            if (settings == null)
            {
                Debug.LogError("EnsureShadersIncluded: could not load ProjectSettings/GraphicsSettings.asset");
                return;
            }

            var so = new SerializedObject(settings);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            bool dirty = false;

            foreach (string name in Required)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"EnsureShadersIncluded: shader not found in editor: {name}");
                    continue;
                }

                bool present = false;
                for (int i = 0; i < list.arraySize && !present; i++)
                    present = list.GetArrayElementAtIndex(i).objectReferenceValue == shader;
                if (present) continue;

                int at = list.arraySize;
                list.InsertArrayElementAtIndex(at);
                list.GetArrayElementAtIndex(at).objectReferenceValue = shader;
                dirty = true;
                Debug.Log($"EnsureShadersIncluded: added '{name}' to Always Included Shaders");
            }

            if (dirty)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
            }
        }
    }
}
