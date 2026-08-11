using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// TestContentLoader is a class that loads test content bundles.
    /// It is used to load test content for graphics tests.
    /// It provides methods to load, unload, and reload test content.
    /// It also provides methods to check the load state of the content bundles.
    /// </summary>
    public class TestContentLoader
    {
        static TestContentLoader s_TestContentLoader;
        internal static TestContentLoader ContentLoader =>
            s_TestContentLoader ??= new TestContentLoader(GetTestContentBundles());

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void OnBeforeSplashScreen()
        {
            ContentLoader.LoadContent();
        }

        /// <summary>
        /// Waits for the content to load.
        /// This method is used to wait for the content to load before running tests.
        /// </summary>
        /// <param name="timeout">
        /// The timeout for the content load.
        /// If the content does not load within this time, a TimeoutException is thrown.
        /// This is used to prevent tests from hanging indefinitely.
        /// </param>
        /// <returns>
        /// An enumerator that waits for the content to load.
        /// </returns>
        /// <exception cref="TimeoutException">
        /// Thrown if the content does not load within the specified timeout.
        /// </exception>
        public static IEnumerator WaitForContentLoadAsync(TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (!ContentLoader.ContentLoadDone)
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException(
                        $"Timed out waiting for test content to load after {timeout.TotalSeconds} seconds."
                    );
                }

                yield return null;
            }

            stopwatch.Stop();
        }

        /// <summary>
        /// The list of content bundles that this loader manages.
        /// </summary>
        /// <remarks>
        /// This is used to track the content bundles that are loaded and unloaded.
        /// </remarks>
        IList<TestContentBundle> Bundles { get; set; }

        internal bool ContentLoadDone
        {
            get
            {
                foreach (var b in Bundles)
                {
                    if (b.State != TestContentBundle.LoadState.Loaded && b.State != TestContentBundle.LoadState.Failed)
                        return false;
                }
                return true;
            }
        }

        bool ContentLoadInProgress
        {
            get
            {
                foreach (var b in Bundles)
                {
                    if (b.State == TestContentBundle.LoadState.Loading)
                        return true;
                }
                return false;
            }
        }

        internal bool ShouldReloadAssets
        {
            get
            {
                foreach (var b in Bundles)
                {
                    if (b.AlwaysReloadAssets)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Constructor for the TestContentLoader class.
        /// This constructor initializes the content loader with the specified content bundles.
        /// </summary>
        /// <param name="bundles">
        /// The content bundles to load.
        /// </param>
        public TestContentLoader(IEnumerable<TestContentBundle> bundles)
        {
            Bundles = new List<TestContentBundle>(bundles ?? Array.Empty<TestContentBundle>());
            if (Bundles.Count == 0)
                GraphicsTestLogger.Log(LogType.Warning, $"No test content bundles were found.");
            LoadContent();
        }

        /// <summary>
        /// Gets the test content bundles.
        /// This method is used to get the test content bundles for the current platform.
        /// </summary>
        /// <returns>
        /// An enumerable of test content bundles.
        /// </returns>
        public static IEnumerable<TestContentBundle> GetTestContentBundles() =>
            GetTestContentBundles(GraphicsTestBuildSettings.LoadOrDefault(), GraphicsTestPlatform.Current);

        /// <summary>
        /// Discovers and classifies the content bundles for the given settings and platform,
        /// so tests can drive the real discovery with a simulated platform.
        /// </summary>
        internal static IEnumerable<TestContentBundle> GetTestContentBundles(
            GraphicsTestBuildSettings settings,
            GraphicsTestPlatform currentPlatform
        )
        {
            GraphicsTestLogger.DebugLog(
                $"Loading test content bundles... Found {settings.TestContentBundlePaths.Length} bundle path(s):\n\t{string.Join("\n\t", settings.TestContentBundlePaths)}"
            );

            if (currentPlatform.IsEditorPlatform)
            {
                foreach (var schema in settings.BuildPlatformSchemata)
                {
                    foreach (var path in new GraphicsTestPlatform(currentPlatform, schema).AllResultsPaths)
                    {
                        yield return new EditorReferenceImageBundle(path);
                    }
                }
            }
            else
            {
                var runtimePlatform = currentPlatform.GetValue<RuntimePlatform>();

                // A build can carry bundles for several platform variants (e.g. per GPU vendor).
                // Consult the most specifically matching bundles first (Load<T> returns the first
                // bundle that contains an asset); bundles that conflict with the running platform on
                // any characteristic are excluded entirely.
                var rankedPaths = RankBundlePathsForPlatform(
                    settings.TestContentBundlePaths,
                    settings.TestContentBundlePlatforms,
                    currentPlatform
                );

                var testDataByFileName = new Dictionary<string, TestDataBundleInfo>();
                foreach (var info in settings.TestDataBundles)
                {
                    if (info != null && !string.IsNullOrEmpty(info.bundleFileName))
                        testDataByFileName[info.bundleFileName] = info;
                }

                var useRemoteBundles =
                    runtimePlatform is RuntimePlatform.Android or RuntimePlatform.WebGLPlayer;

                foreach (var bundlePath in rankedPaths)
                {
                    var fullPath = Path.Combine(Application.streamingAssetsPath, bundlePath);

                    // Bundles recorded as test data are addressed as-given (full asset path or file
                    // name) and stay out of the global search so they never shadow reference images.
                    if (testDataByFileName.TryGetValue(bundlePath, out var testDataInfo))
                    {
                        TestContentBundle testDataBundle = useRemoteBundles
                            ? new RemoteTestDataAssetBundle(fullPath)
                            : new TestDataAssetBundle(fullPath);
                        testDataBundle.LogicalName = testDataInfo.logicalName;
                        yield return testDataBundle;
                        continue;
                    }

                    yield return useRemoteBundles
                        ? new RemoteReferenceImageAssetBundle(fullPath)
                        : new ReferenceImageAssetBundle(fullPath);
                }
            }
        }

        /// <summary>
        /// Orders bundle paths so the most specifically matching bundles are consulted first. A bundle
        /// that conflicts with the running platform on any characteristic is dropped: its references
        /// were authored for other hardware. Bundles without metadata stay neutral, and the sort is
        /// stable, so a build without metadata loads exactly as before.
        /// </summary>
        internal static List<string> RankBundlePathsForPlatform(
            IReadOnlyList<string> bundlePaths,
            IReadOnlyList<TestContentBundlePlatformInfo> bundlePlatforms,
            GraphicsTestPlatform currentPlatform
        )
        {
            var infosByName = new Dictionary<string, TestContentBundlePlatformInfo>();
            if (bundlePlatforms != null)
            {
                foreach (var info in bundlePlatforms)
                {
                    if (info != null && !string.IsNullOrEmpty(info.bundleName))
                        infosByName[info.bundleName] = info;
                }
            }

            var ranked = new List<(string Path, int Score, int Index)>(bundlePaths.Count);
            for (var i = 0; i < bundlePaths.Count; i++)
            {
                var path = bundlePaths[i];
                if (!infosByName.TryGetValue(path, out var info))
                {
                    ranked.Add((path, 0, i)); // no metadata: legacy build, stay neutral
                    continue;
                }

                if (TryScorePlatformMatch(info.ResolveData(), currentPlatform, out var score))
                    ranked.Add((path, score, i));
            }

            // List.Sort is unstable; the index tiebreak keeps build order for equal scores.
            ranked.Sort((a, b) => a.Score != b.Score ? b.Score.CompareTo(a.Score) : a.Index.CompareTo(b.Index));

            var result = new List<string>(ranked.Count);
            foreach (var entry in ranked)
                result.Add(entry.Path);
            return result;
        }

        /// <summary>
        /// Scores how specifically a bundle's build platform matches the running platform: every
        /// shared characteristic adds one. Returns false when any characteristic the bundle declares
        /// differs from the running platform's value. A characteristic the running platform does not
        /// carry stays neutral. Characteristics compare by enum value, so aliased members match.
        /// </summary>
        internal static bool TryScorePlatformMatch(
            IReadOnlyDictionary<Type, Enum> bundleData,
            GraphicsTestPlatform currentPlatform,
            out int score
        )
        {
            score = 0;
            foreach (var pair in bundleData)
            {
                if (!currentPlatform.Data.TryGetValue(pair.Key, out var currentValue))
                    continue; // the running platform doesn't know this dimension: neutral

                if (!currentValue.Equals(pair.Value))
                    return false;

                score++;
            }

            return true;
        }

        /// <summary>
        /// Loads an asset from the content bundles.
        /// This method is used to load assets from the content bundles.
        /// It searches through all the content bundles to find the asset with the specified name.
        /// </summary>
        /// <typeparam name="T">
        /// The type of asset to load.
        /// This is used to specify the type of asset to load from the content bundles.
        /// </typeparam>
        /// <param name="assetName">
        /// The name of the asset to load.
        /// This name is used to locate the asset within the content bundles.
        /// </param>
        /// <param name="loadMessage">
        /// The load message for the asset, indicating any errors that occurred during loading.
        /// </param>
        /// <returns>
        /// The loaded asset of type <typeparamref name="T"/>.
        /// This asset is loaded from the content bundles and can be used in the test.
        /// </returns>
        public T Load<T>(string assetName, out string loadMessage)
            where T : Object
        {
            foreach (var bundle in Bundles)
            {
                if (!bundle.PartOfGlobalSearch)
                    continue;

                var asset = bundle.LoadAsset<T>(assetName);
                if (asset != null)
                {
                    loadMessage =
                        $"Loaded {assetName} from {bundle.GetType().Name} bundle {bundle.Name} at path {bundle.AssetPath(assetName)}";
                    return asset;
                }
            }

            var sb = new StringBuilder();
            var searchedBundles = 0;
            foreach (var b in Bundles)
            {
                if (!b.PartOfGlobalSearch)
                    continue;

                searchedBundles++;
                if (sb.Length > 0)
                    sb.Append("\n\t");
                sb.Append(b.Name).Append(" (status: ").Append(b.State).Append(") -> ").Append(b.AssetPath(assetName));
            }
            loadMessage = $"Failed to load {assetName} from any of {searchedBundles} bundle(s):\n\t{sb}";
            return null;
        }

        /// <summary>
        /// Gets the path to an asset within the content bundles.
        /// This method is used to get the path to an asset within the content bundles.
        /// It searches through all the content bundles to find the asset with the specified name.
        /// </summary>
        /// <param name="assetName">
        /// The name of the asset to get the path for.
        /// This name is used to locate the asset within the content bundles.
        /// </param>
        /// <returns>
        /// The path to the asset within the content bundles.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the content bundle has not been loaded.
        /// </exception>
        public string AssetPath(string assetName)
        {
            foreach (var bundle in Bundles)
            {
                if (bundle.PartOfGlobalSearch && bundle.ContainsAsset(assetName))
                {
                    return bundle.AssetPath(assetName);
                }
            }

            // Only a miss is ambiguous: content still in flight may yet provide the asset.
            if (!ContentLoadDone)
            {
                throw new InvalidOperationException(
                    "Cannot get asset path if the content bundle has not been loaded. Call TestContentLoader.WaitForContentLoadAsync() in a UnitySetUp method to ensure the test content is loaded before tests are run."
                );
            }

            return null;
        }

        /// <summary>
        /// The test data bundles registered under the given logical name; how
        /// <see cref="GraphicsTestData"/> resolves its bundles outside the global search.
        /// </summary>
        internal IEnumerable<TestContentBundle> GetBundlesFor(string logicalName)
        {
            // An unnamed bundle is a reference image bundle, never test data; matching null to null
            // would hand every one of them to GraphicsTestData.
            if (string.IsNullOrEmpty(logicalName))
                return Array.Empty<TestContentBundle>();

            var matches = new List<TestContentBundle>();
            foreach (var bundle in Bundles)
            {
                if (string.Equals(bundle.LogicalName, logicalName, StringComparison.Ordinal))
                    matches.Add(bundle);
            }

            return matches;
        }

        /// <summary>
        /// Registers an additional content bundle and starts loading it; the extension point
        /// for custom <see cref="TestContentBundle"/> implementations. Registered bundles are
        /// searched after the ones discovered from the build settings.
        /// </summary>
        /// <param name="bundle">The bundle to register.</param>
        public void RegisterBundle(TestContentBundle bundle)
        {
            if (bundle == null)
                throw new ArgumentNullException(nameof(bundle));

            Bundles.Add(bundle);

            if (bundle.State != TestContentBundle.LoadState.NotLoaded)
                return;

            GraphicsTestLogger.DebugLog($"Loading {bundle.GetType()} bundle {bundle.Name}...");
            bundle.State = TestContentBundle.LoadState.Loading;
            bundle.LoadBundleAsync().ContinueWith(task => OnLoadCompleted(bundle, task));
        }

        /// <summary>
        /// Loads the content bundles.
        /// This method is used to load the content bundles for the graphics tests.
        /// It loads the content bundles asynchronously and tracks the load state of each bundle.
        /// </summary>
        /// <remarks>
        /// This method is called when the test content loader is initialized.
        /// It loads the content bundles in the order of their priority.
        /// The content bundles are loaded asynchronously to prevent blocking the main thread.
        /// The load state of each bundle is tracked to determine if the content load is complete.
        /// </remarks>
        public void LoadContent()
        {
            if (ContentLoadDone)
            {
                GraphicsTestLogger.DebugLog("All test content already loaded.");
                return;
            }

            if (ContentLoadInProgress)
            {
                GraphicsTestLogger.DebugLog("Test content loading already in progress.");
                return;
            }

            GraphicsTestLogger.DebugLog("Loading test content...");

            var sortedBundles = new List<TestContentBundle>(Bundles);
            sortedBundles.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            foreach (var bundle in sortedBundles)
            {
                GraphicsTestLogger.DebugLog($"Loading {bundle.GetType()} bundle {bundle.Name}...");
                bundle.State = TestContentBundle.LoadState.Loading;

                bundle.LoadBundleAsync().ContinueWith(task => OnLoadCompleted(bundle, task));
            }
        }

        /// <summary>
        /// Reloads the content bundles.
        /// This method is used to reload the content bundles for the graphics tests.
        /// It unloads the existing content bundles and loads them again.
        /// </summary>
        public void ReloadContent()
        {
            UnloadContent();
            LoadContent();
        }

        /// <summary>
        /// Unloads the content bundles.
        /// This method is used to unload the content bundles for the graphics tests.
        /// It unloads the content bundles to free up memory and resources.
        /// </summary>
        public void UnloadContent()
        {
            foreach (var bundle in Bundles)
            {
                bundle.Unload();
            }
        }

        /// <summary>
        /// Resets the content loader.
        /// </summary>
        public static void Reset()
        {
            s_TestContentLoader = new TestContentLoader(GetTestContentBundles());
            s_TestContentLoader.LoadContent();
        }

        /// <summary>
        /// Settles a bundle's load state. A bundle whose load threw would otherwise stay
        /// <see cref="TestContentBundle.LoadState.Loading"/> forever, so content loading would
        /// never finish and the exception would surface only as an unobserved task fault.
        /// </summary>
        void OnLoadCompleted(TestContentBundle bundle, Task task)
        {
            var error = task.Exception;

            // Settled here rather than in the dispatched callback: the dispatcher posts to Unity's
            // synchronization context, which is not pumped in every host, and a bundle left in
            // Loading would block content loading forever.
            if (error != null)
                bundle.State = TestContentBundle.LoadState.Failed;

            MainThreadDispatcher.RunOnMainThread(() =>
            {
                if (error != null)
                {
                    GraphicsTestLogger.Log(
                        LogType.Warning,
                        $"Failed to load {bundle.GetType().Name} bundle {bundle.Name}: {error.InnerException ?? error}"
                    );
                    return;
                }

                if (bundle.State == TestContentBundle.LoadState.Loaded)
                    OnBundleLoaded(bundle);
                else
                    GraphicsTestLogger.DebugLog($"Failed to load {bundle.GetType()} bundle {bundle.Name}");
            });
        }

        /// <summary>
        /// Called when a content bundle is loaded.
        /// This method is used to handle the completion of the content bundle loading.
        /// It checks the load state of the bundle and logs the result.
        /// </summary>
        /// <param name="bundle">
        /// The content bundle that was loaded.
        /// </param>
        void OnBundleLoaded(TestContentBundle bundle)
        {
            if (bundle == null)
            {
                GraphicsTestLogger.Log(LogType.Error, $"Failed to load test content bundle.");
                return;
            }

            GraphicsTestLogger.DebugLog($"{bundle.GetType().Name} {bundle.Name} loaded successfully.");

            if (ContentLoadDone)
            {
                GraphicsTestLogger.DebugLog("All test content finished loading.");

                foreach (var b in Bundles)
                {
                    GraphicsTestLogger.DebugLog($"{bundle.GetType().Name} {b.Name} has {b.State} state.");
                }
            }
        }
    }
}
