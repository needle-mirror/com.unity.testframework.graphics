using System;
using System.IO;
using System.Threading.Tasks;

namespace UnityEngine.TestTools.Graphics
{
    class ReferenceImageAssetBundle : TestContentBundle
    {
        AssetBundle m_AssetBundle;

        internal ReferenceImageAssetBundle(string path)
            : base(path) { }

        public override event Action<TestContentBundle> OnBundleLoaded;

        public override async Task LoadBundleAsync()
        {
            GraphicsTestLogger.Log($"Loading asset bundle {Name}");
            await Awaitable.MainThreadAsync();
            await Awaitable.FixedUpdateAsync();

            if (!File.Exists(Path))
            {
                GraphicsTestLogger.Log(LogType.Warning, $"Reference image asset bundle not found at {Path}");
                m_LoadState = LoadState.Failed;
                return;
            }

            try
            {
                m_AssetBundle = AssetBundle.LoadFromFile(Path);
                if (m_AssetBundle == null)
                {
                    GraphicsTestLogger.Log(
                        LogType.Error,
                        $"Failed to load reference image asset bundle from {Path}. AssetBundle.LoadFromFile returned null."
                    );
                    m_LoadState = LoadState.Failed;
                    return;
                }

                GraphicsTestLogger.Log(
                    LogType.Log,
                    $"Received reference image asset bundle from {Path} with assets:\n{string.Join("\n", m_AssetBundle.GetAllAssetNames())}"
                );
                m_LoadState = LoadState.Loaded;
                OnBundleLoaded?.Invoke(this);
            }
            catch (Exception e)
            {
                GraphicsTestLogger.Log(
                    LogType.Error,
                    $"Encountered error loading reference image asset bundle from {Path}: {e.Message}"
                );
            }

            if (m_AssetBundle == null)
            {
                m_LoadState = LoadState.Failed;
            }
        }

        public override T LoadAsset<T>(string assetName)
        {
            if (m_LoadState != LoadState.Loaded)
            {
                GraphicsTestLogger.DebugLog($"Cannot load asset '{assetName}' from bundle '{Name}': bundle state is {m_LoadState}");
                return null;
            }

            return m_AssetBundle?.LoadAsset<T>(System.IO.Path.GetFileNameWithoutExtension(assetName));
        }

        public override bool ContainsAsset(string assetName)
        {
            return m_AssetBundle?.Contains(assetName) ?? false;
        }

        public override void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }

            m_LoadState = LoadState.NotLoaded;
        }
    }
}
