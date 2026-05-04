using System;
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

        PlatformPath(string rootPath, List<Enum> nodes)
        {
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            m_Root = rootPath;

            var arch = Architecture.X64;
            foreach (var n in nodes)
            {
                if (n is Architecture a)
                {
                    arch = a;
                    break;
                }
            }

            var sb = new StringBuilder(m_Root);
            var tempPaths = new List<string> { m_Root };
            foreach (var node in nodes)
            {
                if (node is Architecture)
                    continue;

                var nodeString = node is RuntimePlatform platform ? platform.ToUniqueString(arch) : node.ToString();
                sb.Append('/').Append(nodeString);
                tempPaths.Add(sb.ToString());
            }

            var currentPath = sb.ToString();
            // Reverse the order so most specific paths are checked first
            tempPaths.Reverse();
            m_AllPaths = tempPaths;
            m_RelativePath = currentPath.Substring(Math.Max(0, m_Root.Length)).Trim('/');
            m_Path = currentPath;
        }

        internal static PlatformPath Construct(PlatformSchema schema, params Enum[] values)
        {
            if (schema.Types.Count == 0)
                return new PlatformPath(schema.rootPath, new List<Enum>());

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

            return new PlatformPath(schema.rootPath, nodes);
        }
    }
}
