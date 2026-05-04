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
            {
                GraphicsTestLogger.Log(LogType.Warning, "No content bundles were built.");
            }
            else
            {
                GraphicsTestLogger.Log(
                    LogType.Log,
                    "Test content bundles were built successfully:\n" + string.Join("\n", bundleNames)
                );
                settings.TestContentBundlePaths = bundleNames;
                settings.Save();
            }

            return GraphicsTestBuildResult.Success;
        }
    }
}
