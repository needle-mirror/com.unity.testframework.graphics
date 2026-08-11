using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Serialized identity of the platform a test content bundle was built for. Written during the
    /// player content build and read back by the runtime loader to rank bundles against the platform
    /// the player is actually running on. Enum types are stored by assembly-qualified name and values
    /// by member name; entries that no longer resolve are skipped with a warning when read. Aliased
    /// enum members (distinct names sharing one value) round-trip safely because ranking compares
    /// values, not names.
    /// </summary>
    [Serializable]
    class TestContentBundlePlatformInfo
    {
        [SerializeField]
        internal string bundleName;

        [SerializeField]
        internal string schemaName;

        [SerializeField]
        internal string[] typeNames = Array.Empty<string>();

        [SerializeField]
        internal string[] valueNames = Array.Empty<string>();

        internal static TestContentBundlePlatformInfo From(string bundleName, GraphicsTestPlatform platform)
        {
            var info = new TestContentBundlePlatformInfo
            {
                bundleName = bundleName,
                schemaName = platform?.Schema?.name ?? string.Empty,
            };

            if (platform?.Data == null)
                return info;

            var typeNames = new List<string>(platform.Data.Count);
            var valueNames = new List<string>(platform.Data.Count);
            foreach (var pair in platform.Data)
            {
                typeNames.Add(pair.Key.AssemblyQualifiedName);
                valueNames.Add(pair.Value.ToString());
            }

            info.typeNames = typeNames.ToArray();
            info.valueNames = valueNames.ToArray();
            return info;
        }

        /// <summary>
        /// Resolves the serialized names back to enum values. Unresolvable entries — a type or member
        /// that no longer exists, e.g. after building with a different framework version — are skipped
        /// with a warning so a partially resolvable bundle still participates in ranking.
        /// </summary>
        internal IReadOnlyDictionary<Type, Enum> ResolveData()
        {
            var data = new Dictionary<Type, Enum>();
            if (typeNames == null || valueNames == null)
                return data;

            var count = Math.Min(typeNames.Length, valueNames.Length);
            for (var i = 0; i < count; i++)
            {
                var type = Type.GetType(typeNames[i]);
                if (type == null || !type.IsEnum)
                {
                    GraphicsTestLogger.LogWarning(
                        $"TestContentBundlePlatformInfo: could not resolve platform dimension type "
                            + $"'{typeNames[i]}' for bundle '{bundleName}'; skipping it."
                    );
                    continue;
                }

                object value;
                try
                {
                    value = Enum.Parse(type, valueNames[i], ignoreCase: true);
                }
                catch (Exception e) when (e is ArgumentException or OverflowException)
                {
                    GraphicsTestLogger.LogWarning(
                        $"TestContentBundlePlatformInfo: could not resolve value '{valueNames[i]}' of "
                            + $"type '{type.Name}' for bundle '{bundleName}'; skipping it."
                    );
                    continue;
                }

                data[type] = (Enum)value;
            }

            return data;
        }
    }
}
