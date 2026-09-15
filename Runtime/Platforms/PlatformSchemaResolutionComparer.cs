using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Equal when both schemata read from the same place, whatever their names. Collapses schemata
    /// from different sources that would otherwise build identical bundles.
    /// </summary>
    internal class PlatformSchemaResolutionComparer : IEqualityComparer<PlatformSchema>
    {
        public bool Equals(PlatformSchema x, PlatformSchema y)
        {
            if (x is null) return y is null;
            return x.ResolvesToSamePathsAs(y);
        }

        public int GetHashCode(PlatformSchema obj)
        {
            return HashCode.Combine(obj.rootPath, string.Join(";", obj.nodes ?? Array.Empty<string>()));
        }
    }
}
