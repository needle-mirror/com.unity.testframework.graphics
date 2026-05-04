using System.Collections.Generic;
using UnityEngine.SceneManagement;
using static UnityEngine.Application;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Diagnostics;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute to bake lighting for the specified scenes before running graphics tests.
    /// This attribute should be used on a test class or test method.
    /// </summary>
    /// <remarks>
    /// This attribute will bake lighting for the specified scenes as a pre-build step.
    /// The scenes will be opened one by one, and the lighting will be baked for each scene.
    /// If the lighting bake fails for any scene, an error will be logged.
    /// </remarks>
    public class BakeLightingAttribute : GraphicsPrebuildSetupAttribute
    {
        string[] ScenePaths { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="BakeLightingAttribute"/> class.
        /// </summary>
        /// <param name="scenePaths">The paths of the scenes to bake lighting for.</param>
        public BakeLightingAttribute(params string[] scenePaths)
            : base(0)
        {
            ScenePaths = scenePaths;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="BakeLightingAttribute"/> class.
        /// </summary>
        /// <param name="order">The order in which to run the pre-build step.</param>
        /// <param name="scenePaths">The paths of the scenes to bake lighting for.</param>
        /// <remarks>
        /// The order is used to determine the order in which the pre-build steps are run.
        /// Lower numbers are run first.
        /// </remarks>
        public BakeLightingAttribute(int order, params string[] scenePaths)
            : base(order)
        {
            ScenePaths = scenePaths;
        }

        /// <inheritdoc cref="GraphicsPrebuildSetupAttribute"/>
        protected override void Setup()
        {
            var filteredPaths = FilterScenePaths(ScenePaths, GraphicsTestBuildSettings.LoadOrDefault().ScenePathsDictionary);
            var result = BakeLightingForScenes(filteredPaths);

            if (!result.BakeSucceeded)
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    $"Failed to bake lighting for scenes: {string.Join(", ", result.FailedScenes)}"
                );
            }
        }

        static string[] FilterScenePaths(string[] scenePaths, Dictionary<MethodIdentifier, List<string>> scenePathsDictionary)
        {
            var filtered = new List<string>();
            foreach (var p in scenePaths)
            {
                foreach (var s in scenePathsDictionary.Values)
                {
                    if (s.Contains(p))
                    {
                        filtered.Add(p);
                        break;
                    }
                }
            }
            return filtered.ToArray();
        }

        static BakeResult BakeLightingForScenes(string[] scenesPaths)
        {
#if !UNITY_EDITOR
            return new BakeResult(false, new List<string>(), new List<string>());
#else
            var sceneOpenAtStartPath = SceneManager.GetActiveScene().path;
            var bakeSuccess = true;
            var failedScenes = new List<string>();
            var successfulScenes = new List<string>();
            var cumulativeBakeTime = 0;

            for (var i = 0; i < scenesPaths.Length; i++)
            {
                var thisSceneBakeSuccess = true;
                var scenePath = scenesPaths[i];
                var prefix = $"[{i + 1}/{scenesPaths.Length}]";

                var sceneToBake = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                LogCallback logCallback = delegate(string message, string stackTrace, LogType logType)
                {
                    GraphicsTestLogger.DebugLog(message);
                    if (logType == LogType.Error || logType == LogType.Exception)
                    {
                        thisSceneBakeSuccess = false;
                        bakeSuccess = false;
                    }
                };

                GraphicsTestLogger.Log(LogType.Log, $"{prefix} Baking lighting for scene {sceneToBake.name}...");

                var sw = new Stopwatch();
                logMessageReceived += logCallback;
                try
                {
                    sw.Start();
                    Lightmapping.Bake();
                    sw.Stop();
                }
                finally
                {
                    logMessageReceived -= logCallback;
                    if (sw.IsRunning)
                        sw.Stop();
                }

                var bakeTime = (int)sw.ElapsedMilliseconds / 1000;
                cumulativeBakeTime += bakeTime;

                if (thisSceneBakeSuccess)
                {
                    successfulScenes.Add(scenePath);
                    EditorSceneManager.SaveScene(sceneToBake);
                    GraphicsTestLogger.Log(
                        LogType.Log,
                        $"{prefix} Baking lighting for scene {sceneToBake.name} succeeded after {bakeTime} seconds."
                    );
                }
                else
                {
                    failedScenes.Add(scenePath);
                    GraphicsTestLogger.Log(
                        LogType.Error,
                        $"{prefix} Baking lighting for scene {sceneToBake.name} failed after {bakeTime} seconds."
                    );
                }
            }

            GraphicsTestLogger.Log(
                LogType.Log,
                $"Finished baking lighting for {scenesPaths.Length} scenes in {cumulativeBakeTime} seconds."
            );

            if (!string.IsNullOrEmpty(sceneOpenAtStartPath))
            {
                EditorSceneManager.OpenScene(sceneOpenAtStartPath, OpenSceneMode.Single);
            }

            return new BakeResult(bakeSuccess, failedScenes, successfulScenes);
#endif
        }
    }

    class BakeResult
    {
        public bool BakeSucceeded { get; init; }
        public List<string> FailedScenes { get; init; }
        public List<string> SuccessfulScenes { get; init; }

        public BakeResult(bool success, List<string> failedScenes, List<string> successfulScenes)
        {
            BakeSucceeded = success;
            FailedScenes = failedScenes;
            SuccessfulScenes = successfulScenes;
        }
    }
}
