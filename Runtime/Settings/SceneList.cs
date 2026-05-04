using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    [Serializable]
    class SceneList : ScriptableObject
    {
        [SerializeField]
        internal List<string> scenePaths;

        [SerializeField]
        internal MethodIdentifier id;

        internal List<string> ScenePaths => scenePaths;
    }

    class SceneListComparer : IEqualityComparer<SceneList>
    {
        public bool Equals(SceneList x, SceneList y)
        {
            if (x == null && y == null)
                return true;
            if (x == null || y == null)
                return false;
            return x.id.Equals(y.id);
        }

        public int GetHashCode(SceneList obj)
        {
            return obj.id.GetHashCode();
        }
    }
}
