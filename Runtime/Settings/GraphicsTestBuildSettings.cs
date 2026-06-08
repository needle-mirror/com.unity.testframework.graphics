using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Serialization;
using UnityEngine.TestTools.Graphics.Platforms;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A class that holds settings for the Graphics Test Build.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(
        fileName = "GraphicsTestBuildSettings.asset",
        menuName = "Graphics Test Framework/Graphics Test Build Settings"
    )]
    public class GraphicsTestBuildSettings : ScriptableObject, ISerializationCallbackReceiver
    {
        const string k_DefaultSavePath = "Assets/Resources/GraphicsTestBuildSettings.asset";
        const int k_DefaultMaxConcurrentOptimizations = 2;
        internal const string k_DefaultActualImagesPath = "Assets/ActualImages";

        /// <summary>
        /// The CLI argument for saving actual images. Used to override the <see cref="SaveActualImages"/> property.
        /// </summary>
        public const string k_SaveActualImagesArgument = "-save-actual-images";

        /// <summary>
        /// The CLI argument for overriding ignore attributes. Used to override the <see cref="OverrideIgnoreAttributes"/> property.
        /// </summary>
        public const string k_OverrideIgnoreAttributesArgument = "-override-ignore-attributes";

        /// <summary>
        /// The CLI argument for shader warnings as errors. Used to override the <see cref="ShaderWarningsAsErrors"/> property.
        /// </summary>
        public const string k_ShaderWarningsAsErrorsArgument = "-shader-warnings-as-errors";

        /// <summary>
        /// The CLI argument for enabling shader stripping. Used to override the <see cref="EnableShaderStripping"/> property.
        /// </summary>
        public const string k_EnableShaderStrippingArgument = "-enable-shader-stripping";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void LoadSettingsOnLoad()
        {
            LoadOrDefault();
        }

        /// <summary>
        /// The path to the GraphicsTestBuildSettings asset.
        /// </summary>
        public string SavePath { get; set; } = k_DefaultSavePath;

        /// <summary>
        /// Constructor for the GraphicsTestBuildSettings class.
        /// </summary>
        public GraphicsTestBuildSettings() { }

        [SerializeField]
        [Tooltip("Automatically build test cases when launching a test run.")]
        bool m_AutoBuildTestCases = true;

        [SerializeField]
        [Tooltip("The test content bundles paths included in the build.")]
        string[] m_TestContentBundlePaths = Array.Empty<string>();

        [SerializeField]
        [Tooltip("The directory to the image results.")]
        string m_ImageResultsPath = string.Empty;

        [SerializeField]
        [Tooltip("Whether to save actual images when running tests, even if they pass.")]
        bool m_SaveActualImages;

        [SerializeField]
        [Tooltip("Whether to override the ignore attributes when running tests.")]
        bool m_OverrideIgnoreAttributes;

        [SerializeField]
        [Tooltip("Treat shader warnings as errors. When enabled, shader warnings detected during builds or test runs will fail the job.")]
        bool m_ShaderWarningsAsErrors;

        [SerializeField]
        [Tooltip(
            "When rebuilding, remove ignored graphics-test scenes from the Build Settings. Disable to keep existing scenes in their current order. Scenes that are not part of any graphics test are never removed."
        )]
        bool m_ClearBuildSettingsScenesOnRebuild = true;

        [SerializeField]
        [HideInInspector]
        string[] m_PreviousScenesPaths = Array.Empty<string>();

        [SerializeField]
        [HideInInspector]
        bool[] m_PreviousScenesEnabled = Array.Empty<bool>();

        [FormerlySerializedAs("m_EnableFileSystemWatcher")]
        [SerializeField]
        [Tooltip(
            "Use a file system watcher to automatically track changes in SceneGraphicsTestCase scene directories and trigger a domain reload after each change."
        )]
        bool m_ReloadDomainWhenEditingTestSceneAssets = true;

        [SerializeField]
        [Tooltip(
            "Optimize your reference images automatically when adding, removing or editing reference image files."
        )]
        bool m_AutoOptimizeReferenceImages;

        [SerializeField]
        [Tooltip("Whether to enable shader stripping in the build. This is useful for reducing the size of the build.")]
        bool m_EnableShaderStripping = true;

        [SerializeField]
        [HideInInspector]
        bool m_ShouldCleanUpAfterBuild;

        [SerializeField]
        [Tooltip("The color scheme to use for visualizing the divergence among reference images.")]
        HeatmapColorScheme m_HeatmapColorScheme = HeatmapColorScheme.BlueYellow;

        [SerializeField]
        [Tooltip(
            "The maximum number of concurrent image optimizations that can be run at the same time. This is useful for limiting the number of concurrent optimizations to avoid overwhelming the system."
        )]
        int m_MaxConcurrentImageOptimizations = k_DefaultMaxConcurrentOptimizations;

        [SerializeField]
        PlatformSchema[] m_PlatformSchemata = { PlatformSchema.k_DefaultSchema, PlatformSchema.k_DefaultSchemaBase };

        [SerializeField]
        PlatformSchema[] m_BuildPlatformSchemata = { };

        [SerializeField]
        string[] m_BuildPlatformNames;

        [SerializeField]
        List<SceneList> m_SceneLists = new();

        /// <summary>
        /// A boolean that determines whether to automatically build test cases when building the player.
        /// </summary>
        /// <remarks>
        /// This is used to automatically build test cases when launching a test run.
        /// </remarks>
        public bool AutoBuildTestCases
        {
            get => m_AutoBuildTestCases;
            set => m_AutoBuildTestCases = value;
        }

        /// <summary>
        /// The paths to the test content bundles included in the build.
        /// </summary>
        /// <remarks>
        /// The test content bundles are used to load test assets for the tests.
        /// </remarks>
        public string[] TestContentBundlePaths
        {
            get => m_TestContentBundlePaths;
            set => m_TestContentBundlePaths = value;
        }

        /// <summary>
        /// The path to the image results.
        /// </summary>
        public string ImageResultsPath
        {
            get => m_ImageResultsPath;
            set => m_ImageResultsPath = value;
        }

        /// <summary>
        /// The resolved path for actual/result images. Falls back to
        /// <c>Assets/ActualImages</c> when <see cref="ImageResultsPath"/> is empty.
        /// </summary>
        public string ActualImagesPath =>
            string.IsNullOrEmpty(m_ImageResultsPath) ? k_DefaultActualImagesPath : m_ImageResultsPath;

        /// <summary>
        /// Whether to save actual images when running tests, even if they pass.
        /// </summary>
        public bool SaveActualImages
        {
            get => m_SaveActualImages;
            set => m_SaveActualImages = value;
        }

        /// <summary>
        /// Whether to save actual images when running tests, even if they pass.
        /// </summary>
        public bool OverrideIgnoreAttributes
        {
            get => m_OverrideIgnoreAttributes;
            set => m_OverrideIgnoreAttributes = value;
        }

        /// <summary>
        /// When enabled, shader warnings detected during builds or test runs are treated as errors
        /// and will fail the job. Opt-in via the <c>-shader-warnings-as-errors</c> CLI flag.
        /// </summary>
        public bool ShaderWarningsAsErrors
        {
            get => m_ShaderWarningsAsErrors;
            set => m_ShaderWarningsAsErrors = value;
        }

        /// <summary>
        /// Whether to remove ignored graphics-test scenes from the Build Settings when rebuilding.
        /// When disabled, scenes already present are kept in their current order. Scenes that are
        /// not part of any graphics test are never removed.
        /// </summary>
        public bool ClearBuildSettingsScenesOnRebuild
        {
            get => m_ClearBuildSettingsScenesOnRebuild;
            set => m_ClearBuildSettingsScenesOnRebuild = value;
        }

        /// <summary>
        /// The paths to the previous scenes. Used to restore the previous scenes after the build.
        /// </summary>
        public string[] PreviousScenesPaths
        {
            get => m_PreviousScenesPaths;
            set => m_PreviousScenesPaths = value;
        }

        /// <summary>
        /// The enabled states of the previous scenes. Used to restore the previous scenes after the build.
        /// </summary>
        public bool[] PreviousScenesEnabled
        {
            get => m_PreviousScenesEnabled;
            set => m_PreviousScenesEnabled = value;
        }

        /// <summary>
        /// Whether to use a file system watcher to automatically track changes in SceneGraphicsTestCase scene directories and trigger a domain reload after each change.
        /// </summary>
        public bool ReloadDomainWhenEditingTestSceneAssets
        {
            get => m_ReloadDomainWhenEditingTestSceneAssets;
            set => m_ReloadDomainWhenEditingTestSceneAssets = value;
        }

        /// <summary>
        /// Optimize your reference images automatically when adding, removing or editing reference image files.
        /// </summary>
        public bool AutoOptimizeReferenceImages
        {
            get => m_AutoOptimizeReferenceImages;
            set => m_AutoOptimizeReferenceImages = value;
        }

        /// <summary>
        /// Whether to enable shader stripping in the build.
        /// </summary>
        public bool EnableShaderStripping
        {
            get => m_EnableShaderStripping;
            set => m_EnableShaderStripping = value;
        }

        /// <summary>
        /// A boolean that determines whether to clean up the Editor state after the build.
        /// </summary>
        public bool ShouldCleanUpAfterBuild
        {
            get => m_ShouldCleanUpAfterBuild;
            set => m_ShouldCleanUpAfterBuild = value;
        }

        /// <summary>
        /// The color scheme to use for visualizing the divergence among reference images.
        /// </summary>
        public HeatmapColorScheme HeatmapColorScheme
        {
            get => m_HeatmapColorScheme;
            set => m_HeatmapColorScheme = value;
        }

        /// <summary>
        /// The maximum number of concurrent image optimizations that can be run at the same time.
        /// </summary>
        /// <remarks>
        /// This is useful for limiting the number of concurrent optimizations to avoid overwhelming the system.
        /// </remarks>
        public int MaxConcurrentImageOptimizations
        {
            get => m_MaxConcurrentImageOptimizations;
            set => m_MaxConcurrentImageOptimizations = value;
        }

        /// <summary>
        /// The platform schemata to use in this project.
        /// </summary>
        /// <remarks>
        /// The schemata are resolved in sequence, meaning that test cases that have their test assets
        /// found using one schema will be skipped by latter schemata. It is recommended to order the
        /// schemata in order of specificity or priority.
        /// </remarks>
        public PlatformSchema[] PlatformSchemata
        {
            get => m_PlatformSchemata;
            set => m_PlatformSchemata = value;
        }

        /// <summary>
        /// The platform schemata used in the most recent build.
        /// </summary>
        public PlatformSchema[] BuildPlatformSchemata
        {
            get => m_BuildPlatformSchemata;
            set => m_BuildPlatformSchemata = value;
        }

        /// <summary>
        /// The names of the platforms used in the most recent build.
        /// </summary>
        public string[] BuildPlatformNames
        {
            get => m_BuildPlatformNames;
            set => m_BuildPlatformNames = value;
        }

        internal Dictionary<MethodIdentifier, List<string>> ScenePathsDictionary { get; private set; } = new();

        static GraphicsTestBuildSettings s_Instance;

        /// <summary>
        /// Clears the cached singleton so the next <see cref="LoadOrDefault"/> call
        /// reloads from disk. Intended for tests only.
        /// </summary>
        internal static void ResetInstance() => s_Instance = null;

#if UNITY_EDITOR
        /// <summary>
        /// Settable asset operations that the Editor assembly can wire through <c>IAssetService</c>.
        /// Defaults fall back to raw <see cref="AssetDatabase"/> / <see cref="EditorUtility"/> calls.
        /// </summary>
        internal static class AssetOps
        {
            internal static Func<string, GraphicsTestBuildSettings> LoadSettings =
                path => AssetDatabase.LoadAssetAtPath<GraphicsTestBuildSettings>(path);
            internal static Func<string, Object[]> LoadAllAssetsAtPath = AssetDatabase.LoadAllAssetsAtPath;
            internal static Func<Object, string> GetAssetPath = AssetDatabase.GetAssetPath;
            internal static Action<Object, string> CreateAsset = AssetDatabase.CreateAsset;
            internal static Action<Object, Object> AddObjectToAsset = AssetDatabase.AddObjectToAsset;
            internal static Action<Object> SetDirty = EditorUtility.SetDirty;
            internal static Action SaveAssets = AssetDatabase.SaveAssets;
            internal static Action Refresh = AssetDatabase.Refresh;
        }
#endif

        /// <summary>
        /// Returns the cached settings instance, loading from disk or creating defaults if needed.
        /// </summary>
        /// <returns>
        /// The cached or newly loaded settings instance.
        /// </returns>
        public static GraphicsTestBuildSettings LoadOrDefault()
        {
            if (s_Instance != null)
                return s_Instance;

            s_Instance = Load(k_DefaultSavePath);

            if (s_Instance == null)
            {
                s_Instance = Create();
            }

            return s_Instance;
        }

        /// <summary>
        /// Create a new GraphicsTestBuildSettings.
        /// </summary>
        /// <remarks>
        /// This method will create a new GraphicsTestBuildSettings and set the name to the file name without extension.
        /// </remarks>
        /// <returns>
        /// The created GraphicsTestBuildSettings.
        /// </returns>
        public static GraphicsTestBuildSettings Create()
        {
            var newSettings = CreateInstance<GraphicsTestBuildSettings>();

            newSettings.name = Path.GetFileNameWithoutExtension(newSettings.SavePath);
            newSettings.MaxConcurrentImageOptimizations = Math.Max(
                k_DefaultMaxConcurrentOptimizations,
                SystemInfo.processorCount / 2
            );

            newSettings.Save();
            return newSettings;
        }

        /// <summary>
        /// Save the GraphicsTestBuildSettings.
        /// </summary>
        /// <remarks>
        /// This method will save the GraphicsTestBuildSettings to the specified path.
        /// If the path does not exist, it will create the directory.
        /// If the path exists, it will mark the asset as dirty and save it.
        /// </remarks>
        /// <returns>
        /// True if the save was successful, false otherwise.
        /// </returns>
        public bool Save()
        {
#if UNITY_EDITOR
            if (string.IsNullOrWhiteSpace(SavePath))
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    "GraphicsTestBuildSettings: SavePath is null or empty. Cannot save asset."
                );
                return false;
            }

            try
            {
                var existingPath = AssetOps.GetAssetPath(this);
                if (string.IsNullOrEmpty(existingPath))
                {
                    var dir = Path.GetDirectoryName(SavePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    AssetOps.CreateAsset(this, SavePath);
                }
                else
                {
                    AssetOps.SetDirty(this);
                }

                AssetOps.SaveAssets();
                AssetOps.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.LogException(ex);
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Load the GraphicsTestBuildSettings from disk.
        /// </summary>
        /// <param name="loadPath">Asset path (editor) or Resources-relative path (runtime).</param>
        /// <returns>The loaded settings, or <c>null</c> if the asset does not exist.</returns>
        public static GraphicsTestBuildSettings Load(string loadPath)
        {
#if UNITY_EDITOR
            return AssetOps.LoadSettings(loadPath);
#else
            return LoadFromResources(loadPath);
#endif
        }

        /// <summary>
        /// Runtime-only load: uses <see cref="Resources.LoadAll"/> to retrieve the main
        /// asset together with its <see cref="SceneList"/> sub-assets and populates
        /// <see cref="ScenePathsDictionary"/>.
        /// </summary>
        static GraphicsTestBuildSettings LoadFromResources(string loadPath)
        {
            var results = Resources.LoadAll(Path.GetFileNameWithoutExtension(loadPath));
            if (results.Length == 0)
                return null;

            GraphicsTestBuildSettings mainAsset = null;
            var sceneLists = new List<SceneList>();
            foreach (var r in results)
            {
                if (mainAsset == null && r is GraphicsTestBuildSettings gts)
                    mainAsset = gts;
                else if (r is SceneList sl)
                    sceneLists.Add(sl);
            }

            if (mainAsset != null && sceneLists.Count > 0)
                mainAsset.PopulateScenePathsDictionaryFromSceneLists(sceneLists);

            return mainAsset;
        }

        internal void PopulateScenePathsDictionaryFromSceneLists(IEnumerable<SceneList> sceneLists)
        {
            ScenePathsDictionary = new();
            foreach (var sceneList in sceneLists)
            {
                ScenePathsDictionary[sceneList.id] = sceneList.ScenePaths;
            }
        }

#if UNITY_EDITOR
        internal void ClearSceneLists()
        {
            var path = AssetOps.GetAssetPath(this);
            var assets = AssetOps.LoadAllAssetsAtPath(path);

            foreach (var obj in assets)
            {
                if (obj is SceneList sceneList)
                    DestroyImmediate(sceneList, true);
            }

            m_SceneLists.Clear();
            AssetOps.SaveAssets();
            AssetOps.Refresh();
        }

        internal void AddSceneList(SceneList sceneList)
        {
            AssetOps.AddObjectToAsset(sceneList, this);
            m_SceneLists.Add(sceneList);
        }
#endif

        /// <summary>
        /// Restore the previous scenes in the EditorBuildSettings.
        /// </summary>
        internal void RestoreEditorBuildSettings()
        {
#if UNITY_EDITOR
            if (PreviousScenesPaths == null || PreviousScenesEnabled == null)
                return;

            if (PreviousScenesPaths.Length != PreviousScenesEnabled.Length)
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    $"GraphicsTestBuildSettings: Previous scenes paths and enabled states do not match. Cannot restore EditorBuildSettings."
                );
                return;
            }

            var scenes = new EditorBuildSettingsScene[PreviousScenesPaths.Length];
            for (var i = 0; i < PreviousScenesPaths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(PreviousScenesPaths[i], PreviousScenesEnabled[i]);
            }

            // Restore the previous scenes
            EditorBuildSettings.scenes = scenes;
            Save();
#endif
        }

        internal void OverwriteSettingsFromCommandLine()
        {
            var reader = RuntimeSettings.CommandLineReader;
            reader.SetFlagIfPresent(ref m_SaveActualImages, k_SaveActualImagesArgument);
            reader.SetFlagIfPresent(ref m_OverrideIgnoreAttributes, k_OverrideIgnoreAttributesArgument);
            reader.SetFlagIfPresent(ref m_ShaderWarningsAsErrors, k_ShaderWarningsAsErrorsArgument);
            reader.UpdateFromArgument(ref m_EnableShaderStripping, k_EnableShaderStrippingArgument, bool.Parse);
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnBeforeSerialize"/>
        public void OnBeforeSerialize()
        {
            if (m_PreviousScenesPaths != null)
            {
                for (var i = 0; i < m_PreviousScenesPaths.Length; i++)
                    m_PreviousScenesPaths[i] = m_PreviousScenesPaths[i].SanitizeBackslashes().Trim('/');
            }
            m_ImageResultsPath = m_ImageResultsPath.SanitizeBackslashes().Trim('/');
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnAfterDeserialize"/>
        public void OnAfterDeserialize() { }
    }
}
