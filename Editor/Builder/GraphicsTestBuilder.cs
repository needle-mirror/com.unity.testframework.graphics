using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.Graphics.Filtering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// This class is no longer needed. The setup is done automatically. If you wish to override the default settings, use the GraphicsTestBuildSettings asset instead.
    /// You can turn off automatic builds through the asset and control the flow yourself using GraphicsTestBuilder.Build().
    /// If you wish to have custom pre-build steps, use one or more GraphicsPrebuildSetupAttribute.
    /// </summary>
    public class SetupGraphicsTestCases
    {
        /// <summary>
        /// Setup the graphics test cases for the current context.
        /// </summary>
        /// <remarks>
        /// This method is no longer needed. The setup is done automatically. If you wish to override the default settings, use the GraphicsTestBuildSettings asset instead.
        /// You can turn off automatic builds through the asset and control the flow yourself using GraphicsTestBuilder.Build().
        /// If you wish to have custom pre-build steps, use one or more GraphicsPrebuildSetupAttribute.
        /// </remarks>
        /// <param name="rootImageTemplatePath">The path to the root image template.</param>
        /// <param name="imageResultsPath">The path to the image results.</param>
        /// <param name="useCustomRuntimePlatform">Whether to use a custom runtime platform.</param>
        /// <param name="customRuntimePlatform">The custom runtime platform to use.</param>
        [Obsolete(
            "This is no longer needed. The setup is done automatically. If you wish to override the default settings, use the GraphicsTestBuildSettings asset instead. You can turn off automatic builds through the asset and control the flow yourself using GraphicsTestBuilder.Build(). If you wish to have custom pre-build steps, use one or more GraphicsPrebuildSetupAttribute."
        )]
        public static void Setup(
            string rootImageTemplatePath = "",
            string imageResultsPath = "",
            bool useCustomRuntimePlatform = false,
            RuntimePlatform customRuntimePlatform = RuntimePlatform.WindowsPlayer
        )
        {
            throw new NotSupportedException(
                "This is no longer needed. The setup is done automatically. If you wish to override the default settings, use the GraphicsTestBuildSettings asset instead. You can turn off automatic builds through the asset and control the flow yourself using GraphicsTestBuilder.Build(). If you wish to have custom pre-build steps, use one or more GraphicsPrebuildSetupAttribute."
            );
        }

        /// <summary>
        /// Set the game view size.
        /// </summary>
        /// <param name="w">The width of the game view.</param>
        /// <param name="h">The height of the game view.</param>
        /// <remarks>
        /// This method has been moved to the GameViewSize class.
        /// </remarks>
        [Obsolete("This method has been moved. (UnityUpgradable) -> GameViewSize.SetGameViewSize(*)")]
        public static void SetGameViewSize(int w, int h)
        {
            GameViewSize.SetGameViewSize(w, h);
        }
    }
}

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Setup the graphics test cases for the current context.
    /// </summary>
    public class GraphicsTestBuilder
    {
        /// <summary>
        /// The settings to use for building the graphics tests.
        /// </summary>
        /// <remarks>
        /// If not set, the default settings will be used.
        /// </remarks>
        public GraphicsTestBuildSettings Settings { get; set; } = GraphicsTestBuildSettings.LoadOrDefault();

        /// <summary>
        /// The build manager to use for building the graphics tests.
        /// </summary>
        /// <remarks>
        /// If not set, the build manager will be created based on the current context.
        /// </remarks>
        public GraphicsTestBuildManager BuildManager { get; set; }

        /// <summary>
        /// The nodes to build the graphics tests for.
        /// </summary>
        public IList<GraphicsTestPlatform> Platforms { get; set; } =
            new List<GraphicsTestPlatform> { GraphicsTestPlatform.Current };

        /// <summary>
        /// The test cases to build.
        /// </summary>
        public IList<GraphicsTestCase> TestCases { get; set; } = new List<GraphicsTestCase>();

        internal CliSettingsConsistencyValidator CliSettingsConsistencyValidator { get; set; } = new();

        /// <summary>
        /// An event that is fired when the graphics test build is finished.
        /// </summary>
        public static event Action<GraphicsTestBuilder> OnTestBuilderFinished = delegate { };

        /// <summary>
        /// Build the graphics tests for the current context.
        /// </summary>
        /// <remarks>
        /// This method will build the graphics tests for the current context.
        /// If the build manager is not set, it will be created based on the current context.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the build manager is not set.
        /// </exception>
        /// <returns>
        /// The result of the build.
        /// </returns>
        public GraphicsTestBuildResult Build()
        {
            // Create build manager if not set
            if (BuildManager is null)
            {
                throw new InvalidOperationException("BuildManager must be set.");
            }

            // Check Cli Settings Consistency
            var consistencyResult = CliSettingsConsistencyValidator.Validate(
                GraphicsTestPlatform.Current.GetValue<GraphicsDeviceType>(),
                GraphicsTestPlatform.PlayerBuild.GetValue<GraphicsDeviceType>(),
                SystemInfo.renderingThreadingMode
            );
            if (consistencyResult.m_Success)
            {
                GraphicsTestLogger.DebugLog(consistencyResult.m_Message);
            }
            else
            {
                GraphicsTestLogger.LogError(consistencyResult.m_Message);
                return GraphicsTestBuildResult.Failed;
            }

            // Print GPU information
            GraphicsTestLogger.Log($"Current build machine GPU information: {GraphicsDeviceInfo.PrintDeviceInfo()}");

            // Build graphics tests
            var platformInfos = new List<string>();
            foreach (var platform in Platforms)
                platformInfos.Add(platform.PrintPlatformInfo());
            GraphicsTestLogger.Log(
                $"Building graphics tests ({BuildManager.TestMode}) for the following platforms  --> {string.Join(string.Empty, platformInfos)}"
            );

            var hasTestCases = false;
            foreach (var tc in TestCases)
            {
                hasTestCases = true;
                break;
            }
            if (!hasTestCases)
            {
                GraphicsTestLogger.Log("Graphics test build finished: No test cases found to build.");
                return GraphicsTestBuildResult.Success;
            }

            Settings.OverwriteSettingsFromCommandLine();

            var testCasesToBuild = TestCaseFiltering.ApplyIgnoreAttributesForPlatform(Platforms, TestCases);

            var nonIgnoredNames = new List<string>();
            var nonIgnoredCount = 0;
            var graphicsTestCases = testCasesToBuild as GraphicsTestCase[] ?? ToArray(testCasesToBuild);
            foreach (var tc in graphicsTestCases)
            {
                if (tc.ShouldBeIgnored)
                    continue;
                nonIgnoredCount++;
                nonIgnoredNames.Add(tc.FullName);
            }
            GraphicsTestLogger.Log(
                LogType.Log,
                $"Building {nonIgnoredCount} test cases:\n{string.Join('\n', nonIgnoredNames)}"
            );

            PlayerSettings.WebGL.useEmbeddedResources = true;
            ImageHandler.instance.ImageResultsPath = Settings.ImageResultsPath;
            EditorPrefs.SetBool("AsynchronousShaderCompilation", false);

            var selectedTestScenes = new HashSet<string>();
            foreach (var tc in graphicsTestCases)
            {
                if (tc is SceneGraphicsTestCase sceneTc && !sceneTc.ShouldBeIgnored)
                    selectedTestScenes.Add(sceneTc.ScenePath);
            }
            var allGraphicsTestScenesSet = new HashSet<string>();
            foreach (var tc in TestCases)
            {
                if (tc is SceneGraphicsTestCase sceneTc)
                    allGraphicsTestScenesSet.Add(sceneTc.ScenePath);
            }
            var scenesInBuild = GenerateBuildSceneList(
                selectedTestScenes,
                allGraphicsTestScenesSet,
                EditorBuildSettings.scenes,
                Settings.ClearBuildSettingsScenesOnRebuild
            );

            var sceneGraphicsTestCases = new List<SceneGraphicsTestCase>();
            foreach (var tc in TestCases)
            {
                if (tc is SceneGraphicsTestCase sceneTc)
                    sceneGraphicsTestCases.Add(sceneTc);
            }
            var sceneListsEnumerable = GenerateSceneLists(sceneGraphicsTestCases);
            var sceneListsSet = new HashSet<SceneList>(new SceneListComparer());
            foreach (var sceneList in sceneListsEnumerable)
                sceneListsSet.Add(sceneList);
            var sceneLists = new List<SceneList>(sceneListsSet);
            sceneLists.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            WriteSettings(Settings, Platforms, sceneLists);

            var transitionScene = CreateTransitionScene();

            var scenesWithTransition = new EditorBuildSettingsScene[scenesInBuild.Length + 1];
            Array.Copy(scenesInBuild, scenesWithTransition, scenesInBuild.Length);
            scenesWithTransition[scenesInBuild.Length] = transitionScene;
            EditorBuildSettings.scenes = scenesWithTransition;

            try
            {
                var scenePaths = new string[EditorBuildSettings.scenes.Length];
                for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
                    scenePaths[i] = EditorBuildSettings.scenes[i].path;
                GraphicsTestLogger.Log(
                    $"The build will contain the following {EditorBuildSettings.scenes.Length} scenes:\n"
                        + string.Join("\n", scenePaths)
                );

                foreach (var action in GraphicsTestCaseCollector.Instance.GetAllSetupActions())
                {
                    GraphicsTestLogger.Log(LogType.Log, $"Running pre-build setup action: {action}");
                    action.Action.Invoke();
                }

                var nonIgnoredTestCases = new List<GraphicsTestCase>();
                foreach (var tc in graphicsTestCases)
                {
                    if (!tc.ShouldBeIgnored)
                        nonIgnoredTestCases.Add(tc);
                }
                var result = BuildManager.Build(Settings, Platforms, nonIgnoredTestCases);

                OnTestBuilderFinished.Invoke(this);
                return result;
            }
            catch
            {
                Settings.RestoreEditorBuildSettings();
                Settings.RestorePlayerGraphicsApis();
                throw;
            }
        }

        internal static IEnumerable<SceneList> GenerateSceneLists(
            IEnumerable<SceneGraphicsTestCase> sceneGraphicsTestCases
        )
        {
            var testCasesByMethod = new Dictionary<MethodIdentifier, List<SceneGraphicsTestCase>>();
            foreach (var t in sceneGraphicsTestCases)
            {
                var parameters = t.MethodInfo.GetParameters();
                var paramTypeNames = new string[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                    paramTypeNames[i] = parameters[i].ParameterType.FullName;
                var key = new MethodIdentifier(t.MethodInfo.TypeInfo.FullName, t.MethodInfo.Name, paramTypeNames);
                if (!testCasesByMethod.TryGetValue(key, out var list))
                {
                    list = new List<SceneGraphicsTestCase>();
                    testCasesByMethod[key] = list;
                }
                list.Add(t);
            }

            var sortedKeys = new List<MethodIdentifier>(testCasesByMethod.Keys);
            sortedKeys.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));

            foreach (var key in sortedKeys)
            {
                var sceneList = ScriptableObject.CreateInstance<SceneList>();
                sceneList.id = key;
                sceneList.name = key.methodName;
                var scenePathsSet = new HashSet<string>();
                foreach (var tc in testCasesByMethod[key])
                    scenePathsSet.Add(tc.ScenePath);
                var sortedScenePaths = new List<string>(scenePathsSet);
                sortedScenePaths.Sort(StringComparer.Ordinal);
                sceneList.scenePaths = sortedScenePaths;

                yield return sceneList;
            }
        }

        /// <summary>
        /// Computes the Build Settings scene list for the upcoming build.
        /// </summary>
        /// <remarks>
        /// Scenes already present keep their position (and GUID); selected scenes are forced enabled
        /// and any selected scene not yet present is appended in ordinal order. A graphics-test scene
        /// that is ignored in this run (present in <paramref name="allGraphicsTestScenes"/> but not in
        /// <paramref name="selectedTestScenes"/>) is removed only when
        /// <paramref name="clearBuildSettingsScenesOnRebuild"/> is <c>true</c>; scenes that are not part
        /// of any graphics test are never removed.
        /// </remarks>
        /// <param name="selectedTestScenes">Scene paths of the non-ignored scene test cases for this run.</param>
        /// <param name="allGraphicsTestScenes">Scene paths of all scene test cases in this run, ignored or not.</param>
        /// <param name="previousScenes">The current Build Settings scenes, whose order is preserved.</param>
        /// <param name="clearBuildSettingsScenesOnRebuild">Whether to remove ignored graphics-test scenes that are already present.</param>
        /// <returns>The scenes to write back to the Build Settings.</returns>
        internal static EditorBuildSettingsScene[] GenerateBuildSceneList(
            HashSet<string> selectedTestScenes,
            HashSet<string> allGraphicsTestScenes,
            EditorBuildSettingsScene[] previousScenes,
            bool clearBuildSettingsScenesOnRebuild
        )
        {
            if (selectedTestScenes == null)
            {
                throw new ArgumentNullException(nameof(selectedTestScenes));
            }

            if (allGraphicsTestScenes == null)
            {
                throw new ArgumentNullException(nameof(allGraphicsTestScenes));
            }

            if (previousScenes == null)
            {
                throw new ArgumentNullException(nameof(previousScenes));
            }

            var scenesToWrite = new List<EditorBuildSettingsScene>(previousScenes.Length + selectedTestScenes.Count);
            var keptScenePaths = new HashSet<string>(previousScenes.Length);

            foreach (var scene in previousScenes)
            {
                var scenePath = scene.path;
                if (selectedTestScenes.Contains(scenePath))
                {
                    // Selected scene: keep its position and GUID, just ensure it is enabled.
                    scene.enabled = true;
                    scenesToWrite.Add(scene);
                    keptScenePaths.Add(scenePath);
                }
                else if (clearBuildSettingsScenesOnRebuild && allGraphicsTestScenes.Contains(scenePath))
                {
                    // Drop scenes that are not part of the build (selected \ all)
                    GraphicsTestLogger.Log(
                        LogType.Log,
                        $"Removing scene {scenePath} from the Build Settings because all test cases using it are ignored."
                    );
                }
                else
                {
                    // A non-graphics-test scene, or an ignored scene we were told to keep: leave it
                    // untouched (position, enabled state and GUID).
                    scenesToWrite.Add(scene);
                    keptScenePaths.Add(scenePath);
                }
            }

            var sortedSelectedScenes = new List<string>(selectedTestScenes);
            sortedSelectedScenes.Sort(StringComparer.Ordinal);
            foreach (var scenePath in sortedSelectedScenes)
            {
                if (keptScenePaths.Add(scenePath))
                {
                    scenesToWrite.Add(new EditorBuildSettingsScene(scenePath, true));
                }
            }

            return scenesToWrite.ToArray();
        }

        internal static void WriteSettings(
            GraphicsTestBuildSettings settings,
            IList<GraphicsTestPlatform> testPlatforms,
            IEnumerable<SceneList> sceneListsToAdd
        )
        {
            settings ??= GraphicsTestBuildSettings.LoadOrDefault();

            var scenes = EditorBuildSettings.scenes;
            settings.PreviousScenesPaths = new string[scenes.Length];
            settings.PreviousScenesEnabled = new bool[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                settings.PreviousScenesPaths[i] = scenes[i].path;
                settings.PreviousScenesEnabled[i] = scenes[i].enabled;
            }

            var schemaComparer = new PlatformSchema.SchemaEqualityComparer();
            var schemataSet = new HashSet<PlatformSchema>(schemaComparer);
            foreach (var p in testPlatforms)
                schemataSet.Add(p.Schema);
            var schemataList = new List<PlatformSchema>(schemataSet);
            schemataList.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));
            settings.BuildPlatformSchemata = schemataList.ToArray();

            var platformNames = new List<string>();
            foreach (var t in testPlatforms)
                platformNames.Add($"{t.Schema.name.ToLower().Replace(" ", "-")}-{t.Name}");
            settings.BuildPlatformNames = platformNames.ToArray();

            settings.ClearSceneLists();

            var listsToAdd = sceneListsToAdd as SceneList[] ?? ToArray(sceneListsToAdd);
            foreach (var sceneList in listsToAdd)
                settings.AddSceneList(sceneList);

            settings.PopulateScenePathsDictionaryFromSceneLists(listsToAdd);
            settings.ShouldCleanUpAfterBuild = true;
            settings.Save();
        }

        static EditorBuildSettingsScene CreateTransitionScene()
        {
            var currentScene = SceneManager.GetActiveScene();

            // Create a new Scene asset
            // If the current scene starts with InitTestScene it most likely means that we're in playmode tests
            // so we want to load the scene additively to avoid unloading the test framework's special scene
            // Otherwise, we load the scene in Single mode to avoid issues with 'unsaved' scenes. Overall a bit of
            // a scuffed setup but this is just the way things are I suppose.
            var transitionScene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                currentScene.name.StartsWith("InitTestScene") ? NewSceneMode.Additive : NewSceneMode.Single
            );

            const string saveDirectory = "Assets/GraphicsTestFramework/Temp";
            const string sceneName = "GraphicsTestTransitionScene.unity";

            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            EditorSceneManager.SaveScene(transitionScene, saveDirectory + "/" + sceneName);
            EditorSceneManager.CloseScene(transitionScene, true);

            // Add the new Scene to the Build Settings
            return new EditorBuildSettingsScene(saveDirectory + "/" + sceneName, true);
        }

        static T[] ToArray<T>(IEnumerable<T> source)
        {
            if (source is T[] array)
                return array;
            var list = new List<T>(source);
            return list.ToArray();
        }
    }
}
