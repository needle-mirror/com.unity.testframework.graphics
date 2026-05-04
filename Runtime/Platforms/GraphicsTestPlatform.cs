using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Text;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Represents the platform properties of a test environment.
    /// </summary>
    public sealed class GraphicsTestPlatform : IEquatable<GraphicsTestPlatform>
    {
        internal PlatformSchema Schema { get; }
        internal ReadOnlyDictionary<Type, Enum> Data { get; }
        readonly PlatformPath m_PlatformPath;
        string m_CachedName;

        static readonly ConcurrentDictionary<int, IList<GraphicsTestPlatform>> k_CombineCache = new();

        /// <summary>
        /// The 'Default' GraphicsTestPlatform. It <see cref="IsSupersetOf"/> all other nodes.
        /// </summary>
        public static GraphicsTestPlatform Default { get; } = new();

        /// <summary>
        /// Creates a new GraphicsTestPlatform using all available <see cref="IPlatformNode.DataType"/> enums
        /// </summary>
        /// <param name="values">
        /// The specific values for this platform.
        /// These can be of any <see cref="IPlatformNode.DataType"/> that has been declared in the project.
        /// There cannot be two values of the same type.
        /// </param>
        public GraphicsTestPlatform(params Enum[] values) : this(PlatformSchema.AllPlatformSchema, values) { }

        /// <summary>
        /// Creates a new GraphicsTestPlatform using all <see cref="IPlatformNode.DataType"/> enums present in the <paramref name="schema"/>
        /// </summary>
        /// <param name="schema">The schema to use for constructing this platform.
        /// Only values of <see cref="IPlatformNode.DataType"/> present in the schema will be added to the platform's data</param>
        /// <param name="values">
        /// The specific values for this platform.
        /// These can be of any <see cref="IPlatformNode.DataType"/> that has been declared in the project.
        /// There cannot be two values of the same type.
        /// </param>
        public GraphicsTestPlatform(PlatformSchema schema, params Enum[] values)
        {
            Schema = schema;
            Data = new ReadOnlyDictionary<Type, Enum>(CreateTypeDictionary(Schema, values));
            m_PlatformPath = PlatformPath.Construct(Schema, CopyValuesToArray(Data?.Values));
        }

        /// <summary>
        /// Creates a new GraphicsTestPlatform using the data from <paramref name="basePlatform"/> but using the schema <paramref name="schema"/>
        /// </summary>
        /// <param name="basePlatform">
        /// The platform to base the new platform on. The constructor will copy the data from the <paramref name="basePlatform"/>
        /// </param>
        /// <param name="schema">The schema to use for constructing this platform.
        /// Only values of <see cref="IPlatformNode.DataType"/> present in the schema will be added to the platform's data</param>
        public GraphicsTestPlatform(GraphicsTestPlatform basePlatform, PlatformSchema schema)
        {
            Schema = schema;
            Data = new ReadOnlyDictionary<Type, Enum>(CreateTypeDictionary(Schema, CopyValuesToArray(basePlatform.Data?.Values)));
            m_PlatformPath = PlatformPath.Construct(Schema, CopyValuesToArray(Data?.Values));
        }

        /// <summary>
        /// Creates a new GraphicsTestPlatform using the data from <paramref name="basePlatform"/> and adding data from <paramref name="values"/>
        /// </summary>
        /// <param name="basePlatform">
        /// The platform to base the new platform on. The constructor will copy the data from the <paramref name="basePlatform"/>
        /// and will create a new schema for it to add the data from <paramref name="values"/>
        /// </param>
        /// <param name="values">
        /// The specific values for this platform.
        /// These can be of any <see cref="IPlatformNode.DataType"/> that has been declared in the project.
        /// There cannot be two values of the same type.
        /// </param>
        public GraphicsTestPlatform(GraphicsTestPlatform basePlatform, params Enum[] values)
        {
            // Use the original schema types and add any new types from the values
            var types = basePlatform.Schema.Types;

            var enumValues = new List<Enum>(basePlatform.Data.Values ?? throw new InvalidOperationException());

            foreach (var v in values)
            {
                if (!types.Contains(v.GetType()))
                    types.Add(v.GetType());

                var found = false;
                foreach (var e in enumValues)
                {
                    if (e.GetType() == v.GetType())
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    enumValues.Add(v);
            }

            Schema = new PlatformSchema(
                basePlatform.Schema.name,
                basePlatform.Schema.rootPath,
                types.ToArray()
                );
            Data = new ReadOnlyDictionary<Type, Enum>(CreateTypeDictionary(Schema, enumValues.ToArray()));
            m_PlatformPath = PlatformPath.Construct(Schema, CopyValuesToArray(Data.Values));
        }

        /// <summary>
        /// Retrieves the value of the data with type <typeparamref name="T"/> if present in the platform's data.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the enum to be retrieved. For example <see cref="UnityEngine.RuntimePlatform"/>
        /// </typeparam>
        /// <returns>
        /// The <typeparamref name="T"/> value stored in the platform's data, or the default value for <typeparamref name="T"/>.
        /// </returns>
        public T GetValue<T>() where T : Enum
        {
            if (Data.TryGetValue(typeof(T), out var value))
                return (T)value;
            GraphicsTestLogger.LogError($"Failed to get value of type {typeof(T)}");
            return default;
        }

        /// <summary>
        /// Returns the directory path for the test results.
        /// </summary>
        /// <remarks>
        /// The path is constructed from the color space, platform, graphics device, and XR device.
        /// For example: Linear/WindowsEditor/Direct3D11/None
        /// </remarks>
        /// <value>
        /// The directory path for the test results.
        /// </value>
        public string ResultsPath => m_PlatformPath.m_RelativePath;

        /// <summary>
        /// All the possible results paths for this platform, ordered based on its platform schema.
        /// </summary>
        public IEnumerable<string> AllResultsPaths => m_PlatformPath.m_AllPaths;

        /// <summary>
        /// Converts the GraphicsTestPlatform to a string value.
        /// </summary>
        /// <returns>
        /// The string representation of the GraphicsTestPlatform.
        /// </returns>
        public override string ToString() => Data?.Values?.Count == 0 ? "AllPlatforms" : ResultsPath.Replace('/', '-');

        /// <summary>
        /// The name of this platform, as it will appear on test content bundle names.
        /// </summary>
        public string Name
        {
            get
            {
                if (m_CachedName != null)
                    return m_CachedName;

                if (Data?.Values?.Count == 0)
                {
                    m_CachedName = "allplatforms";
                    return m_CachedName;
                }

                var parts = ToString().Split('-');
                var truncated = new string[parts.Length];
                for (var i = 0; i < parts.Length; i++)
                    truncated[i] = parts[i].Substring(0, Math.Min(parts[i].Length, 24));
                m_CachedName = string.Join('-', truncated).ToLower();
                return m_CachedName;
            }
        }

        /// <summary>
        /// A string representation of the GraphicsTestPlatform for printing to the console.
        /// </summary>
        /// <returns>
        /// The string representation of the platform information.
        /// </returns>
        public string PrintPlatformInfo()
        {
            var maxLineLength = 0;

            var sb = new StringBuilder();
            foreach (var kvp in Data)
            {
                var platformInfo = $"{kvp.Key.Name}: {kvp.Value}";
                sb.AppendLine(platformInfo);

                maxLineLength = Math.Max(maxLineLength, platformInfo.Length);
            }

            var separator = new string('=', maxLineLength);
            sb.Insert(0, "\n" + separator + "\n");
            sb.AppendLine(separator);
            return sb.ToString();
        }

        /// <summary>
        /// Retrieves the current platform based on a specified schema.
        /// </summary>
        /// <param name="schema">
        /// The schema to use for constructing the current platform.
        /// </param>
        /// <returns>
        /// A new GraphicsTestPlatform based on the current state and the given schema.
        /// </returns>
        public static GraphicsTestPlatform GetCurrent(PlatformSchema schema)
        {
            var orderedNodes = PlatformNodeRegistry.GetOrderedNodes();
            var currents = new Enum[orderedNodes.Count];
            for (var i = 0; i < orderedNodes.Count; i++)
                currents[i] = orderedNodes[i].Current;
            return new(schema ?? PlatformSchema.AllPlatformSchema, currents);
        }

        /// <summary>
        /// Retrieves the current build platform based on a specified schema.
        /// </summary>
        /// <param name="schema">
        /// The schema to use for constructing the current build platform.
        /// </param>
        /// <returns>
        /// A new GraphicsTestPlatform based on the current build settings and the given schema.
        /// </returns>
        public static GraphicsTestPlatform GetBuildPlatform(PlatformSchema schema)
        {
            var orderedNodes = PlatformNodeRegistry.GetOrderedNodes();
            var builds = new Enum[orderedNodes.Count];
            for (var i = 0; i < orderedNodes.Count; i++)
                builds[i] = orderedNodes[i].Build;
            return new(schema ?? PlatformSchema.AllPlatformSchema, builds);
        }

        /// <summary>
        /// The current test environment. This constructs the current platform based on all available <see cref="IPlatformNode.DataType"/>
        /// </summary>
        public static GraphicsTestPlatform Current =>
            GetCurrent(PlatformSchema.AllPlatformSchema);

        /// <summary>
        /// The current build environment settings. This constructs the build platform based on all available <see cref="IPlatformNode.DataType"/>
        /// </summary>
        public static GraphicsTestPlatform PlayerBuild =>
#if !UNITY_EDITOR
            throw new InvalidOperationException();
#else
            GetBuildPlatform(PlatformSchema.AllPlatformSchema);
#endif

        /// <summary>
        /// Returns true if the GraphicsTestPlatform is an editor platform.
        /// </summary>
        public bool IsEditorPlatform =>
            GetValue<RuntimePlatform>()
                is RuntimePlatform.LinuxEditor
                    or RuntimePlatform.WindowsEditor
                    or RuntimePlatform.OSXEditor;

        /// <summary>
        /// Returns true if the GraphicsTestPlatform is a subset of the other specified GraphicsTestPlatform.
        /// </summary>
        /// <param name="other">The GraphicsTestPlatform to compare against.</param>
        /// <remarks>
        /// A GraphicsTestPlatform is a subset of another if all of its properties are equal to or more specific than the other.
        /// For example, a GraphicsTestPlatform with a specific architecture and platform is a subset of a GraphicsTestPlatform with a default architecture and platform.
        /// </remarks>
        /// <returns>
        /// True if the GraphicsTestPlatform is a subset of the other specified GraphicsTestPlatform, false otherwise.
        /// </returns>
        public bool IsSubsetOf(GraphicsTestPlatform other)
        {
            if (other == null)
                return false;
            if (ReferenceEquals(this, other))
                return false;
            if (Data.Count <= other.Data.Count)
                return false;
            if (other.Data.Keys == null)
                return true;
            foreach (var k in other.Data.Keys)
            {
                if (!Data.TryGetValue(k, out var dataVal) || !other.Data[k].Equals(dataVal))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns true if the GraphicsTestPlatform is a superset of the other specified GraphicsTestPlatform.
        /// </summary>
        /// <param name="other">The GraphicsTestPlatform to compare against.</param>
        /// <remarks>
        /// A GraphicsTestPlatform is a superset of another if all of its properties are equal to or less specific than the other.
        /// For example, a GraphicsTestPlatform with a default architecture and platform is a superset of a GraphicsTestPlatform with a specific architecture and platform.
        /// </remarks>
        /// <returns>
        /// True if the GraphicsTestPlatform is a superset of the other specified GraphicsTestPlatform, false otherwise.
        /// </returns>
        public bool IsSupersetOf(GraphicsTestPlatform other) => other.IsSubsetOf(this);

        /// <summary>
        /// Compares two GraphicsTestPlatform to see if the left is a subset of the right.
        /// </summary>
        /// <param name="left">
        /// The left GraphicsTestPlatform to compare.
        /// </param>
        /// <param name="right">
        /// The right GraphicsTestPlatform to compare.
        /// </param>
        /// <returns>
        /// True if the left GraphicsTestPlatform is a subset of the right GraphicsTestPlatform or if they are equal.
        /// </returns>
        public static bool operator <=(GraphicsTestPlatform left, GraphicsTestPlatform right) =>
            left.IsSubsetOf(right) || left.Equals(right);

        /// <summary>
        /// Compares two GraphicsTestPlatform to see if the left is a superset of the right.
        /// </summary>
        /// <param name="left">
        /// The left GraphicsTestPlatform to compare.
        /// </param>
        /// <param name="right">
        /// The right GraphicsTestPlatform to compare.
        /// </param>
        /// <returns>
        /// True if the left GraphicsTestPlatform is a superset of the right GraphicsTestPlatform or if they are equal.
        /// </returns>
        public static bool operator >=(GraphicsTestPlatform left, GraphicsTestPlatform right) =>
            left.IsSupersetOf(right) || left.Equals(right);

        /// <summary>
        /// Compares two GraphicsTestPlatform to see if the left is a subset of the right.
        /// </summary>
        /// <param name="left">
        /// The left GraphicsTestPlatform to compare.
        /// </param>
        /// <param name="right">
        /// The right GraphicsTestPlatform to compare.
        /// </param>
        /// <returns>
        /// True if the left GraphicsTestPlatform is a subset of the right GraphicsTestPlatform and not equal.
        /// </returns>
        public static bool operator <(GraphicsTestPlatform left, GraphicsTestPlatform right) =>
            left.IsSubsetOf(right);

        /// <summary>
        /// Compares two GraphicsTestPlatform to see if the left is a superset of the right.
        /// </summary>
        /// <param name="left">
        /// The left GraphicsTestPlatform to compare.
        /// </param>
        /// <param name="right">
        /// The right GraphicsTestPlatform to compare.
        /// </param>
        /// <returns>
        /// True if the left GraphicsTestPlatform is a superset of the right GraphicsTestPlatform and not equal.
        /// </returns>
        public static bool operator >(GraphicsTestPlatform left, GraphicsTestPlatform right) =>
            left.IsSupersetOf(right);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)"/>
        public bool Equals(GraphicsTestPlatform other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (Data.Count != other.Data.Count)
                return false;
            foreach (var k in Data.Keys)
            {
                if (!other.Data.TryGetValue(k, out var otherVal) || !Data[k].Equals(otherVal))
                    return false;
            }
            return true;
        }

        /// <inheritdoc cref="Object.Equals(object)"/>
        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is GraphicsTestPlatform other && Equals(other);
        }

        /// <inheritdoc cref="op_Equality"/>
        public static bool operator ==(GraphicsTestPlatform x, GraphicsTestPlatform y)
        {
            return x?.Equals(y) ?? ReferenceEquals(y, null);
        }

        /// <inheritdoc cref="op_Inequality"/>
        public static bool operator !=(GraphicsTestPlatform x, GraphicsTestPlatform y)
        {
            return !(x?.Equals(y) ?? ReferenceEquals(y, null));
        }

        /// <inheritdoc cref="Object.GetHashCode"/>
        public override int GetHashCode()
        {
            var hashCode = 0;
            foreach (var kvp in Data)
                hashCode ^= kvp.Value.GetHashCode();
            return hashCode;
        }

        internal static IList<GraphicsTestPlatform> Combine(List<Enum> values)
        {
            if (values == null || values.Count == 0)
                return new List<GraphicsTestPlatform> { Default };

            // Compute cache key from values
            var cacheKey = ComputeCacheKey(values);
            if (k_CombineCache.TryGetValue(cacheKey, out var cached))
                return cached;

            // Build groups array, filtering nulls and grouping by type in a single pass
            var groupDict = new Dictionary<Type, List<Enum>>();
            foreach (var v in values)
            {
                if (v == null)
                    continue;

                var type = v.GetType();
                if (!groupDict.TryGetValue(type, out var list))
                {
                    list = new List<Enum>();
                    groupDict[type] = list;
                }
                list.Add(v);
            }

            if (groupDict.Count == 0)
            {
                var defaultResult = new List<GraphicsTestPlatform> { Default };
                k_CombineCache.TryAdd(cacheKey, defaultResult);
                return defaultResult;
            }

            var sortedTypes = new List<Type>(groupDict.Keys);
            sortedTypes.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            var groups = new List<Enum>[sortedTypes.Count];
            for (var groupIndex = 0; groupIndex < sortedTypes.Count; groupIndex++)
                groups[groupIndex] = groupDict[sortedTypes[groupIndex]];

            // Calculate total combinations and pre-allocate
            var totalCombinations = 1;
            foreach (var group in groups)
                totalCombinations *= group.Count;

            var results = new List<GraphicsTestPlatform>(totalCombinations);

            // Generate combinations iteratively using index arithmetic
            var current = new Enum[groups.Length];
            for (var i = 0; i < totalCombinations; i++)
            {
                var remainder = i;
                for (var g = groups.Length - 1; g >= 0; g--)
                {
                    var groupSize = groups[g].Count;
                    current[g] = groups[g][remainder % groupSize];
                    remainder /= groupSize;
                }
                results.Add(new GraphicsTestPlatform(current));
            }

            k_CombineCache.TryAdd(cacheKey, results);
            return results;
        }

        static int ComputeCacheKey(List<Enum> values)
        {
            // Use XOR for order-independent hashing - same values in any order produce the same key
            var hash = 0;
            foreach (var v in values)
            {
                if (v == null)
                    continue;
                // XOR is commutative, so order doesn't matter
                hash ^= HashCode.Combine(v.GetType(), v);
            }
            return hash;
        }

        static Enum[] CopyValuesToArray(IEnumerable<Enum> values)
        {
            if (values == null)
                return Array.Empty<Enum>();
            var list = new List<Enum>();
            foreach (var v in values)
                list.Add(v);
            return list.Count > 0 ? list.ToArray() : Array.Empty<Enum>();
        }

        Dictionary<Type, Enum> CreateTypeDictionary(PlatformSchema schema, params Enum[] values)
        {
            var dict = new Dictionary<Type, Enum>();
            if (values is not { Length: > 0 })
                return dict;

            foreach (var v in values)
            {
                if (v == null)
                    continue;

                var valueType = v.GetType();

                // Architecture is not part of PlatformSchema.Types because it was added after
                // the schema system was designed. It must be included manually so that
                // PlatformPath can assemble paths containing architecture segments.
                // TODO: Add Architecture as a first-class schema node to remove this special case.
                if (!schema.Types.Contains(valueType) && valueType != typeof(Architecture))
                    continue;

                if (!dict.TryAdd(valueType, v))
                    throw new ArgumentException(
                        $"A platform cannot have multiple values of the same type {valueType}. Tried to add {v} but {dict[valueType]} was already present."
                    );
            }

            return dict;
        }
    }
}
