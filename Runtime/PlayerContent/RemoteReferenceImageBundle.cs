using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace UnityEngine.TestTools.Graphics
{
    class RemoteReferenceImageAssetBundle : TestContentBundle
    {
        protected AssetBundle m_AssetBundle;
        const int k_MaxRetryCount = 3;

        internal RemoteReferenceImageAssetBundle(string path)
            : base(path) { }

        public override event Action<TestContentBundle> OnBundleLoaded;

        public override async Task LoadBundleAsync()
        {
            await Awaitable.MainThreadAsync();
            await Awaitable.FixedUpdateAsync();

            for (var r = k_MaxRetryCount; r > 0; r--)
            {
                GraphicsTestLogger.Log($"Loading asset bundle {Name}, retries remaining: {r}");
                using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(Path))
                {
                    await uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        GraphicsTestLogger.Log(
                            LogType.Log,
                            $"Encountered error loading async reference image asset bundle from {Path}: {uwr.error}"
                        );
                        m_LoadState = LoadState.Failed;
                    }
                    else
                    {
                        m_AssetBundle = DownloadHandlerAssetBundle.GetContent(uwr);

                        GraphicsTestLogger.Log(
                            LogType.Log,
                            $"Successfully loaded async reference image asset bundle from {Path} with assets:\n{string.Join("\n", m_AssetBundle.GetAllAssetNames())}"
                        );
                        m_LoadState = LoadState.Loaded;
                        OnBundleLoaded?.Invoke(this);
                        break;
                    }
                }

                await Task.Yield();
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

        public override void Unload()
        {
            if (m_AssetBundle != null)
            {
                m_AssetBundle.Unload(true);
                m_AssetBundle = null;
            }

            m_LoadState = LoadState.NotLoaded;
        }

        public override bool ContainsAsset(string assetName)
        {
            return m_AssetBundle?.Contains(System.IO.Path.GetFileNameWithoutExtension(assetName)) ?? false;
        }
    }
}
