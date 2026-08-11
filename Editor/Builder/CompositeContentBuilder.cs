using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Runs an ordered list of <see cref="IPlayerContentBuilder"/>s as one, concatenating the bundle
    /// names they return and aggregating the metadata of builders that implement the companion
    /// interfaces (<see cref="IPerPlatformBundleSource"/>, <see cref="ITestDataBundleSource"/>).
    /// The list is read when the build starts, so a builder registered during prebuild setup is
    /// included in that build.
    /// </summary>
    sealed class CompositeContentBuilder : IPlayerContentBuilder, IPerPlatformBundleSource, ITestDataBundleSource
    {
        readonly IReadOnlyList<IPlayerContentBuilder> m_Builders;

        // The builders the most recent build actually ran, so its metadata and cleanup describe
        // that build even if the registry changed while it was running.
        List<IPlayerContentBuilder> m_BuildersThatRan;

        IReadOnlyList<IPlayerContentBuilder> CurrentBuilders => m_BuildersThatRan ?? m_Builders;

        /// <summary>
        /// The list is read at build time, not copied, so builders registered after construction
        /// (for example by a prebuild setup action) still run.
        /// </summary>
        public CompositeContentBuilder(IReadOnlyList<IPlayerContentBuilder> builders)
        {
            m_Builders = builders ?? throw new ArgumentNullException(nameof(builders));
        }

        public IEnumerable<string> BuildContent(
            IList<GraphicsTestCase> testCases,
            IEnumerable<GraphicsTestPlatform> platforms,
            BuildTarget buildTarget
        )
        {
            var platformList = new List<GraphicsTestPlatform>(platforms);
            // Snapshot per build: the list is the live registry, and a builder may register another.
            m_BuildersThatRan = new List<IPlayerContentBuilder>(m_Builders);
            foreach (var builder in m_BuildersThatRan)
            {
                foreach (var bundleName in builder.BuildContent(testCases, platformList, buildTarget))
                    yield return bundleName;
            }
        }

        public void CleanUp()
        {
            foreach (var builder in CurrentBuilders)
                builder.CleanUp();
        }

        public IEnumerable<(string BundleName, GraphicsTestPlatform Platform)> BuiltBundles
        {
            get
            {
                foreach (var builder in CurrentBuilders)
                {
                    if (builder is not IPerPlatformBundleSource source)
                        continue;

                    foreach (var bundle in source.BuiltBundles)
                        yield return bundle;
                }
            }
        }

        public IEnumerable<(string BundleFileName, string LogicalName)> BuiltTestDataBundles
        {
            get
            {
                foreach (var builder in CurrentBuilders)
                {
                    if (builder is not ITestDataBundleSource source)
                        continue;

                    foreach (var bundle in source.BuiltTestDataBundles)
                        yield return bundle;
                }
            }
        }
    }
}
