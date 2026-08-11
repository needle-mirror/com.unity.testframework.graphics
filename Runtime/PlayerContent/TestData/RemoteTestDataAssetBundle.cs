using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A test data content bundle loaded through UnityWebRequest (Android, WebGL). Assets are
    /// addressed as-given (full asset path or file name), matching <see cref="TestDataAssetBundle"/>.
    /// </summary>
    class RemoteTestDataAssetBundle : RemoteReferenceImageAssetBundle
    {
        internal RemoteTestDataAssetBundle(string path)
            : base(path)
        {
            PartOfGlobalSearch = false;
        }

        public override T LoadAsset<T>(string assetName)
        {
            if (m_LoadState != LoadState.Loaded)
            {
                GraphicsTestLogger.DebugLog($"Cannot load asset '{assetName}' from bundle '{Name}': bundle state is {m_LoadState}");
                return null;
            }

            return TestDataAssetLookup.LoadAsset<T>(m_AssetBundle, assetName);
        }

        public override bool ContainsAsset(string assetName)
        {
            return TestDataAssetLookup.Contains(m_AssetBundle, assetName);
        }

        public override IEnumerable<string> GetAssetNames()
        {
            return m_AssetBundle != null ? m_AssetBundle.GetAllAssetNames() : base.GetAssetNames();
        }
    }
}
