using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.UI
{
    [CustomEditor(typeof(GraphicsTestBuildSettings))]
    class GraphicsTestBuildSettingsEditor : Editor
    {
        ReorderableList m_ReferenceImagesList;
        ReorderableList m_PlatformSchemata;
        ReorderableList m_PlatformNodeList;
        ReorderableList m_ScenePaths;
        ReorderableList m_BundlePaths;
        ReorderableList m_BuildPlatforms;
        ReorderableList m_SceneLists;

        SerializedProperty m_AutoBuildProp;
        SerializedProperty m_TestBundlesProp;
        SerializedProperty m_ScenePathsProp;
        SerializedProperty m_ImageResultsProp;
        SerializedProperty m_SaveActualImagesProp;
        SerializedProperty m_OverrideIgnoreProp;
        SerializedProperty m_ShaderWarningsAsErrorsProp;
        SerializedProperty m_ClearBuildSettingsScenesOnRebuildProp;
        SerializedProperty m_AutoOptimizeProp;
        SerializedProperty m_ReloadDomainProp;
        SerializedProperty m_ShaderStrippingProp;
        SerializedProperty m_HeatmapColorSchemeProp;
        SerializedProperty m_MaxImageOptimizationConcurrencyProp;
        SerializedProperty m_PlatformSchemaProp;
        SerializedProperty m_BuildPlatformSchemaProp;
        SerializedProperty m_BuildPlatformsProp;
        SerializedProperty m_SceneListsProp;

        bool m_OptimizerFoldout = true;
        bool m_AdvancedFoldout = true;
        bool m_VisualizationFoldout = true;
        bool m_ContributedFoldout = true;

        /// <summary>
        /// A schema an Image Build Settings asset adds to the build, and whether the union already
        /// reads the same folders, in which case the build drops this copy.
        /// </summary>
        readonly List<(string asset, PlatformSchema schema, bool collapsed)> m_Contributed = new();

        /// <summary>
        /// Rebuilding scans the whole project, so it happens on selection and on project changes
        /// rather than per repaint.
        /// </summary>
        bool m_ContributedDirty = true;

        void OnEnable()
        {
            if (target == null)
                return;

            m_ContributedDirty = true;
            EditorApplication.projectChanged += MarkContributedDirty;

            m_AutoBuildProp = serializedObject.FindProperty("m_AutoBuildTestCases");
            m_TestBundlesProp = serializedObject.FindProperty("m_TestContentBundlePaths");
            m_ScenePathsProp = serializedObject.FindProperty("m_ScenePaths");
            m_ImageResultsProp = serializedObject.FindProperty("m_ImageResultsPath");
            m_SaveActualImagesProp = serializedObject.FindProperty("m_SaveActualImages");
            m_OverrideIgnoreProp = serializedObject.FindProperty("m_OverrideIgnoreAttributes");
            m_ShaderWarningsAsErrorsProp = serializedObject.FindProperty("m_ShaderWarningsAsErrors");
            m_ClearBuildSettingsScenesOnRebuildProp = serializedObject.FindProperty("m_ClearBuildSettingsScenesOnRebuild");
            m_AutoOptimizeProp = serializedObject.FindProperty("m_AutoOptimizeReferenceImages");
            m_ReloadDomainProp = serializedObject.FindProperty("m_ReloadDomainWhenEditingTestSceneAssets");
            m_ShaderStrippingProp = serializedObject.FindProperty("m_EnableShaderStripping");
            m_HeatmapColorSchemeProp = serializedObject.FindProperty("m_HeatmapColorScheme");
            m_MaxImageOptimizationConcurrencyProp = serializedObject.FindProperty("m_MaxConcurrentImageOptimizations");
            m_BuildPlatformsProp = serializedObject.FindProperty("m_BuildPlatformNames");
            m_PlatformSchemaProp = serializedObject.FindProperty("m_PlatformSchemata");
            m_BuildPlatformSchemaProp = serializedObject.FindProperty("m_BuildPlatformSchemata");
            m_SceneListsProp = serializedObject.FindProperty("m_SceneLists");

            m_ScenePaths = CreateReorderableList(m_ScenePathsProp, "Scene Paths", false);
            m_BundlePaths = CreateReorderableList(m_TestBundlesProp, "Test Content Bundle Paths", false);
            m_BuildPlatforms = CreateReorderableList(m_BuildPlatformsProp, "Build Platforms", false);

            m_SceneLists = CreateReorderableList(m_SceneListsProp, "Scene Lists", false);
            m_PlatformSchemata = CreatePlatformSchemataList();
        }

        /// <summary>
        /// Editable but not draggable: the order of this list does not decide which schema resolves
        /// first, so a drag handle would promise something it cannot deliver. Marking the field
        /// <c>[NonReorderable]</c> would do the same, but it drops the whole list control in favour
        /// of the legacy array drawer and its Size field, which resizes by copying the last entry —
        /// silently producing a duplicate schema the build then discards.
        /// </summary>
        ReorderableList CreatePlatformSchemataList()
        {
            // No header: the section's foldout already labels it.
            return new ReorderableList(serializedObject, m_PlatformSchemaProp, false, false, true, true)
            {
                elementHeightCallback = index =>
                    EditorGUI.GetPropertyHeight(m_PlatformSchemaProp.GetArrayElementAtIndex(index), true) + 4,
                drawElementCallback = (rect, index, _, _) =>
                {
                    var element = m_PlatformSchemaProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    rect.height = EditorGUI.GetPropertyHeight(element, true);

                    var schemaName = element.FindPropertyRelative("name")?.stringValue;
                    var label = string.IsNullOrEmpty(schemaName) ? $"Schema {index}" : schemaName;
                    EditorGUI.PropertyField(rect, element, new GUIContent(label), true);
                },
                onAddCallback = list =>
                {
                    var index = m_PlatformSchemaProp.arraySize;
                    m_PlatformSchemaProp.InsertArrayElementAtIndex(index);

                    // The default add copies the last entry, which resolves the same folders and is
                    // dropped as a duplicate. Start blank so the new entry is one the user fills in.
                    var added = m_PlatformSchemaProp.GetArrayElementAtIndex(index);
                    added.FindPropertyRelative("name").stringValue = string.Empty;
                    added.FindPropertyRelative("rootPath").stringValue = string.Empty;
                    added.FindPropertyRelative("nodes").ClearArray();
                    list.index = index;
                },
            };
        }

        void OnDisable() => EditorApplication.projectChanged -= MarkContributedDirty;

        void MarkContributedDirty() => m_ContributedDirty = true;

        /// <summary>
        /// Walks the sources the way <see cref="Builder.GraphicsTestAutoBuilder.CollectSchemata"/>
        /// does — this asset first, then discovered assets — so the entries marked collapsed are the
        /// ones the build actually drops.
        /// </summary>
        void RebuildContributedSchemata(GraphicsTestBuildSettings settings)
        {
            m_ContributedDirty = false;
            m_Contributed.Clear();

            var seen = new HashSet<PlatformSchema>(new PlatformSchemaResolutionComparer());
            foreach (var schema in settings?.PlatformSchemata ?? System.Array.Empty<PlatformSchema>())
            {
                if (schema != null)
                    seen.Add(schema);
            }

            foreach (var asset in ImageBuildSettingsUtility.DiscoverImageBuildSettings())
            {
                if (asset == null)
                    continue;

                var assetName = AssetDatabase.GetAssetPath(asset);
                foreach (var schema in asset.PlatformSchemata ?? System.Array.Empty<PlatformSchema>())
                {
                    if (schema != null)
                        m_Contributed.Add((assetName, schema, !seen.Add(schema)));
                }
            }
        }

        void DrawContributedSchemata()
        {
            if (m_Contributed.Count == 0)
                return;

            EditorGUILayout.Space();
            m_ContributedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_ContributedFoldout,
                $"Contributed by Image Build Settings ({m_Contributed.Count})"
            );

            if (m_ContributedFoldout)
            {
                var prevEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUI.indentLevel++;
                string lastAsset = null;
                foreach (var (asset, schema, collapsed) in m_Contributed)
                {
                    if (asset != lastAsset)
                    {
                        GUI.enabled = prevEnabled;
                        EditorGUILayout.ObjectField(
                            AssetDatabase.LoadAssetAtPath<GraphicsTestImageBuildSettings>(asset),
                            typeof(GraphicsTestImageBuildSettings),
                            false
                        );
                        GUI.enabled = false;
                        lastAsset = asset;
                    }

                    EditorGUI.indentLevel++;
                    var label = collapsed
                        ? $"{schema.name} — already covered by this asset, not built"
                        : $"{schema.name} — {schema.rootPath}";
                    EditorGUILayout.LabelField(label);
                    EditorGUI.indentLevel--;
                }

                EditorGUI.indentLevel--;
                GUI.enabled = prevEnabled;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        public override void OnInspectorGUI()
        {
            if (target == null || m_AutoBuildProp == null)
                return;

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_AutoBuildProp);

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_ImageResultsProp);
            EditorGUILayout.Space();
            var settings = serializedObject.targetObject as GraphicsTestBuildSettings;

            // The list control draws no foldout of its own, so drive one from the same isExpanded
            // the default array drawer used, keeping the section collapsed or open across selections.
            m_PlatformSchemaProp.isExpanded = EditorGUILayout.Foldout(
                m_PlatformSchemaProp.isExpanded,
                $"Platform Schemata ({m_PlatformSchemaProp.arraySize})",
                true
            );

            if (m_PlatformSchemaProp.isExpanded)
            {
                m_PlatformSchemata.DoLayoutList();

                var prevExtractEnabled = GUI.enabled;
                GUI.enabled = m_PlatformSchemaProp.arraySize > 0;
                if (GUILayout.Button("Extract to Image Build Settings"))
                    ExtractToImageBuildSettings(settings);
                GUI.enabled = prevExtractEnabled;
            }

            var hasInvalidPlatform = false;
            if (settings?.PlatformSchemata != null)
            {
                foreach (var p in settings.PlatformSchemata)
                {
                    if (p.hasInvalidNodeNames)
                    {
                        hasInvalidPlatform = true;
                        break;
                    }
                }
            }
            if (hasInvalidPlatform)
            {
                var validNames = new List<string>();
                foreach (var t in PlatformNodeRegistry.k_EnumTypes)
                    validNames.Add(t.Name);
                EditorGUILayout.HelpBox($"Valid node values: {string.Join(", ", validNames)}", MessageType.Warning);
            }

            if (m_ContributedDirty)
                RebuildContributedSchemata(settings);
            DrawContributedSchemata();

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            m_OptimizerFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                m_OptimizerFoldout,
                "Reference Image Optimization"
            );
            if (m_OptimizerFoldout)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_AutoOptimizeProp, new GUIContent("Auto Optimize"));
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Concurrency Limit", GUILayout.Width(EditorGUIUtility.labelWidth - 4));
                var min = 1;
                var max = SystemInfo.processorCount;
                var current = m_MaxImageOptimizationConcurrencyProp.intValue;
                var newValue = EditorGUILayout.IntSlider(current, min, max);
                if (newValue != current)
                {
                    m_MaxImageOptimizationConcurrencyProp.intValue = newValue;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            m_VisualizationFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_VisualizationFoldout, "Visualization");
            if (m_VisualizationFoldout)
            {
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(m_HeatmapColorSchemeProp, new GUIContent("Heatmap Color Scheme"));
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            m_AdvancedFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(m_AdvancedFoldout, "Advanced");
            if (m_AdvancedFoldout)
            {
                EditorGUILayout.PropertyField(m_SaveActualImagesProp, new GUIContent("Always Save Results"));
                EditorGUILayout.PropertyField(m_ReloadDomainProp, new GUIContent("Enable Scene Watcher"));
                EditorGUILayout.PropertyField(m_ShaderStrippingProp);
                EditorGUILayout.PropertyField(m_OverrideIgnoreProp);
                EditorGUILayout.PropertyField(m_ShaderWarningsAsErrorsProp, new GUIContent("Shader Warnings as Errors"));
                EditorGUILayout.PropertyField(
                    m_ClearBuildSettingsScenesOnRebuildProp,
                    new GUIContent("Clear Build Settings Scenes On Rebuild")
                );
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.Space();

            EditorGUILayout.Separator();

            EditorGUILayout.TextArea("Latest Build Settings (Read Only)", EditorStyles.boldLabel);

            EditorGUILayout.Space();
            m_ScenePaths.DoLayoutList();
            m_BundlePaths.DoLayoutList();
            m_BuildPlatforms.DoLayoutList();
            m_SceneLists.DoLayoutList();
            // Recorded by the build in the order it searched them.
            var prevSchemaEnabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUILayout.PropertyField(m_BuildPlatformSchemaProp);
            GUI.enabled = prevSchemaEnabled;
            EditorGUILayout.Space();

            if (serializedObject.ApplyModifiedProperties())
            {
                ((GraphicsTestBuildSettings)target).Save();
            }
        }

        void ExtractToImageBuildSettings(GraphicsTestBuildSettings settings)
        {
            if (settings?.PlatformSchemata == null)
                return;

            var path = EditorUtility.SaveFilePanelInProject(
                "Save Image Build Settings",
                "GraphicsTestImageBuildSettings",
                "asset",
                "Choose where to save the extracted Image Build Settings"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var extracted = new PlatformSchema[settings.PlatformSchemata.Length];
            for (var i = 0; i < extracted.Length; i++)
                extracted[i] = settings.PlatformSchemata[i]?.Clone();

            var newAsset = CreateInstance<GraphicsTestImageBuildSettings>();
            newAsset.PlatformSchemata = extracted;
            AssetDatabase.CreateAsset(newAsset, path);

            m_PlatformSchemaProp.ClearArray();
            serializedObject.ApplyModifiedProperties();
            settings.Save();

            AssetDatabase.SaveAssets();
            m_ContributedDirty = true;
            Selection.activeObject = newAsset;
            EditorGUIUtility.PingObject(newAsset);
        }

        ReorderableList CreateReorderableList(SerializedProperty property, string label, bool isEditable = true)
        {
            var list = new ReorderableList(serializedObject, property, isEditable, true, isEditable, isEditable)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, label);
                },
                drawElementCallback = (rect, index, _, _) =>
                {
                    var element = property.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;

                    var prevEnabled = GUI.enabled;
                    GUI.enabled = isEditable;
                    EditorGUI.PropertyField(rect, element, GUIContent.none);
                    GUI.enabled = prevEnabled;
                },
            };

            return list;
        }
    }
}
