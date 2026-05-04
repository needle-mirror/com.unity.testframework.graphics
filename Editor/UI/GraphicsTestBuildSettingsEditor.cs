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

        void OnEnable()
        {
            if (target == null)
                return;

            m_AutoBuildProp = serializedObject.FindProperty("m_AutoBuildTestCases");
            m_TestBundlesProp = serializedObject.FindProperty("m_TestContentBundlePaths");
            m_ScenePathsProp = serializedObject.FindProperty("m_ScenePaths");
            m_ImageResultsProp = serializedObject.FindProperty("m_ImageResultsPath");
            m_SaveActualImagesProp = serializedObject.FindProperty("m_SaveActualImages");
            m_OverrideIgnoreProp = serializedObject.FindProperty("m_OverrideIgnoreAttributes");
            m_ShaderWarningsAsErrorsProp = serializedObject.FindProperty("m_ShaderWarningsAsErrors");
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
            EditorGUILayout.PropertyField(m_PlatformSchemaProp);
            var hasInvalidPlatform = false;
            var settings = serializedObject.targetObject as GraphicsTestBuildSettings;
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
            EditorGUILayout.PropertyField(m_BuildPlatformSchemaProp);
            EditorGUILayout.Space();

            // Save the settings to automatically
            if (serializedObject.ApplyModifiedProperties())
            {
                ((GraphicsTestBuildSettings)target).Save();
            }
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
