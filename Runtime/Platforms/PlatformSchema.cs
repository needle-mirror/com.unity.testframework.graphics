using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// A platform schema for use in constructing <see cref="GraphicsTestPlatform"/> objects.
    /// </summary>
    [Serializable]
    public class PlatformSchema : ISerializationCallbackReceiver, IEquatable<PlatformSchema>
    {
        /// <summary>
        /// The name of the schema.
        /// This will be used to identify nodes and bundles built using the schema.
        /// </summary>
        public string name;

        /// <summary>
        /// The root path of the schema. Should be relative to the project root.
        /// This will be used to read and write test assets when using the schema.
        /// </summary>
        public string rootPath;

        /// <summary>
        /// The platform nodes that will be used to generate <see cref="GraphicsTestPlatform"/> objects
        /// These should be provided in order as they will dictate the directory structure for test assets.
        /// </summary>
        public string[] nodes;

        [SerializeField]
        [HideInInspector]
        string typeString;


        internal bool hasInvalidNodeNames
        {
            get
            {
                if (nodes == null) return false;
                foreach (var n in nodes)
                {
                    if (!PlatformNodeRegistry.k_Nodes.ContainsKey(n))
                        return true;
                }
                return false;
            }
        }

        internal List<Type> Types { get; set; }

        internal PlatformSchema(string name, string rootPath, params Type[] types)
        {
            this.name = name;
            this.rootPath = rootPath;
            Types = new List<Type>(types);
            nodes = new string[Types.Count];
            for (var i = 0; i < Types.Count; i++)
                nodes[i] = Types[i].Name;
            typeString = GetTypeNameString();
        }

        internal static PlatformSchema AllPlatformSchema
        {
            get
            {
                var arr = new Type[PlatformNodeRegistry.k_EnumTypes.Count];
                var i = 0;
                foreach (var t in PlatformNodeRegistry.k_EnumTypes)
                    arr[i++] = t;
                return new("AllPlatformSchema", string.Empty, arr);
            }
        }

        internal const string k_DefaultReferenceImagesRoot = "Assets/ReferenceImages";
        internal const string k_DefaultReferenceImagesBaseRoot = "Assets/ReferenceImagesBase";

        internal static readonly PlatformSchema k_DefaultSchema = new(
            "Default",
            k_DefaultReferenceImagesRoot,
            typeof(ColorSpace),
            typeof(RuntimePlatform),
            typeof(Architecture),
            typeof(GraphicsDeviceType),
            typeof(XrDevice)
        );

        internal static readonly PlatformSchema k_DefaultSchemaBase = new(
            "Default Base",
            k_DefaultReferenceImagesBaseRoot
        );

        internal string GetTypeNameString()
        {
            if (Types == null || Types.Count == 0)
                return string.Empty;

            var parts = new List<string>(Types.Count);
            foreach (var t in Types)
                parts.Add(t.AssemblyQualifiedName);
            return string.Join(';', parts);
        }

        IEnumerable<Type> GetTypes(string typeNames)
        {
            var split = typeNames.Split(';');
            foreach (var s in split)
            {
                var type = Type.GetType(s);
                if (type != null)
                    yield return type;
                else
                    GraphicsTestLogger.LogError("Invalid type in platform schema \"{this.name}\": ${s}");
            }
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnBeforeSerialize"/>
        public void OnBeforeSerialize()
        {
            if (nodes == null || nodes.Length == 0)
            {
                typeString = string.Empty;
            }
            else
            {
                Types ??= new List<Type>();
                Types.Clear();
                foreach (var node in nodes)
                {
                    if (PlatformNodeRegistry.k_Nodes.TryGetValue(node, out var nodeType))
                    {
                        Types.Add(nodeType.DataType);
                    }
                }
                typeString = GetTypeNameString();
            }
            rootPath = (rootPath ?? string.Empty).SanitizeBackslashes().Trim('/');
        }

        /// <inheritdoc cref="ISerializationCallbackReceiver.OnAfterDeserialize"/>
        public void OnAfterDeserialize()
        {
            if (string.IsNullOrEmpty(typeString))
                Types = new List<Type>();
            else
            {
                Types = new List<Type>();
                foreach (var t in GetTypes(typeString))
                    Types.Add(t);
            }
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(PlatformSchema other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.nodes != null && ((nodes == null) != (other.nodes == null) || (nodes != null && nodes.Length != other.nodes.Length)))
                return false;
            if (nodes == null)
                return name.Equals(other.name) && rootPath.Equals(other.rootPath);
            for (var i = 0; i < nodes.Length; i++)
            {
                if (other.nodes != null && nodes[i] != other.nodes[i])
                    return false;
            }
            return name.Equals(other.name) && rootPath.Equals(other.rootPath);
        }

        internal class SchemaEqualityComparer : IEqualityComparer<PlatformSchema>
        {
            public bool Equals(PlatformSchema x, PlatformSchema y)
            {
                return x?.Equals(y) ?? y == null;
            }

            public int GetHashCode(PlatformSchema obj)
            {
                return HashCode.Combine(obj.name, obj.rootPath, string.Join(";", obj.nodes));
            }
        }
    }
}
