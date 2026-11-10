using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// A platform schema for use in constructing <see cref="GraphicsTestPlatform"/> objects.
    /// </summary>
    [Serializable]
    public class PlatformSchema : ISerializationCallbackReceiver
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

        /// <summary>Shared templates: <see cref="Clone"/> before serializing or mutating.</summary>
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

        /// <summary>Returns an independent copy of this schema, safe to mutate and serialize.</summary>
        internal PlatformSchema Clone() => new(name, rootPath, Types.ToArray());

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
                // HACK for CoreCLR compatibility: System.Runtime.InteropServices.Architecture moved between
                // assemblies in the BCL, so the assembly-qualified name stored in the schema no longer resolves.
                if ((type == null) && s.StartsWith("System.Runtime.InteropServices.Architecture,"))
                    type = typeof(Architecture);

                if (type != null)
                    yield return type;
                else
                    GraphicsTestLogger.LogError($"Invalid type in platform schema {name}: {s}");
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
                // Preserve already-resolved Types as a fallback so serialization does not
                // silently drop entries whose IPlatformNode isn't currently registered with
                // PlatformNodeRegistry. Registry-provided types still take precedence so
                // edits to nodes[] remain authoritative.
                var existingByName = new Dictionary<string, Type>(Types?.Count ?? 0);
                if (Types != null)
                {
                    foreach (var t in Types)
                    {
                        if (t != null)
                            existingByName[t.Name] = t;
                    }
                }

                var rebuilt = new List<Type>(nodes.Length);
                foreach (var node in nodes)
                {
                    if (PlatformNodeRegistry.k_Nodes.TryGetValue(node, out var nodeType))
                        rebuilt.Add(nodeType.DataType);
                    else if (existingByName.TryGetValue(node, out var preserved))
                        rebuilt.Add(preserved);
                }
                Types = rebuilt;
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

        /// <summary>
        /// Whether both schemata read reference images from the same place: same <see cref="rootPath"/>
        /// and node sequence. <see cref="name"/> is ignored because it does not affect resolution.
        /// </summary>
        internal bool ResolvesToSamePathsAs(PlatformSchema other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (rootPath != other.rootPath) return false;

            var nodesA = nodes ?? Array.Empty<string>();
            var nodesB = other.nodes ?? Array.Empty<string>();
            if (nodesA.Length != nodesB.Length) return false;
            for (var i = 0; i < nodesA.Length; i++)
            {
                if (nodesA[i] != nodesB[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// How many of this schema's <see cref="nodes"/> constrain which runs an image applies to.
        /// Contexts do not: a fixture appends its context to every schema that does not declare one,
        /// so counting them would let the fixture decide how a schema ranks. A schema left with
        /// nothing but contexts still matches every run.
        /// </summary>
        internal int ConstrainingNodeCount
        {
            get
            {
                var count = 0;
                foreach (var node in nodes ?? Array.Empty<string>())
                {
                    // An unregistered node counts: a schema must not be demoted because one of its
                    // dimensions failed to register.
                    if (!PlatformNodeRegistry.k_Nodes.TryGetValue(node, out var registered)
                        || registered.Kind != PlatformDimensionKind.Context)
                        count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Whether any of this schema's <see cref="nodes"/> identifies the machine rather than
        /// describing how the run was configured.
        /// </summary>
        internal bool HasIdentityDimension
        {
            get
            {
                foreach (var node in nodes ?? Array.Empty<string>())
                {
                    if (PlatformNodeRegistry.k_Nodes.TryGetValue(node, out var registered)
                        && registered.Kind == PlatformDimensionKind.Identity)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Whether every identity dimension this schema declares actually identifies the current
        /// machine. A dimension reporting a value marked <see cref="NotAnIdentityAttribute"/> names
        /// nothing, and a schema whose identity is absent resolves the same folders as one that
        /// never declared it.
        /// </summary>
        internal bool IdentityIsSatisfied()
        {
            foreach (var node in nodes ?? Array.Empty<string>())
            {
                if (!PlatformNodeRegistry.k_Nodes.TryGetValue(node, out var registered)
                    || registered.Kind != PlatformDimensionKind.Identity)
                    continue;

                // Build rather than Current: in the Editor the device under test is named by the
                // test settings, not by the machine running the build.
                var value = registered.Build;
                if (value == null || IsMarkedNotAnIdentity(value))
                    return false;
            }
            return true;
        }

        static readonly ConcurrentDictionary<Enum, bool> s_NotAnIdentityCache = new();

        /// <summary>
        /// Whether <paramref name="value"/> carries <see cref="NotAnIdentityAttribute"/>, meaning it
        /// names no machine even though its dimension is an <see cref="PlatformDimensionKind.Identity"/>.
        /// Cached because it is reflected per schema, per platform, on every build.
        /// </summary>
        static bool IsMarkedNotAnIdentity(Enum value)
        {
            return s_NotAnIdentityCache.GetOrAdd(value, v =>
            {
                var field = v.GetType().GetField(v.ToString());
                return field != null
                    && field.IsDefined(typeof(NotAnIdentityAttribute), inherit: false);
            });
        }

        /// <summary>
        /// Orders schemata for first-match-wins reference-image resolution: schemata that identify
        /// this machine, then configuration schemata, then schemata whose identity dimension turned
        /// out to be absent, then schemata that constrain nothing. Within each group the order is
        /// <see cref="CompareForResolution"/>, and no arrangement of schemata is an error.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An image filed under a device folder was captured for that device, so it must win over a
        /// generic one however many dimensions the generic schema happens to constrain. A schema
        /// whose identity is absent has lost the only reason to outrank a configuration schema, so
        /// it sorts below them and its folders are read as a plain fallback.
        /// </para>
        /// <para>
        /// A schema with no nodes matches every run, so it is the universal fallback and sorts last
        /// whatever else is present. The asset bundle builder reads the final schema that way.
        /// </para>
        /// <para>
        /// The result is a function of the schemata alone, so it does not depend on where they came
        /// from or on the order they were discovered in. Schemata contributed by a package are
        /// ordered by the same rule as the project's own.
        /// </para>
        /// </remarks>
        internal static List<PlatformSchema> OrderForResolution(IEnumerable<PlatformSchema> schemata)
        {
            var identifiesThisMachine = new List<PlatformSchema>();
            var configuration = new List<PlatformSchema>();
            var identityAbsent = new List<PlatformSchema>();
            var universalFallback = new List<PlatformSchema>();

            foreach (var schema in schemata)
            {
                if (schema == null)
                    continue;

                if (schema.ConstrainingNodeCount == 0)
                    universalFallback.Add(schema);
                else if (!schema.HasIdentityDimension)
                    configuration.Add(schema);
                else if (schema.IdentityIsSatisfied())
                    identifiesThisMachine.Add(schema);
                else
                    identityAbsent.Add(schema);
            }

            identifiesThisMachine.Sort(CompareForResolution);
            configuration.Sort(CompareForResolution);
            identityAbsent.Sort(CompareForResolution);
            universalFallback.Sort(CompareForResolution);

            var ordered = new List<PlatformSchema>(
                identifiesThisMachine.Count + configuration.Count
                    + identityAbsent.Count + universalFallback.Count);
            ordered.AddRange(identifiesThisMachine);
            ordered.AddRange(configuration);
            ordered.AddRange(identityAbsent);
            ordered.AddRange(universalFallback);
            return ordered;
        }

        /// <summary>
        /// Orders two schemata of the same group, most constrained first.
        /// </summary>
        /// <remarks>
        /// Every node a schema declares is one more thing the run had to match for its image to
        /// apply, so the schema declaring more of them describes the run more exactly and is
        /// searched first. The remaining keys only separate schemata that constrain the same
        /// number of dimensions and would otherwise resolve in an arbitrary order. Name comes
        /// before root path deliberately: renaming a schema is the one way to change resolution
        /// order without moving reference images.
        /// </remarks>
        static int CompareForResolution(PlatformSchema a, PlatformSchema b)
        {
            var byNodeCount = b.ConstrainingNodeCount.CompareTo(a.ConstrainingNodeCount);
            if (byNodeCount != 0)
                return byNodeCount;

            var byName = string.Compare(a.name, b.name, StringComparison.Ordinal);
            if (byName != 0)
                return byName;

            var byRootPath = string.Compare(a.rootPath, b.rootPath, StringComparison.Ordinal);
            if (byRootPath != 0)
                return byRootPath;

            // The same dimensions nested in a different order read different folders, so two
            // schemata alike in every other key still resolve different images.
            return string.Compare(
                string.Join(",", a.nodes ?? Array.Empty<string>()),
                string.Join(",", b.nodes ?? Array.Empty<string>()),
                StringComparison.Ordinal);
        }
    }
}
