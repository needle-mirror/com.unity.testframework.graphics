using System;
using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor.TestTools.Graphics
{
    /// <summary>
    /// Represents metrics for reference images used in graphics tests.
    /// </summary>
    [Serializable]
    public record ReferenceImageMetrics
    {
        /// <summary>
        /// The number of nodes for which reference images were compared. This includes the 'base' platform and any additional nodes that have reference images.
        /// </summary>
        public int PlatformCount { get; init; }

        /// <summary>
        /// The accumulated divergence across all nodes.
        /// This value represents the average divergence of reference images compared to the best match.
        /// A lower value indicates better optimization, as it means the reference images are more consistent across nodes.
        /// A value of 0 indicates that all reference images are identical across nodes.
        /// </summary>
        public double AccumulatedDivergence { get; init; }

        /// <inheritdoc/>
        public override string ToString() =>
            $"{AccumulatedDivergence.ToString(CultureInfo.InvariantCulture)}:{PlatformCount.ToString(CultureInfo.InvariantCulture)}";

        internal static ReferenceImageMetrics FromString(string serialized)
        {
            var deconstructed = serialized.Split(":");
            if (deconstructed.Length != 2)
                throw new ArgumentException("Malformed serialized object: " + serialized, nameof(serialized));

            return new ReferenceImageMetrics
            {
                AccumulatedDivergence = double.Parse(deconstructed[0], CultureInfo.InvariantCulture),
                PlatformCount = int.Parse(deconstructed[1], CultureInfo.InvariantCulture),
            };
        }

        internal static IDictionary<string, string> ToSerializedDictionary(
            IDictionary<string, ReferenceImageMetrics> cache
        )
        {
            if (cache == null)
                return null; // null-in, null-out by design for serialization round-trips
            var result = new Dictionary<string, string>();
            foreach (var kvp in cache)
                result.Add(kvp.Key, kvp.Value.ToString());
            return result;
        }

        internal static IDictionary<string, ReferenceImageMetrics> FromSerializedDictionary(
            IDictionary<string, string> cache
        )
        {
            if (cache == null)
                return null; // null-in, null-out by design for serialization round-trips
            var result = new Dictionary<string, ReferenceImageMetrics>();
            foreach (var kvp in cache)
                result.Add(kvp.Key, FromString(kvp.Value));
            return result;
        }
    }
}
