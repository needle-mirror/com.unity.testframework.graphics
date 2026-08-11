using System.Collections.Generic;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Optional companion to <see cref="IPlayerContentBuilder"/> for content builders that can report
    /// which platform each built bundle was resolved for. The build manager persists this as per-bundle
    /// platform metadata so the runtime loader can prefer the bundles matching the machine it runs on.
    /// </summary>
    interface IPerPlatformBundleSource
    {
        /// <summary>
        /// The bundles produced by the most recent <see cref="IPlayerContentBuilder.BuildContent"/>
        /// call, paired with the platform each bundle's reference images were resolved for.
        /// </summary>
        IEnumerable<(string BundleName, GraphicsTestPlatform Platform)> BuiltBundles { get; }
    }
}
