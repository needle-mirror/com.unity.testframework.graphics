using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Abstract class representing a content bundle for graphics tests.
    /// This class provides a base for loading and unloading content bundles,
    /// </summary>
    public abstract class TestContentBundle
    {
        /// <summary>
        /// Enumeration of the load state of the content bundle.
        /// This is used to track the loading state of the content bundle.
        /// It contains options for not loaded, loading, loaded, and failed states.
        /// </summary>
        public enum LoadState
        {
            /// <summary>
            /// The content bundle has not been loaded yet.
            /// </summary>
            NotLoaded,

            /// <summary>
            /// The content bundle is currently being loaded.
            /// </summary>
            Loading,

            /// <summary>
            /// The content bundle has been successfully loaded.
            /// </summary>
            Loaded,

            /// <summary>
            /// The content bundle failed to load.
            /// This can happen if the content bundle is not found or if there is an error during loading.
            /// </summary>
            Failed,
        }

        /// <summary>
        /// The current load state of the content bundle.
        /// This is used to track the loading state of the content bundle.
        /// It can be one of the values from the <see cref="LoadState"/> enumeration.
        /// </summary>
        protected LoadState m_LoadState = LoadState.NotLoaded;

        /// <summary>
        /// The name of the content bundle.
        /// This is used to identify the content bundle.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The path to the content bundle.
        /// This is used to locate the content bundle on disk.
        /// </summary>
        protected string Path { get; }

        /// <summary>
        /// The chunk index parsed from the trailing "-N" of <see cref="Name"/>. Content bundles are
        /// loaded in ascending order of it. It does not affect the order they are searched in, which
        /// is the order <see cref="TestContentLoader"/> discovered them (most specific platform first).
        /// </summary>
        public int Priority => GetPriorityFromIndex(Name);

        /// <summary>
        /// The current load state of the content bundle.
        /// This is used to track the loading state of the content bundle.
        /// It can be one of the values from the <see cref="LoadState"/> enumeration.
        /// </summary>
        public LoadState State
        {
            get => m_LoadState;
            internal set => m_LoadState = value;
        }

        /// <summary>
        /// Whether to always reload assets from the content bundle.
        /// </summary>
        public virtual bool AlwaysReloadAssets { get; set; } = false;

        /// <summary>
        /// Whether the bundle participates in <see cref="TestContentLoader"/>'s global asset search.
        /// Test data bundles opt out so their assets never shadow reference images (and vice versa);
        /// they are resolved only through <see cref="GraphicsTestData"/> via <see cref="LogicalName"/>.
        /// </summary>
        public bool PartOfGlobalSearch { get; set; } = true;

        /// <summary>
        /// The logical name a test data bundle was declared under, or null for content bundles that
        /// are part of the global search. <see cref="GraphicsTestData"/> resolves its bundles by
        /// this name, so a bundle registered through <see cref="TestContentLoader.RegisterBundle"/>
        /// must set it to serve the test data declared under that name.
        /// </summary>
        public string LogicalName { get; set; }

        /// <summary>
        /// Constructor for the TestContentBundle class.
        /// This constructor initializes the content bundle with the specified path.
        /// It extracts the name of the content bundle from the path and sets the priority based on the index in the name.
        /// </summary>
        /// <param name="path">
        /// The path to the content bundle.
        /// </param>
        protected TestContentBundle(string path)
        {
            Path = path;
            Name = path.Replace('/', '-');
        }

        /// <summary>
        /// Asynchronously loads the content bundle.
        /// This method should be overridden in derived classes to provide the actual loading logic.
        /// </summary>
        /// <returns>
        /// A task representing the asynchronous operation of loading the content bundle.
        /// </returns>
        public abstract Task LoadBundleAsync();

        /// <summary>
        /// Loads an asset from the content bundle.
        /// This method should be overridden in derived classes to provide the actual loading logic.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the asset to load.
        /// This type must derive from <see cref="UnityEngine.Object"/>.
        /// </typeparam>
        /// <param name="assetName">
        /// The name of the asset to load.
        /// This name is used to locate the asset within the content bundle.
        /// </param>
        /// <returns>
        /// The loaded asset of type <typeparamref name="T"/>.
        /// This asset is loaded from the content bundle and can be used in the test.
        /// </returns>
        public abstract T LoadAsset<T>(string assetName)
            where T : Object;

        /// <summary>
        /// Unloads the content bundle.
        /// This method should be overridden in derived classes to provide the actual unloading logic.
        /// </summary>
        public abstract void Unload();

        /// <summary>
        /// Checks if the content bundle contains an asset with the specified name.
        /// This method should be overridden in derived classes to provide the actual checking logic.
        /// </summary>
        /// <param name="assetName">
        /// The name of the asset to check for.
        /// </param>
        /// <returns>
        /// True if the content bundle contains the asset with the specified name, false otherwise.
        /// </returns>
        public abstract bool ContainsAsset(string assetName);

        /// <summary>
        /// Enumerates the addressable names of the assets in the content bundle. Bundles that
        /// cannot enumerate their contents return an empty sequence.
        /// </summary>
        /// <returns>The addressable asset names, or an empty sequence.</returns>
        public virtual IEnumerable<string> GetAssetNames() => Array.Empty<string>();

        /// <summary>
        /// Gets the path to an asset within the content bundle.
        /// This method should be overridden in derived classes to provide the actual path logic.
        /// </summary>
        /// <param name="assetName">
        /// The name of the asset to get the path for.
        /// </param>
        /// <returns>
        /// The path to the asset within the content bundle.
        /// </returns>
        public virtual string AssetPath(string assetName) => $"{Path}/{assetName}";

        /// <summary>
        /// Event that is triggered when the content bundle is loaded.
        /// This event is used to notify subscribers when the content bundle has been successfully loaded.
        /// </summary>
        /// <remarks>
        /// This event is triggered when the content bundle is loaded.
        /// Subscribers can use this event to perform actions after the content bundle has been loaded.
        /// </remarks>
        public abstract event Action<TestContentBundle> OnBundleLoaded;

        int GetPriorityFromIndex(string name)
        {
            var index = name.LastIndexOf('-');
            if (index == -1)
                return 0;
            try
            {
                return int.Parse(name.Substring(index + 1));
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.DebugLog($"Failed to parse bundle priority from '{name}': {ex.Message}");
                return 0;
            }
        }
    }
}
