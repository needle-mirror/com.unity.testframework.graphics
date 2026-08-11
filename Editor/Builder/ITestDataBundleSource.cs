using System.Collections.Generic;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Optional companion to <see cref="IPlayerContentBuilder"/> for builders that produce test
    /// data bundles, persisted as <c>GraphicsTestBuildSettings.TestDataBundles</c> so the loader
    /// keeps them out of the global search and resolves them by logical name.
    /// </summary>
    interface ITestDataBundleSource
    {
        /// <summary>
        /// The bundles of the most recent build: bundle file name plus declared logical name.
        /// </summary>
        IEnumerable<(string BundleFileName, string LogicalName)> BuiltTestDataBundles { get; }
    }
}
