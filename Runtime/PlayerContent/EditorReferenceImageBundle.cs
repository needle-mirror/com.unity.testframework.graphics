using System;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics
{
    class EditorReferenceImageBundle : TestContentBundle
    {
        public EditorReferenceImageBundle(string path)
            : base(path) { }

        public override bool AlwaysReloadAssets { get; set; } = true;

        public override event Action<TestContentBundle> OnBundleLoaded;

        public override async Task LoadBundleAsync()
        {
            await Awaitable.MainThreadAsync();

            if (System.IO.Directory.Exists(Path))
            {
                m_LoadState = LoadState.Loaded;
                OnBundleLoaded?.Invoke(this);
            }
            else
            {
                GraphicsTestLogger.Log(LogType.Log, $"Reference images not found at {Path}");
                m_LoadState = LoadState.Failed;
            }
        }

        public override T LoadAsset<T>(string assetName)
        {
#if UNITY_EDITOR
            if (m_LoadState != LoadState.Loaded)
            {
                GraphicsTestLogger.DebugLog($"Cannot load asset '{assetName}' from editor bundle '{Name}': bundle state is {m_LoadState}");
                return null;
            }

            var assetPath = AssetPath(assetName);
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                GraphicsTestLogger.DebugLog($"Reference image asset not found at {assetPath}");
            }
            return asset;
#else
            throw new System.NotSupportedException(
                "EditorReferenceImageBundle.LoadAsset is not supported outside of the editor"
            );
#endif
        }

        public override string AssetPath(string assetName)
        {
            return string.Join('/', Path, assetName);
        }

        public override bool ContainsAsset(string assetName)
        {
            return System.IO.File.Exists(AssetPath(assetName));
        }

        public override void Unload()
        {
            m_LoadState = LoadState.NotLoaded;
        }
    }
}
