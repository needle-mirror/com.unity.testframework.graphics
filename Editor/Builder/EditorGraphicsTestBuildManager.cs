using System;
using System.Collections.Generic;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    class EditorGraphicsTestBuildManager : GraphicsTestBuildManager
    {
        public sealed override TestMode TestMode { get; protected set; }

        internal EditorGraphicsTestBuildManager(TestMode testMode)
        {
            TestMode = testMode;
        }

        public override GraphicsTestBuildResult Build(
            GraphicsTestBuildSettings settings,
            IEnumerable<GraphicsTestPlatform> platforms,
            IList<GraphicsTestCase> graphicsTestCases
        )
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (platforms == null)
                throw new ArgumentNullException(nameof(platforms));

            if (graphicsTestCases == null)
                throw new ArgumentNullException(nameof(graphicsTestCases));

            foreach (var tc in graphicsTestCases)
            {
                if (tc == null)
                    throw new ArgumentNullException(
                        nameof(graphicsTestCases),
                        "Graphics test cases must not contain null elements."
                    );
            }

            foreach (var platform in platforms)
            {
                var images = ReferenceImageUtility.Default.CollectReferenceImagePathsFor(graphicsTestCases, platform);

                var imageLines = new List<string>();
                foreach (var pair in images)
                    imageLines.Add($"{pair.Key} => {pair.Value}");
                GraphicsTestLogger.Log(
                    $"Found {images.Count} reference images for platform {platform}:\n" + string.Join("\n", imageLines)
                );

                ReferenceImageUtility.Default.SetupReferenceImageImportSettings(images.Values);
            }

            TestContentLoader.Reset();

            return GraphicsTestBuildResult.Success;
        }
    }
}
