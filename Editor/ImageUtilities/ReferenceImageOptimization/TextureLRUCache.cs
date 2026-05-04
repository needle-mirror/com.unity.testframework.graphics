using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    class TextureLruCache
    {
        readonly int m_Capacity;
        readonly LinkedList<string> m_LruList = new();
        readonly Dictionary<string, Texture2D> m_Cache = new();
        readonly object m_Lock = new();
        readonly IAssetService<Texture2D> m_AssetService;

        public TextureLruCache(int capacity, IAssetService<Texture2D> provider)
        {
            m_Capacity = capacity;
            m_AssetService = provider;
        }

        public bool TryGet(string path, out Texture2D texture)
        {
            lock (m_Lock)
            {
                if (m_Cache.TryGetValue(path, out texture))
                {
                    m_LruList.Remove(path);
                    m_LruList.AddFirst(path);
                    return true;
                }
                return false;
            }
        }

        public bool TryAdd(string path, Texture2D texture)
        {
            lock (m_Lock)
            {
                if (!m_Cache.TryAdd(path, texture))
                    return false;

                m_LruList.AddFirst(path);

                if (m_Cache.Count <= m_Capacity)
                    return true;

                var oldest = m_LruList.Last.Value;
                m_LruList.RemoveLast();

                if (!m_Cache.TryGetValue(oldest, out var toDestroy))
                    return true;

                Task.Run(
                    () =>
                        MainThreadDispatcher.RunOnMainThread(() =>
                        {
                            if (toDestroy != null && m_AssetService.ContainsAsset(toDestroy))
                            {
                                Resources.UnloadAsset(toDestroy);
                            }
                        })
                );

                m_Cache.Remove(oldest);
                return true;
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                foreach (var tex in m_Cache.Values)
                {
                    if (tex != null && m_AssetService.ContainsAsset(tex))
                    {
                        Resources.UnloadAsset(tex);
                    }
                }
                m_Cache.Clear();
                m_LruList.Clear();
            }
        }
    }
}
