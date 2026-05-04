using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        bool ContentLoadDone
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
        public static IEnumerable<TestContentBundle> GetTestContentBundles()
        {
            var settings = GraphicsTestBuildSettings.LoadOrDefault();
            GraphicsTestLogger.DebugLog(
                $"Loading test content bundles... Found {settings.TestContentBundlePaths.Length} bundle path(s):\n\t{string.Join("\n\t", settings.TestContentBundlePaths)}"
            );

            if (GraphicsTestPlatform.Current.IsEditorPlatform)
            {
                foreach (var schema in settings.BuildPlatformSchemata)
                {
                    foreach (var path in GraphicsTestPlatform.GetCurrent(schema).AllResultsPaths)
                    {
                        yield return new EditorReferenceImageBundle(path);
                    }
                }
            }
            else
            {
                foreach (var bundlePath in settings.TestContentBundlePaths)
                {
                    yield return GraphicsTestPlatform.Current.GetValue<RuntimePlatform>() switch
                    {
                        RuntimePlatform.Android or RuntimePlatform.WebGLPlayer => new RemoteReferenceImageAssetBundle(
                            Path.Combine(Application.streamingAssetsPath, bundlePath)
                        ),
                        _ => new ReferenceImageAssetBundle(Path.Combine(Application.streamingAssetsPath, bundlePath)),
                    };
                }
            }
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
                var asset = bundle.LoadAsset<T>(assetName);
                if (asset != null)
                {
                    loadMessage =
                        $"Loaded {assetName} from {bundle.GetType().Name} bundle {bundle.Name} at path {bundle.AssetPath(assetName)}";
                    return asset;
                }
            }

            var sb = new StringBuilder();
            foreach (var b in Bundles)
            {
                if (sb.Length > 0)
                    sb.Append("\n\t");
                sb.Append(b.Name).Append(" (status: ").Append(b.State).Append(") -> ").Append(b.AssetPath(assetName));
            }
            loadMessage = $"Failed to load {assetName} from any of {Bundles.Count} bundle(s):\n\t{sb}";
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
            if (!ContentLoadDone)
            {
                throw new InvalidOperationException(
                    "Cannot get asset path if the content bundle has not been loaded. Call TestContentLoader.WaitForContentLoadAsync() in a UnitySetUp method to ensure the test content is loaded before tests are run."
                );
            }

            foreach (var bundle in Bundles)
            {
                if (bundle.ContainsAsset(assetName))
                {
                    return bundle.AssetPath(assetName);
                }
            }

            return null;
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

                bundle
                    .LoadBundleAsync()
                    .ContinueWith(_ =>
                    {
                        MainThreadDispatcher.RunOnMainThread(() =>
                        {
                            if (bundle.State == TestContentBundle.LoadState.Loaded)
                                OnBundleLoaded(bundle);
                            else
                                GraphicsTestLogger.DebugLog($"Failed to load {bundle.GetType()} bundle {bundle.Name}");
                        });
                    });
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
