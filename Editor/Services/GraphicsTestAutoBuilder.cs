using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

[assembly: PrebuildSetupWithTestData(typeof(UnityEditor.TestTools.Graphics.Builder.GraphicsTestAutoBuilder))]
[assembly: PostBuildCleanupWithTestData(typeof(UnityEditor.TestTools.Graphics.Builder.GraphicsTestAutoBuilder))]

namespace UnityEditor.TestTools.Graphics.Builder
{
    class GraphicsTestAutoBuilder
        : IPrebuildSetupWithTestData,
            IPostbuildCleanupWithTestData,
            IPostprocessBuildWithReport
    {
        public void Setup(TestData testData)
        {
            GraphicsTestLogger.DebugLog($"Received test data from UTF:\n{testData}");
            var settings = GraphicsTestBuildSettings.LoadOrDefault();
            if (!settings.AutoBuildTestCases)
                return;

            var currentPlatform = testData.TestMode.HasFlag(TestMode.Player)
                ? GraphicsTestPlatform.PlayerBuild
                : GraphicsTestPlatform.Current;

            var fixtureArgSets = new List<Enum[]>();
            foreach (var t in testData.TestList)
            {
                var args = new List<Enum>(FindFixtureArguments(t));
                fixtureArgSets.Add(args.ToArray());
            }

            var combinations = new PlatformCombinationCliReader().ReadCombinations();

            // A combined player must build exactly the graphics APIs it carries, so drive the player's
            // API list from the combined set instead of relying on the project's PlayerSettings. The
            // previous configuration is restored on cleanup.
            if (testData.TestMode.HasFlag(TestMode.Player))
                ApplyCombinedGraphicsApis(settings, combinations);

            var platforms = GraphicsTestPlatformResolver.ResolvePlatforms(
                settings.PlatformSchemata, currentPlatform, fixtureArgSets, combinations);

            var builder = new GraphicsTestBuilder
            {
                Settings = settings,
                BuildManager = GraphicsTestBuildManager.FromContext(testData.TestMode, testData.TestPlatform),
                Platforms = platforms,
                TestCases = GraphicsTestCaseCollector.Instance.GetAllTestCasesFromTestList(testData.TestList),
            };

            if (settings.ShaderWarningsAsErrors)
                ShaderWarningCollector.StartCollecting();

            builder.Build();
        }

        static void ApplyCombinedGraphicsApis(GraphicsTestBuildSettings settings, PlatformCombinations combinations)
        {
            var combined = combinations.ValuesFor(typeof(UnityEngine.Rendering.GraphicsDeviceType));
            if (combined.Count == 0)
                return;

            var apis = new UnityEngine.Rendering.GraphicsDeviceType[combined.Count];
            for (var i = 0; i < combined.Count; i++)
                apis[i] = (UnityEngine.Rendering.GraphicsDeviceType)combined[i];

            settings.SavePlayerGraphicsApis();
            var target = EditorUserBuildSettings.activeBuildTarget;
            PlayerSettings.SetUseDefaultGraphicsAPIs(target, false);
            PlayerSettings.SetGraphicsAPIs(target, apis);
        }

        static IEnumerable<Enum> FindFixtureArguments(ITest start)
        {
            if (start is TestFixture fixture)
            {
                try
                {
                    var args = fixture.Arguments ?? Array.Empty<object>();
                    var result = new List<Enum>();
                    foreach (var arg in args)
                    {
                        if (arg is Enum e && GraphicsTestPlatform.Current.Schema.Types.Contains(e.GetType()))
                            result.Add(e);
                    }
                    return result;
                }
                catch (Exception e)
                {
                    GraphicsTestLogger.DebugLog(e.Message);
                }
            }

            return start.Parent == null ? Array.Empty<Enum>() : FindFixtureArguments(start.Parent);
        }

        public void Cleanup(TestData testData)
        {
            GraphicsTestLogger.DebugLog($"Received test data from UTF:\n{testData}");
            var settings = GraphicsTestBuildSettings.LoadOrDefault();

            GraphicsTestBuilder builder = new()
            {
                BuildManager = GraphicsTestBuildManager.FromContext(testData.TestMode, testData.TestPlatform),
            };

            if (settings.AutoBuildTestCases && settings.ShouldCleanUpAfterBuild && !BuildPipeline.isBuildingPlayer)
            {
                builder.BuildManager.CleanUp(settings);
            }

            if (settings.ShaderWarningsAsErrors && ShaderWarningCollector.HasWarnings)
            {
                ShaderWarningCollector.StopAndReport();
                throw new BuildFailedException(
                    $"Build failed: {ShaderWarningCollector.CollectedWarnings.Count} shader warning(s) detected. "
                    + $"See {Path.GetFullPath(ShaderWarningCollector.OutputFilePath)}"
                );
            }
        }

        public int callbackOrder => 99;

        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = GraphicsTestBuildSettings.LoadOrDefault();
            GraphicsTestBuilder builder = new()
            {
                BuildManager = new PlayerGraphicsTestBuildManager(null, report.summary.platform),
            };

            if (!settings.AutoBuildTestCases || !settings.ShouldCleanUpAfterBuild)
                return;

            var includedModulesCount = 0;
            if (report.strippingInfo?.includedModules != null)
            {
                foreach (var _ in report.strippingInfo.includedModules)
                    includedModulesCount++;
            }
            GraphicsTestLogger.Log(
                LogType.Log,
                $"Graphics Test Build finished for target {report.summary.platform} at path {report.summary.outputPath}.\n "
                    + $"\tBuild size: {report.summary.totalSize / (1024.0 * 1024.0):F2} MB\n" // bytes to MB
                    + $"\tBuild time: {report.summary.totalTime}\n"
                    + $"\tBuilt {report.scenesUsingAssets?.Length ?? 0} scenes containing assets, "
                    + $"{report.packedAssets?.Length ?? 0} packed assets, "
                    + $"{includedModulesCount} included modules "
                    + $"in {report.steps?.Length ?? 0} steps.\n"
                    + $"\tBuild produced {report.summary.totalWarnings} warnings and {report.summary.totalErrors} errors: "
                    + $"\t\t{report.SummarizeErrors()}"
            );
            builder.BuildManager.CleanUp(settings);

            if (settings.ShaderWarningsAsErrors && ShaderWarningCollector.HasWarnings)
            {
                ShaderWarningCollector.StopAndReport();
                throw new BuildFailedException(
                    $"Build failed: {ShaderWarningCollector.CollectedWarnings.Count} shader warning(s) detected. "
                    + $"See {Path.GetFullPath(ShaderWarningCollector.OutputFilePath)}"
                );
            }
        }
    }
}
