using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Describes a set of test data assets a test declares (typically through
    /// <see cref="RequireTestDataAttribute"/>): packed into a content bundle for player builds,
    /// loaded through <see cref="GraphicsTestData"/>.
    /// </summary>
    public interface ITestDataDescriptor
    {
        /// <summary>
        /// The logical bundle name: identical at build and run time, and descriptors sharing it
        /// merge into one bundle.
        /// </summary>
        string BundleName { get; }

        /// <summary>
        /// The declared asset paths or wildcard patterns, e.g. "Assets/Scenes/500_SSAO/*.exr".
        /// </summary>
        IEnumerable<string> AssetPatterns { get; }

        /// <summary>
        /// Expands <see cref="AssetPatterns"/> to concrete project-relative asset paths. This runs
        /// where the project files exist (the Editor and the build machine); players never call it.
        /// </summary>
        /// <returns>The project-relative paths of every declared asset.</returns>
        IEnumerable<string> GetAssetPaths();

        /// <summary>
        /// Maps a source asset path to the name used to address it in the bundle. The default
        /// implementation returns the asset path unchanged, which preserves AssetBundle's native
        /// addressing: assets load by full path or by file name. Override to define custom keys.
        /// </summary>
        /// <param name="assetPath">The project-relative path of a declared asset.</param>
        /// <returns>The addressable name for the asset.</returns>
        string GetAddressableName(string assetPath);
    }
}
