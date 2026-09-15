using System.Collections.Generic;
using System.IO;
using UnityEditor.TestTools.Graphics.Platforms;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Abstract class for managing the building of graphics tests.
    /// </summary>
    public abstract class GraphicsTestBuildManager
    {
        internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

        /// <summary>
        /// Build the graphics tests using this BuildManager.
        /// </summary>
        /// <param name="settings">
        /// The settings to use for the build.
        /// </param>
        /// <param name="platforms">
        /// The nodes to build for.
        /// </param>
        /// <param name="graphicsTestCases">
        /// The graphics test cases to build.
        /// </param>
        /// <returns>
        /// The result of the build.
        /// </returns>
        public abstract GraphicsTestBuildResult Build(
            GraphicsTestBuildSettings settings,
            IEnumerable<GraphicsTestPlatform> platforms,
            IList<GraphicsTestCase> graphicsTestCases
        );

        /// <summary>
        /// Clean up the build for the graphics tests.
        /// </summary>
        /// <param name="settings">
        /// The settings to use for cleaning up the graphics tests.
        /// </param>
        /// <remarks>
        /// This method is called after the build is complete to clean up any temporary files or directories.
        /// </remarks>
        public void CleanUp(GraphicsTestBuildSettings settings)
        {
            if (settings == null)
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    "GraphicsTestBuildSettings is null. Cannot clean up after build."
                );
                return;
            }

            GraphicsTestLogger.Log("Cleaning up Graphics Tests.");
            settings.ShouldCleanUpAfterBuild = false;
            settings.RestoreEditorBuildSettings();
            settings.Save();
            AssetService.Refresh();

            DeleteDirectory("Assets/GraphicsTestFramework/Temp", deleteIfNotEmpty: true);
            DeleteDirectory("Assets/GraphicsTestFramework");
            DeleteDirectory("Assets/Resources");
        }

        void DeleteDirectory(string path, bool deleteIfNotEmpty = false)
        {
            var isEmpty = true;
            if (Directory.Exists(path))
            {
                foreach (var _ in Directory.EnumerateFileSystemEntries(path))
                {
                    isEmpty = false;
                    break;
                }
                if (isEmpty || deleteIfNotEmpty)
                    AssetService.DeleteAsset(path);
            }
        }

        /// <summary>
        /// The TestMode for the graphics test build.
        /// </summary>
        public abstract TestMode TestMode { get; protected set; }

        /// <summary>
        /// Creates a new instance of the graphics test build manager based on the specified context.
        /// </summary>
        /// <param name="testMode">The context to use for creating the build manager.</param>
        /// <param name="platform">The platform to build for.</param>
        /// <returns>A new instance of the graphics test build manager for the specified context.</returns>
        /// <exception cref="System.ArgumentException">If the context is unknown.</exception>
        public static GraphicsTestBuildManager FromContext(TestMode testMode, RuntimePlatform platform)
        {
            return testMode switch
            {
                TestMode.Player => new PlayerGraphicsTestBuildManager(
                    new CompositeContentBuilder(PlayerContentBuilders.All),
                    ResolveBuildTarget(platform, EditorUserBuildSettings.activeBuildTarget)
                ),
                TestMode.EditMode or TestMode.PlayMode => new EditorGraphicsTestBuildManager(testMode),
                _ => throw new System.ArgumentException("Unknown Test Mode: " + testMode),
            };
        }

        /// <summary>
        /// Picks the BuildTarget to build test content for.
        /// </summary>
        /// <param name="platform">The platform the test run targets.</param>
        /// <param name="activeBuildTarget">The BuildTarget the Editor is currently set to.</param>
        /// <returns>
        /// <paramref name="activeBuildTarget"/> when it belongs to <paramref name="platform"/>, so that two
        /// BuildTargets sharing a RuntimePlatform stay distinct, otherwise the default BuildTarget of
        /// <paramref name="platform"/>.
        /// </returns>
          /// <remarks>
          /// When both are candidates the active target wins, because RuntimePlatform cannot express the
          /// distinction the caller asked for: it collapses StandaloneWindows/StandaloneWindows64 and
          /// StandaloneLinux64/LinuxHeadlessSimulation. A run whose -buildTarget and -testPlatform disagree
          /// therefore builds content for -buildTarget. UTR derives both from one --platform flag, and the
          /// Test Runner window passes the active target as the target platform, so neither can produce that
          /// disagreement.
          /// </remarks>
        internal static BuildTarget ResolveBuildTarget(RuntimePlatform platform, BuildTarget activeBuildTarget)
        {
            if (activeBuildTarget.ToRuntimePlatformOrNull() == platform)
                return activeBuildTarget;

            return platform.ToBuildTarget();
        }
    }
}
