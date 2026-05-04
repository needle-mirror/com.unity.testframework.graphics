using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    class GraphicsTestNewComparisonWindow : PopupWindowContent
    {
        const string k_AssetPath =
            "Packages/com.unity.testframework.graphics/Editor/UI/ComparisonModal/GraphicsTestNewComparisonWindow.uxml";

        IAssetService AssetService { get; set; } = new AssetDatabaseService();
        VisualElement m_Root;
        Dictionary<string, PlatformSchema> m_PlatformTrees;

        VisualElement m_XmlModeContainer;
        VisualElement m_PlatformModeContainer;
        VisualElement m_AdhocModeContainer;

        ComparisonType m_CurrentMode = ComparisonType.LoadFromXml;

        /// <summary>
        /// Raised when the user creates a new comparison tab.
        /// Parameters: the tab data and the display label for the tab.
        /// </summary>
        internal static event Action<GraphicsTestsWindow.ImageComparisonTab, string> s_OnComparisonCreated;

        /// <summary>
        /// Raised when the user creates an ad-hoc comparison between two specific images.
        /// This bypasses the test list entirely.
        /// </summary>
        internal static event Action<Texture2D, Texture2D, string, string, string> s_OnAdhocComparisonCreated;

        static readonly Dictionary<ComparisonType, Vector2> k_ModeSizes = new()
        {
            { ComparisonType.LoadFromXml, new Vector2(400, 190) },
            { ComparisonType.CrossPlatform, new Vector2(400, 450) },
            { ComparisonType.Adhoc, new Vector2(400, 210) },
        };

        public override Vector2 GetWindowSize() => k_ModeSizes[m_CurrentMode];

        public override void OnGUI(Rect rect)
        {
            // Intentionally left empty — UI Toolkit handles rendering
        }

        public override void OnOpen()
        {
            var visualTreeAsset = AssetService.LoadAssetAtPath<VisualTreeAsset>(k_AssetPath);
            if (visualTreeAsset == null)
            {
                UnityEngine.Debug.LogError($"Failed to load UXML asset at '{k_AssetPath}'. The Graphics Test Framework package may be corrupted.");
                return;
            }
            visualTreeAsset.CloneTree(editorWindow.rootVisualElement);
            m_Root = editorWindow.rootVisualElement;

            var settings = GraphicsTestBuildSettings.LoadOrDefault();
            m_PlatformTrees = new Dictionary<string, PlatformSchema>();

            foreach (var tree in settings.BuildPlatformSchemata)
                m_PlatformTrees.TryAdd(tree.name, tree);

            foreach (var tree in settings.PlatformSchemata)
                m_PlatformTrees.TryAdd(tree.name, tree);

            m_XmlModeContainer = m_Root.Q<VisualElement>("XmlModeContainer");
            m_PlatformModeContainer = m_Root.Q<VisualElement>("PlatformModeContainer");
            m_AdhocModeContainer = m_Root.Q<VisualElement>("AdhocModeContainer");

            SetupModeToggle();
            SetupXmlMode();
            SetupPlatformMode();
            SetupAdhocMode();
        }

        void SetupModeToggle()
        {
            var resultTypeField = m_Root.Q<EnumField>("ResultTypeField");
            resultTypeField.RegisterValueChangedCallback(evt =>
            {
                var mode = (ComparisonType)evt.newValue;
                m_CurrentMode = mode;
                m_XmlModeContainer.style.display =
                    mode == ComparisonType.LoadFromXml ? DisplayStyle.Flex : DisplayStyle.None;
                m_PlatformModeContainer.style.display =
                    mode == ComparisonType.CrossPlatform ? DisplayStyle.Flex : DisplayStyle.None;
                m_AdhocModeContainer.style.display =
                    mode == ComparisonType.Adhoc ? DisplayStyle.Flex : DisplayStyle.None;

                var size = k_ModeSizes[mode];
                editorWindow.minSize = size;
                editorWindow.maxSize = size;
            });
        }

        // ── Load from XML ──────────────────────────────────────────────

        void SetupXmlMode()
        {
            var xmlSchemaField = m_Root.Q<DropdownField>("XmlSchemaField");
            var sortedSchemaKeys = new List<string>(m_PlatformTrees.Keys);
            sortedSchemaKeys.Sort(StringComparer.Ordinal);
            xmlSchemaField.choices.AddRange(sortedSchemaKeys);
            xmlSchemaField.value = xmlSchemaField.choices.Count > 0 ? xmlSchemaField.choices[0] : "";

            m_Root.Q<Button>("LoadXmlButton").clickable.clicked += () =>
            {
                PlatformSchema schema = null;
                var schemaName = xmlSchemaField.value;
                if (!string.IsNullOrEmpty(schemaName) && m_PlatformTrees.TryGetValue(schemaName, out var platformTree))
                    schema = platformTree;

                if (!ResultsUtility.ExtractImagesFromResultsXml(schema, out var platform))
                    return;

                var tab = new GraphicsTestsWindow.ImageComparisonTab(platform);
                s_OnComparisonCreated?.Invoke(tab, platform.Name);
                editorWindow.Close();
            };
        }

        // ── Cross-Platform Comparison ──────────────────────────────────

        void SetupPlatformMode()
        {
            var schemaField = m_Root.Q<DropdownField>("SchemaField");
            var sortedPlatformKeys = new List<string>(m_PlatformTrees.Keys);
            sortedPlatformKeys.Sort(StringComparer.Ordinal);
            schemaField.choices.AddRange(sortedPlatformKeys);
            schemaField.value = schemaField.choices.Count > 0 ? schemaField.choices[0] : "";
            schemaField.RegisterValueChangedCallback(evt => PopulatePlatformOptions(m_PlatformTrees[evt.newValue]));

            if (schemaField.choices.Count > 0)
            {
                schemaField.value = schemaField.choices[0];
                PopulatePlatformOptions(m_PlatformTrees[schemaField.choices[0]]);
            }

            m_Root.Q<Button>("PlatformCompareButton").clickable.clicked += () =>
            {
                var schemaName = m_Root.Q<DropdownField>("SchemaField").value;
                if (string.IsNullOrEmpty(schemaName))
                    return;

                var schema = m_PlatformTrees[schemaName];
                var platformA = new GraphicsTestPlatform(schema, CollectEnumValues("OptionsA"));
                var platformB = new GraphicsTestPlatform(schema, CollectEnumValues("OptionsB"));

                var imageAPath = platformA.Schema.rootPath + "/" + platformA.ResultsPath;
                var imageBPath = platformB.Schema.rootPath + "/" + platformB.ResultsPath;

                var tab = new GraphicsTestsWindow.ImageComparisonTab(imageAPath, imageBPath)
                {
                    ImageALabel = platformA.Name,
                    ImageBLabel = platformB.Name,
                };
                var label = platformA.Name + " vs " + platformB.Name;

                s_OnComparisonCreated?.Invoke(tab, label);
                editorWindow.Close();
            };
        }

        void PopulatePlatformOptions(PlatformSchema schema)
        {
            PopulateOptionsGroup("OptionsA", schema);
            PopulateOptionsGroup("OptionsB", schema);
        }

        void PopulateOptionsGroup(string groupName, PlatformSchema schema)
        {
            var options = m_Root.Q<GroupBox>(groupName);
            options.Clear();
            foreach (var type in schema.Types)
            {
                var enumField = new EnumField(type.Name, PlatformNodeRegistry.k_Nodes[type.Name].Current);
                options.Add(enumField);
            }
        }

        Enum[] CollectEnumValues(string groupName)
        {
            var values = new List<Enum>();
            foreach (var child in m_Root.Q<GroupBox>(groupName).Children())
            {
                if (child is EnumField enumField)
                    values.Add(enumField.value);
            }
            return values.ToArray();
        }

        // ── Ad-hoc Comparison ──────────────────────────────────────────

        void SetupAdhocMode()
        {
            m_Root.Q<Button>("AdhocCompareButton").clickable.clicked += () =>
            {
                var imageA = m_Root.Q<ObjectField>("ImageAField").value as Texture2D;
                var imageB = m_Root.Q<ObjectField>("ImageBField").value as Texture2D;

                if (imageA == null || imageB == null)
                {
                    Debug.LogWarning("Both Image A and Image B must be assigned.");
                    return;
                }

                var label = imageA.name + " vs " + imageB.name;
                s_OnAdhocComparisonCreated?.Invoke(imageA, imageB, label, imageA.name, imageB.name);
                editorWindow.Close();
            };
        }
    }

    enum ComparisonType
    {
        LoadFromXml,
        CrossPlatform,
        Adhoc,
    }
}
