using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    readonly struct PlatformPath
    {
        internal readonly string m_Root;
        internal readonly string m_Path;
        internal readonly string m_RelativePath;
        internal readonly List<string> m_AllPaths;
        internal readonly List<Enum[]> m_AllPathValues;

        PlatformPath(string rootPath, List<Enum> nodes, bool elideSegments)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            m_Root = rootPath;

            var arch = Architecture.X64;
            var hasExplicitArch = false;
            foreach (var n in nodes)
            {
                if (n is Architecture a)
                {
                    arch = a;
                    hasExplicitArch = true;
                    break;
                }
            }

            var sb = new StringBuilder(m_Root);
            var tempPaths = new List<string> { m_Root };
            var valuesSoFar = new List<Enum>();
            var tempValues = new List<Enum[]> { Array.Empty<Enum>() };
            foreach (var node in nodes)
            {
                if (node is Architecture)
                    continue;

                if (elideSegments && IsElidableFromPath(node))
                    continue;

                var nodeString = node is RuntimePlatform platform ? platform.ToUniqueString(arch) : GetPathSegment(node);
                sb.Append('/').Append(nodeString);
                tempPaths.Add(sb.ToString());

                valuesSoFar.Add(node);
                // A platform segment asserts the architecture only when it encodes it (e.g.
                // "OSXEditor_AppleSilicon"); a plain segment is the same folder for every architecture.
                if (node is RuntimePlatform p && hasExplicitArch && nodeString != p.ToUniqueString(Architecture.X64))
                    valuesSoFar.Add(arch);
                tempValues.Add(valuesSoFar.ToArray());
            }

            var currentPath = sb.ToString();
            // Reverse the order so most specific paths are checked first
            tempPaths.Reverse();
            tempValues.Reverse();
            m_AllPaths = tempPaths;
            m_AllPathValues = tempValues;
            m_RelativePath = currentPath.Substring(Math.Max(0, m_Root.Length)).Trim('/');
            m_Path = currentPath;
        }

        internal static PlatformPath Construct(PlatformSchema schema, params Enum[] values)
        {
            return Construct(schema, true, values);
        }

        internal static PlatformPath Construct(PlatformSchema schema, bool elideSegments, params Enum[] values)
        {
            if (schema.Types.Count == 0)
                return new PlatformPath(schema.rootPath, new List<Enum>(), elideSegments);

            var nodes = new List<Enum>();
            foreach (var type in schema.Types)
            {
                Enum value = null;
                foreach (var v in values)
                {
                    if (v != null && v.GetType() == type)
                    {
                        value = v;
                        break;
                    }
                }
                if (value != null)
                    nodes.Add(value);
            }

            var nodeSet = new HashSet<Enum>(nodes);
            foreach (var value in values)
            {
                if (value != null && !nodeSet.Contains(value))
                    nodes.Add(value);
            }

            return new PlatformPath(schema.rootPath, nodes, elideSegments);
        }

        /// <summary>
        /// The path segment for <paramref name="value"/>, delegated to the registered node for the
        /// value's type so a node can pin a canonical segment (e.g. for aliased enum members whose
        /// <c>ToString()</c> differs across scripting runtimes). Unregistered types keep their name.
        /// </summary>
        static string GetPathSegment(Enum value)
        {
            var node = PlatformNodeRegistry.GetNodeByDataType(value.GetType());
            return node != null ? node.GetPathSegment(value) : value.ToString();
        }

        static readonly ConcurrentDictionary<Enum, bool> s_ElidableCache = new();

        static bool IsElidableFromPath(Enum value)
        {
            return s_ElidableCache.GetOrAdd(value, v =>
            {
                var field = v.GetType().GetField(v.ToString());
                return field != null
                    && field.IsDefined(typeof(ElideFromPlatformPathAttribute), inherit: false);
            });
        }
    }
}
