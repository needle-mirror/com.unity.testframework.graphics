using System;
using System.Collections.Generic;
using System.IO;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Default <see cref="ITestDataDescriptor"/> implementation. Expands wildcard patterns of the
    /// form "Directory/*.ext" (top directory only) to concrete asset paths and addresses assets by
    /// their unchanged asset path. Subclass and override <see cref="GetAssetPaths"/> to collect
    /// assets programmatically, or <see cref="GetAddressableName"/> to define custom keys.
    /// </summary>
    public class TestDataDescriptor : ITestDataDescriptor
    {
        readonly string m_BundleName;
        readonly string[] m_AssetPatterns;

        /// <summary>
        /// Creates a descriptor for a set of test data assets.
        /// </summary>
        /// <param name="bundleName">The logical bundle name; descriptors sharing a name are merged.</param>
        /// <param name="assetPatterns">Asset paths or wildcard patterns, e.g. "Assets/Scenes/500_SSAO/*.exr".</param>
        public TestDataDescriptor(string bundleName, IEnumerable<string> assetPatterns)
        {
            if (string.IsNullOrEmpty(bundleName))
                throw new ArgumentException("Bundle name must not be null or empty.", nameof(bundleName));

            m_BundleName = bundleName;
            m_AssetPatterns = assetPatterns != null
                ? new List<string>(assetPatterns).ToArray()
                : Array.Empty<string>();
        }

        /// <inheritdoc/>
        public string BundleName => m_BundleName;

        /// <inheritdoc/>
        public IEnumerable<string> AssetPatterns => m_AssetPatterns;

        /// <inheritdoc/>
        /// <remarks>
        /// Patterns containing '*' are expanded with <see cref="Directory.GetFiles(string, string, SearchOption)"/>
        /// over the pattern's directory (top directory only), skipping .meta files. Patterns without
        /// wildcards are returned when the file exists. Declarations that resolve to nothing are not
        /// reported here; the build fails on them so broken markup surfaces at build time.
        /// </remarks>
        public virtual IEnumerable<string> GetAssetPaths()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var pattern in m_AssetPatterns)
            {
                if (string.IsNullOrEmpty(pattern))
                    continue;

                if (pattern.Contains('*'))
                {
                    var directory = Path.GetDirectoryName(pattern);
                    var searchPattern = Path.GetFileName(pattern);

                    if (!Directory.Exists(directory))
                        continue;

                    foreach (var file in Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly))
                    {
                        var assetPath = file.Replace(Path.DirectorySeparatorChar, '/');
                        if (!assetPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                            result.Add(assetPath);
                    }
                }
                else if (File.Exists(pattern))
                {
                    result.Add(pattern.Replace(Path.DirectorySeparatorChar, '/'));
                }
            }

            // Sorted: the build's and the Editor's "keep the first" rules read this from separate
            // calls and must agree.
            var ordered = new List<string>(result);
            ordered.Sort(StringComparer.Ordinal);
            return ordered;
        }

        /// <inheritdoc/>
        public virtual string GetAddressableName(string assetPath) => assetPath;
    }
}
