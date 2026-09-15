using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.UI
{
    [CustomEditor(typeof(GraphicsTestImageBuildSettings))]
    class GraphicsTestImageBuildSettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var buildSettings = GraphicsTestBuildSettings.LoadOrDefault();
            if (buildSettings.PlatformSchemata is { Length: > 0 }
                && GUILayout.Button("Import from GraphicsTestBuildSettings"))
            {
                ImportFromBuildSettings(buildSettings);
            }
        }

        void ImportFromBuildSettings(GraphicsTestBuildSettings buildSettings)
        {
            var imageSettings = (GraphicsTestImageBuildSettings)target;
            var merged = new List<PlatformSchema>(imageSettings.PlatformSchemata);
            var keptInBuildSettings = new List<PlatformSchema>();

            foreach (var incoming in buildSettings.PlatformSchemata)
            {
                if (incoming == null)
                    continue;

                if (merged.Exists(existing => existing.ResolvesToSamePathsAs(incoming)))
                    continue;

                // Still builds, under a hashed bundle name, so leave it in place and ask for a rename.
                if (merged.Exists(existing => existing.name == incoming.name))
                {
                    GraphicsTestLogger.Log(
                        LogType.Warning,
                        $"Schema '{incoming.name}' already exists in the Image Build Settings with different "
                            + "content; rename it before importing. Leaving it on GraphicsTestBuildSettings."
                    );
                    keptInBuildSettings.Add(incoming);
                    continue;
                }

                merged.Add(incoming.Clone());
            }

            Undo.RecordObject(imageSettings, "Import PlatformSchemata");
            imageSettings.PlatformSchemata = merged.ToArray();
            EditorUtility.SetDirty(imageSettings);

            Undo.RecordObject(buildSettings, "Clear PlatformSchemata");
            buildSettings.PlatformSchemata = keptInBuildSettings.ToArray();
            buildSettings.Save();

            AssetDatabase.SaveAssets();
        }
    }
}
