using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A test data content bundle loaded from a local file. Assets are addressed as-given, so
    /// assets sharing a file name stay distinguishable by full path.
    /// </summary>
    class TestDataAssetBundle : ReferenceImageAssetBundle
    {
        internal TestDataAssetBundle(string path)
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
