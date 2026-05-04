using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    class ComparisonLruCache
    {
        readonly int m_Capacity;
        readonly LinkedList<string> m_LruList = new();
        readonly Dictionary<string, ITextureComparisonResult> m_Cache = new();
        readonly object m_Lock = new();

        public ComparisonLruCache(int capacity)
        {
            m_Capacity = capacity;
        }

        public bool TryGet(string key, out ITextureComparisonResult result)
        {
            lock (m_Lock)
            {
                if (!m_Cache.TryGetValue(key, out result))
                    return false;

                m_LruList.Remove(key);
                m_LruList.AddFirst(key);
                return true;
            }
        }

        public bool TryAdd(string key, ITextureComparisonResult result)
        {
            lock (m_Lock)
            {
                if (!m_Cache.TryAdd(key, result))
                    return false;

                m_LruList.AddFirst(key);

                if (m_Cache.Count <= m_Capacity)
                    return true;

                var oldest = m_LruList.Last.Value;
                m_LruList.RemoveLast();
                m_Cache.Remove(oldest);

                return true;
            }
        }

        public void Clear()
        {
            lock (m_Lock)
            {
                m_Cache.Clear();
                m_LruList.Clear();
            }
        }
    }
}
