using System;
using System.Collections.Generic;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Loads the test data assets declared for one test case (see
    /// <see cref="RequireTestDataAttribute"/>): from the AssetDatabase in the Editor, from content
    /// bundles in players. Access it through <see cref="GraphicsTestCase.TestData"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Load{T}"/> and <see cref="GetAssetPath"/> throw
    /// <see cref="TestDataNotFoundException"/> on a miss; <see cref="TryLoad{T}"/> covers optional
    /// assets. In players, wait for <see cref="TestContentLoader.WaitForContentLoadAsync"/> in a
    /// UnitySetUp method first.
    /// </remarks>
    public sealed class GraphicsTestData
    {
        static readonly IReadOnlyList<ITestDataDescriptor> k_NoDescriptors = Array.Empty<ITestDataDescriptor>();

        static readonly Func<string, IEnumerable<TestContentBundle>> k_DefaultBundleSource = logicalName =>
            TestContentLoader.ContentLoader.GetBundlesFor(logicalName);

        /// <summary>
        /// Resolves a logical bundle name to content bundles for the player-side lookups. Tests
        /// point this at their own <see cref="TestContentLoader"/>; production uses the singleton.
        /// </summary>
        internal static Func<string, IEnumerable<TestContentBundle>> BundleSource { get; set; } =
            k_DefaultBundleSource;

        internal static void ResetBundleSource() => BundleSource = k_DefaultBundleSource;

#if UNITY_EDITOR
        /// <summary>
        /// Test hook: routes lookups through the player-side bundle path instead of the
        /// AssetDatabase. Player builds compile the bundle path unconditionally.
        /// </summary>
        internal static bool ForcePlayerBundles { get; set; }
#endif

        /// <summary>
        /// An empty instance, fresh per access: <see cref="LoadMessage"/> is per-lookup state, so
        /// a shared instance would leak diagnostics between callers.
        /// </summary>
        public static GraphicsTestData Empty => new(k_NoDescriptors);

        readonly IReadOnlyList<ITestDataDescriptor> m_Descriptors;

        internal GraphicsTestData(IReadOnlyList<ITestDataDescriptor> descriptors)
        {
            m_Descriptors = descriptors ?? k_NoDescriptors;
        }

        /// <summary>
        /// The descriptors of the declared test data sets.
        /// </summary>
        public IReadOnlyList<ITestDataDescriptor> Descriptors => m_Descriptors;

        /// <summary>
        /// Whether any test data was declared for the test case.
        /// </summary>
        public bool HasData => m_Descriptors.Count > 0;

        /// <summary>
        /// Diagnostics from the most recent lookup.
        /// </summary>
        public string LoadMessage { get; private set; } = string.Empty;

        /// <summary>
        /// Whether every declared test data set has a backing store: in the Editor, each descriptor
        /// resolves to at least one asset; in players, each descriptor has a loaded content bundle.
        /// </summary>
        public bool IsAvailable
        {
            get
            {
#if UNITY_EDITOR
                if (!ForcePlayerBundles)
                {
                    foreach (var descriptor in m_Descriptors)
                    {
                        if (descriptor == null)
                            continue;

                        var any = false;
                        foreach (var _ in descriptor.GetAssetPaths())
                        {
                            any = true;
                            break;
                        }

                        if (!any)
                            return false;
                    }

                    return true;
                }
#endif
                foreach (var descriptor in m_Descriptors)
                {
                    if (descriptor == null)
                        continue;

                    var loaded = false;
                    foreach (var bundle in BundleSource(descriptor.BundleName))
                    {
                        if (bundle.State == TestContentBundle.LoadState.Loaded)
                        {
                            loaded = true;
                            break;
                        }
                    }

                    if (!loaded)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// All addressable asset names in the declared test data sets.
        /// </summary>
        public IEnumerable<string> AssetNames
        {
            get
            {
#if UNITY_EDITOR
                if (!ForcePlayerBundles)
                {
                    foreach (var entry in EditorAssets)
                        yield return entry.AddressableName;
                    yield break;
                }
#endif
                foreach (var bundle in GetBundles())
                {
                    foreach (var name in bundle.GetAssetNames())
                        yield return name;
                }
            }
        }

        /// <summary>
        /// Loads a declared asset by full asset path, file name, or file stem; the full path
        /// disambiguates assets sharing a file name.
        /// </summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="assetName">The asset path, file name, or file stem.</param>
        /// <returns>The loaded asset; never null.</returns>
        /// <exception cref="TestDataNotFoundException">
        /// The asset is missing or its bundle failed to load; the message lists what was declared,
        /// built, and searched.
        /// </exception>
        public T Load<T>(string assetName)
            where T : Object
        {
            if (TryLoad<T>(assetName, out var asset))
                return asset;

            throw new TestDataNotFoundException(LoadMessage);
        }

        /// <summary>
        /// Loads a declared asset without failing on a miss; for optional assets.
        /// </summary>
        /// <typeparam name="T">The asset type.</typeparam>
        /// <param name="assetName">The asset path, file name, or file stem.</param>
        /// <param name="asset">The loaded asset, or null.</param>
        /// <returns>True when the asset was loaded; <see cref="LoadMessage"/> has the details either way.</returns>
        public bool TryLoad<T>(string assetName, out T asset)
            where T : Object
        {
#if UNITY_EDITOR
            if (!ForcePlayerBundles)
            {
                var entry = FindEditorAsset(assetName);
                if (entry.HasValue)
                {
                    asset = AssetDatabase.LoadAssetAtPath<T>(entry.Value.AssetPath);
                    if (asset != null)
                    {
                        LoadMessage = $"Loaded {assetName} from {entry.Value.AssetPath} via the AssetDatabase.";
                        return true;
                    }

                    LoadMessage =
                        $"Test data asset '{assetName}' resolved to {entry.Value.AssetPath} but could not be loaded as {typeof(T).Name}.";
                    return false;
                }

                asset = null;
                LoadMessage = BuildMissMessage(assetName);
                return false;
            }
#endif
            foreach (var bundle in GetBundles())
            {
                asset = bundle.LoadAsset<T>(assetName);
                if (asset != null)
                {
                    LoadMessage = $"Loaded {assetName} from {bundle.GetType().Name} bundle {bundle.Name}.";
                    return true;
                }
            }

            asset = null;
            LoadMessage = BuildMissMessage(assetName);
            return false;
        }

        /// <summary>
        /// Checks whether a declared asset exists.
        /// </summary>
        /// <param name="assetName">The asset path, file name, or file stem.</param>
        /// <returns>True when the asset is present in the declared test data.</returns>
        public bool ContainsAsset(string assetName)
        {
#if UNITY_EDITOR
            if (!ForcePlayerBundles)
                return FindEditorAsset(assetName).HasValue;
#endif
            foreach (var bundle in GetBundles())
            {
                if (bundle.ContainsAsset(assetName))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// The asset's path: the project-relative source path in the Editor (usable for raw
        /// file reads), the informational bundle path in players.
        /// </summary>
        /// <param name="assetName">The asset path, file name, or file stem.</param>
        /// <returns>The asset path; never null.</returns>
        /// <exception cref="TestDataNotFoundException">The asset is not part of the declared test data.</exception>
        public string GetAssetPath(string assetName)
        {
#if UNITY_EDITOR
            if (!ForcePlayerBundles)
            {
                var entry = FindEditorAsset(assetName);
                if (entry.HasValue)
                    return entry.Value.AssetPath;

                LoadMessage = BuildMissMessage(assetName);
                throw new TestDataNotFoundException(LoadMessage);
            }
#endif
            foreach (var bundle in GetBundles())
            {
                if (bundle.ContainsAsset(assetName))
                    return bundle.AssetPath(assetName);
            }

            LoadMessage = BuildMissMessage(assetName);
            throw new TestDataNotFoundException(LoadMessage);
        }

        string BuildMissMessage(string assetName)
        {
            var sb = new StringBuilder();
            sb.Append($"Failed to find test data asset '{assetName}'.");

            if (!HasData)
            {
                sb.Append(
                    " No test data is declared for this test case; annotate the fixture or method with [RequireTestData]."
                );
                return sb.ToString();
            }

            sb.Append(" Declared test data:");
            foreach (var descriptor in m_Descriptors)
            {
                if (descriptor == null)
                    continue;

                sb.Append($"\n\tbundle '{descriptor.BundleName}' <- {string.Join(", ", descriptor.AssetPatterns)}");
            }

#if UNITY_EDITOR
            if (!ForcePlayerBundles)
            {
                sb.Append("\nResolved assets:");
                foreach (var entry in EditorAssets)
                {
                    sb.Append($"\n\t{entry.AddressableName}");
                }

                return sb.ToString();
            }
#endif
            sb.Append("\nContent bundles:");
            var anyBundle = false;
            foreach (var bundle in GetBundles())
            {
                anyBundle = true;
                sb.Append($"\n\t{bundle.Name} (state: {bundle.State}) containing:");
                foreach (var name in bundle.GetAssetNames())
                    sb.Append($"\n\t\t{name}");
            }

            if (!anyBundle)
            {
                sb.Append(
                    "\n\tnone. The test data bundles were not built into this player, or content loading has "
                        + "not finished; wait for TestContentLoader.WaitForContentLoadAsync in a UnitySetUp method."
                );
            }

            return sb.ToString();
        }

#if UNITY_EDITOR
        readonly struct EditorAssetEntry
        {
            public readonly string AddressableName;
            public readonly string AssetPath;

            public EditorAssetEntry(string addressableName, string assetPath)
            {
                AddressableName = addressableName;
                AssetPath = assetPath;
            }
        }

        List<EditorAssetEntry> m_EditorAssets;

        List<EditorAssetEntry> EditorAssets
        {
            get
            {
                if (m_EditorAssets != null)
                    return m_EditorAssets;

                m_EditorAssets = new List<EditorAssetEntry>();
                // One asset covered by two declarations is still one asset; keeping both would
                // report it as an ambiguous match.
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var descriptor in m_Descriptors)
                {
                    if (descriptor == null)
                        continue;

                    foreach (var assetPath in descriptor.GetAssetPaths())
                    {
                        var addressableName = descriptor.GetAddressableName(assetPath);
                        if (seen.Add(addressableName))
                            m_EditorAssets.Add(new EditorAssetEntry(addressableName, assetPath));
                    }
                }

                return m_EditorAssets;
            }
        }

        EditorAssetEntry? FindEditorAsset(string assetName)
        {
            foreach (var entry in EditorAssets)
            {
                if (string.Equals(entry.AddressableName, assetName, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }

            EditorAssetEntry? firstMatch = null;
            var matchCount = 0;
            foreach (var entry in EditorAssets)
            {
                // Resolve against the addressable name: a bundle never knew the source path, so
                // matching on it would resolve names in the Editor that no player can serve.
                var fileName = System.IO.Path.GetFileName(entry.AddressableName);
                var stem = System.IO.Path.GetFileNameWithoutExtension(entry.AddressableName);
                if (
                    string.Equals(fileName, assetName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stem, assetName, StringComparison.OrdinalIgnoreCase)
                )
                {
                    firstMatch ??= entry;
                    matchCount++;
                }
            }

            if (matchCount > 1)
            {
                GraphicsTestLogger.LogWarning(
                    $"Test data lookup '{assetName}' matches {matchCount} declared assets; using "
                        + $"{firstMatch.Value.AssetPath}. Use the full asset path to disambiguate."
                );
            }

            return firstMatch;
        }
#endif

        IEnumerable<TestContentBundle> GetBundles()
        {
            // Declarations that share a bundle name resolve to one bundle; visiting it per
            // declaration would duplicate asset listings and diagnostics.
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var descriptor in m_Descriptors)
            {
                if (descriptor == null || !visited.Add(descriptor.BundleName ?? string.Empty))
                    continue;

                foreach (var bundle in BundleSource(descriptor.BundleName))
                    yield return bundle;
            }
        }
    }
}
