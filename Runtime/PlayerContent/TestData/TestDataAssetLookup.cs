using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Lookup for test data bundles: assets are addressed as-given, so a full asset path
    /// disambiguates assets sharing a file name; a file name additionally retries as a stem, for
    /// bundles that addressed the asset without its extension. (Reference image bundles instead
    /// normalize every key to a stem.)
    /// </summary>
    static class TestDataAssetLookup
    {
        public static T LoadAsset<T>(AssetBundle bundle, string assetName)
            where T : Object
        {
            if (bundle == null)
                return null;

            var asset = bundle.LoadAsset<T>(assetName);
            if (asset != null)
                return asset;

            if (!ShouldRetryAsStem(assetName, out var stem))
                return null;

            WarnOnAmbiguousStem(bundle, stem);
            return bundle.LoadAsset<T>(stem);
        }

        public static bool Contains(AssetBundle bundle, string assetName)
        {
            if (bundle == null)
                return false;

            if (bundle.Contains(assetName))
                return true;

            return ShouldRetryAsStem(assetName, out var stem) && bundle.Contains(stem);
        }

        /// <summary>
        /// Whether a missed lookup should be retried as a file stem. A path is never retried: a
        /// caller passing a full asset path is disambiguating, so resolving it to a same-named
        /// asset from another folder would answer a different question than the one asked.
        /// </summary>
        static bool ShouldRetryAsStem(string assetName, out string stem)
        {
            stem = System.IO.Path.GetFileNameWithoutExtension(assetName);

            if (assetName.IndexOf('/') >= 0 || assetName.IndexOf('\\') >= 0)
                return false;

            return !string.Equals(stem, assetName, StringComparison.OrdinalIgnoreCase);
        }

        static void WarnOnAmbiguousStem(AssetBundle bundle, string stem)
        {
            var matches = new List<string>();
            foreach (var name in bundle.GetAllAssetNames())
            {
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(name), stem, StringComparison.OrdinalIgnoreCase))
                    matches.Add(name);
            }

            if (matches.Count > 1)
            {
                GraphicsTestLogger.LogWarning(
                    $"Test data lookup '{stem}' matches {matches.Count} assets in bundle '{bundle.name}'; "
                        + $"loading the first. Use the full asset path to disambiguate:\n\t{string.Join("\n\t", matches)}"
                );
            }
        }
    }
}
