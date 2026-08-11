using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    class PlayerGraphicsTestBuildManager : GraphicsTestBuildManager
    {
        readonly IPlayerContentBuilder m_ContentBuilder;
        readonly BuildTarget m_BuildTarget;

        public override TestMode TestMode { get; protected set; } = TestMode.Player;

        internal PlayerGraphicsTestBuildManager(IPlayerContentBuilder contentBuilder, BuildTarget buildTarget)
        {
            m_ContentBuilder = contentBuilder;
            m_BuildTarget = buildTarget;
        }

        public override GraphicsTestBuildResult Build(
            GraphicsTestBuildSettings settings,
            IEnumerable<GraphicsTestPlatform> platforms,
            IList<GraphicsTestCase> graphicsTestCases
        )
        {
            if (!settings)
                throw new ArgumentNullException(nameof(settings));

            if (platforms == null)
                throw new ArgumentNullException(nameof(platforms));

            if (m_ContentBuilder == null)
                throw new ArgumentNullException(nameof(m_ContentBuilder));

            if (graphicsTestCases == null)
                throw new ArgumentNullException(nameof(graphicsTestCases));

            foreach (var tc in graphicsTestCases)
            {
                if (tc == null)
                    throw new ArgumentNullException(nameof(graphicsTestCases));
            }

            var bundleNamesList = new List<string>();
            foreach (var name in m_ContentBuilder.BuildContent(graphicsTestCases, platforms, m_BuildTarget))
                bundleNamesList.Add(name);
            var bundleNames = bundleNamesList.ToArray();

            if (bundleNames.Length == 0)
                GraphicsTestLogger.Log(LogType.Warning, "No content bundles were built.");
            else
                GraphicsTestLogger.Log(
                    LogType.Log,
                    "Test content bundles were built successfully:\n" + string.Join("\n", bundleNames)
                );

            // Always written, so a build that produces nothing does not leave the previous build's
            // bundles for the player to look for.
            settings.TestContentBundlePaths = bundleNames;
            settings.TestContentBundlePlatforms = CollectBundlePlatformInfos(m_ContentBuilder);
            settings.TestDataBundles = CollectTestDataBundleInfos(m_ContentBuilder);
            settings.Save();

            return GraphicsTestBuildResult.Success;
        }

        /// <summary>
        /// Collects per-bundle platform metadata when the content builder can provide it. Builders
        /// that can't report platforms clear the metadata, so stale entries from a previous build
        /// never rank bundles they don't describe.
        /// </summary>
        internal static TestContentBundlePlatformInfo[] CollectBundlePlatformInfos(IPlayerContentBuilder contentBuilder)
        {
            if (contentBuilder is not IPerPlatformBundleSource withPlatforms)
                return Array.Empty<TestContentBundlePlatformInfo>();

            var infos = new List<TestContentBundlePlatformInfo>();
            foreach (var (bundleName, platform) in withPlatforms.BuiltBundles)
                infos.Add(TestContentBundlePlatformInfo.From(bundleName, platform));
            return infos.ToArray();
        }

        /// <summary>
        /// Collects the test data bundle metadata when the content builder can provide it. Builders
        /// that can't report test data bundles clear the metadata, so stale entries from a previous
        /// build never divert bundles they don't describe out of the global asset search.
        /// </summary>
        internal static TestDataBundleInfo[] CollectTestDataBundleInfos(IPlayerContentBuilder contentBuilder)
        {
            if (contentBuilder is not ITestDataBundleSource withTestData)
                return Array.Empty<TestDataBundleInfo>();

            var infos = new List<TestDataBundleInfo>();
            foreach (var (bundleFileName, logicalName) in withTestData.BuiltTestDataBundles)
            {
                infos.Add(new TestDataBundleInfo { bundleFileName = bundleFileName, logicalName = logicalName });
            }
            return infos.ToArray();
        }
    }
}
