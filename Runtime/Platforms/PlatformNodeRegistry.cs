using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
#if UNITY_6000_5_OR_NEWER
using UnityEngine.Assemblies;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    static class PlatformNodeRegistry
    {
        static PlatformNodeRegistry()
        {
            LoadPluginsFromAssemblies();
        }

        internal static readonly Dictionary<string, IPlatformNode> k_Nodes = new();
        internal static readonly Dictionary<Type, IPlatformNode> k_NodesByType = new();
        internal static readonly Dictionary<Type, IPlatformNode> k_NodesByDataType = new();
        internal static readonly HashSet<Type> k_EnumTypes = new();

        internal static T GetNode<T>() where T : class, IPlatformNode
        {
            return k_NodesByType.TryGetValue(typeof(T), out var node) ? node as T : null;
        }

        internal static IPlatformNode GetNodeByType(Type type)
        {
            return k_NodesByType.TryGetValue(type, out var node) ? node : null;
        }

        /// <summary>
        /// Returns the node whose <see cref="IPlatformNode.DataType"/> is <paramref name="dataType"/>,
        /// or null when no node declares that enum type.
        /// </summary>
        internal static IPlatformNode GetNodeByDataType(Type dataType)
        {
            return k_NodesByDataType.TryGetValue(dataType, out var node) ? node : null;
        }

        /// <summary>
        /// Returns all registered nodes in a deterministic order (sorted by name).
        /// </summary>
        internal static List<IPlatformNode> GetOrderedNodes()
        {
            var list = new List<IPlatformNode>(k_Nodes.Values);
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return list;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void LoadPluginsFromAssemblies()
        {
            var nodeType = typeof(IPlatformNode);
            var hostAssemblyName = nodeType.Assembly.GetName().Name;
            var nodes = new List<IPlatformNode>();
#if UNITY_6000_5_OR_NEWER
            foreach (var assembly in CurrentAssemblies.GetLoadedAssemblies())
#else
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
#endif
            {
                if (!CouldContainPlatformNodes(assembly, hostAssemblyName))
                    continue;

                Type[] types = null;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (Exception ex)
                {
                    GraphicsTestLogger.LogWarning($"[PlatformNodeRegistry] Failed to load types from assembly '{assembly.FullName}': {ex.Message}");
                }

                if (types == null)
                    continue;

                foreach (var t in types)
                {
                    if (nodeType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                    {
                        try
                        {
                            nodes.Add((IPlatformNode)Activator.CreateInstance(t));
                        }
                        catch (Exception ex)
                        {
                            GraphicsTestLogger.LogWarning(
                                $"[PlatformNodeRegistry] Failed to instantiate '{t.FullName}': {ex.Message}");
                        }
                    }
                }
            }

            foreach (var plugin in nodes)
            {
                RegisterPlugin(plugin);
            }
        }

        static void RegisterPlugin(IPlatformNode node)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.Name))
                throw new ArgumentException($"Invalid node: {node}");

            k_Nodes[node.Name] = node;
            k_NodesByType[node.GetType()] = node;
            k_NodesByDataType[node.DataType] = node;
            k_EnumTypes.Add(node.DataType);
        }

        /// <summary>
        /// An assembly can only contain <see cref="IPlatformNode"/> implementations if it is
        /// the host assembly itself or directly references it.
        /// </summary>
        static bool CouldContainPlatformNodes(Assembly assembly, string hostAssemblyName)
        {
            if (assembly.GetName().Name == hostAssemblyName)
                return true;
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name == hostAssemblyName)
                    return true;
            }

            return false;
        }
    }
}
