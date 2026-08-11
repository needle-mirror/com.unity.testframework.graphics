using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A request to build reference-image content for several values of one or more platform
    /// characteristics in a single build (for example GraphicsVendor: [AMD, Nvidia]), as parsed from
    /// the <c>-combine&lt;Node&gt;s</c> command-line arguments by <see cref="PlatformCombinationCliReader"/>.
    /// <see cref="Expand"/> turns the per-characteristic value lists into the concrete tuples of their
    /// Cartesian product, one value per characteristic.
    /// </summary>
    class PlatformCombinations
    {
        readonly IReadOnlyDictionary<Type, IReadOnlyList<Enum>> m_ValuesByCharacteristic;

        /// <summary>An empty request: no characteristics are combined.</summary>
        internal static PlatformCombinations Empty { get; } = new(null);

        internal PlatformCombinations(IReadOnlyDictionary<Type, IReadOnlyList<Enum>> valuesByCharacteristic)
        {
            m_ValuesByCharacteristic =
                valuesByCharacteristic ?? new Dictionary<Type, IReadOnlyList<Enum>>();
        }

        /// <summary>Whether no characteristic values were requested (a build with no combinations).</summary>
        internal bool IsEmpty => m_ValuesByCharacteristic.Count == 0;

        /// <summary>The characteristic types (enum types) whose values are being combined.</summary>
        internal IEnumerable<Type> Characteristics => m_ValuesByCharacteristic.Keys;

        /// <summary>Whether values for <paramref name="characteristic"/> were requested.</summary>
        internal bool Includes(Type characteristic) => m_ValuesByCharacteristic.ContainsKey(characteristic);

        /// <summary>
        /// The requested values for <paramref name="characteristic"/>, in the order they appeared on the
        /// command line; empty when none were requested for it.
        /// </summary>
        internal IReadOnlyList<Enum> ValuesFor(Type characteristic) =>
            m_ValuesByCharacteristic.TryGetValue(characteristic, out var values) ? values : Array.Empty<Enum>();

        /// <summary>
        /// Expands the per-characteristic value lists into the tuples of their Cartesian product, for
        /// example {Vendor: [A, B], XrDevice: [None, Quest]} yields [A+None, A+Quest, B+None, B+Quest].
        /// Each tuple carries one value per characteristic. Characteristics are combined in a
        /// deterministic order (sorted by full type name). Returns a single empty tuple when there are
        /// no combinations, so callers expand to exactly the unmodified platform.
        /// </summary>
        internal IReadOnlyList<Enum[]> Expand()
        {
            var tuples = new List<Enum[]> { Array.Empty<Enum>() };
            if (IsEmpty)
                return tuples;

            var orderedTypes = new List<Type>(m_ValuesByCharacteristic.Keys);
            orderedTypes.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));

            foreach (var type in orderedTypes)
            {
                var values = m_ValuesByCharacteristic[type];
                var expanded = new List<Enum[]>(tuples.Count * values.Count);
                foreach (var tuple in tuples)
                {
                    foreach (var value in values)
                    {
                        var next = new Enum[tuple.Length + 1];
                        Array.Copy(tuple, next, tuple.Length);
                        next[tuple.Length] = value;
                        expanded.Add(next);
                    }
                }
                tuples = expanded;
            }

            return tuples;
        }
    }
}
