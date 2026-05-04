using System;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.UIElements;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow : EditorWindow, IHasCustomMenu
    {
        const int k_WindowPriority = 203;
        const string k_WindowName = "Graphics Tests";
        const string k_WindowMenuPath = "Window/General/" + k_WindowName;
        const string k_WindowIconPath =
            "Packages/com.unity.testframework.graphics/Editor/UI/Assets/gtf-icon-flask-conical.png";
        const string k_WindowStyleSheetPath =
            "Packages/com.unity.testframework.graphics/Editor/UI/GraphicsTestsWindow/GraphicsTestsWindow.uss";

        const string k_DocsSiteUrl = "https://docs.unity3d.com/Packages/com.unity.testframework.graphics@latest/";

        [SerializeField]
        VisualTreeAsset uxmlAsset;

        [SerializeField]
        StyleSheet ussAsset;

        internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

        VisualElement m_Root;
        TabView m_TabView;
        bool m_IsInitialized;

        static readonly Type[] k_PreferredDockTargets =
        {
            typeof(TestRunner.TestRunnerWindow),
            Type.GetType("UnityEditor.InspectorWindow, UnityEditor"),
        };

        [MenuItem(k_WindowMenuPath, false, k_WindowPriority)]
        public static void OpenWindow()
        {
            CreateOrShowWindow();
        }

        internal static GraphicsTestsWindow CreateOrShowWindow()
        {
            GraphicsTestsWindow window = null;
            if (HasOpenInstances<GraphicsTestsWindow>())
            {
                FocusWindowIfItsOpen(typeof(GraphicsTestsWindow));
                foreach (var w in Resources.FindObjectsOfTypeAll<GraphicsTestsWindow>())
                {
                    window = w;
                    break;
                }
            }

            if (window == null)
                window = CreateWindow<GraphicsTestsWindow>(desiredDockNextTo: k_PreferredDockTargets);

            window.Show();
            window.Focus();
            return window;
        }

        void CreateGUI()
        {
            titleContent = new GUIContent(k_WindowName, EditorGUIUtility.Load(k_WindowIconPath) as Texture2D);
        }

        void OnEnable()
        {
            rootVisualElement.Clear();
            uxmlAsset.CloneTree(rootVisualElement);
            m_Root = rootVisualElement;
            m_Root.styleSheets.Add(AssetService.LoadAssetAtPath<StyleSheet>(k_WindowStyleSheetPath));

            m_TabView = m_Root.Q<TabView>("TabView");
            SetupTreeView();
            SetupImageComparisonView();
            SetupOptimization();
            SetupSettingsInspector();
            SetupLogView();
            SetupIgnoreUtils();

            LoadReferenceImageMetrics();
            LoadTestResults();
            BuildFullTreeModel();
            RestoreTreeState();

            SetupTestCallbacks();
            m_IsInitialized = true;
        }

        void OnFocus()
        {
            if (m_IsInitialized)
            {
                LoadTestResults();
                BuildFullTreeModel();
                m_ShouldUpdateImageComparisonView = true;
            }
        }

        void OnDisable()
        {
            SaveWindowState();
            TearDownTestCallbacks();
            TearDownSettingsInspector();
            TearDownImageComparisonView();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(
                new GUIContent("Open Documentation"),
                false,
                () => Application.OpenURL(k_DocsSiteUrl)
            );

            menu.AddItem(
                new GUIContent("Clear Test Results"),
                false,
                ClearTestResults
            );
        }
    }
}
