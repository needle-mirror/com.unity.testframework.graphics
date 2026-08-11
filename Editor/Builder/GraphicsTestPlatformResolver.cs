using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Turns the configured platform schemata, the current build environment, per-fixture platform
    /// arguments, and command-line <see cref="PlatformCombinations"/> into the concrete list of
    /// <see cref="GraphicsTestPlatform"/>s to build reference-image content for.
    /// </summary>
    static class GraphicsTestPlatformResolver
    {
        /// <summary>
        /// Builds one platform per schema from <paramref name="currentPlatform"/> (schemata are
        /// resolved in the order given), expanded by the given fixture-argument sets and by the
        /// command-line platform combinations (the Cartesian product of all requested characteristic
        /// values, see <see cref="PlatformCombinations"/>).
        /// </summary>
        internal static IList<GraphicsTestPlatform> ResolvePlatforms(
            IReadOnlyList<PlatformSchema> schemata,
            GraphicsTestPlatform currentPlatform,
            IReadOnlyList<Enum[]> fixtureArgSets,
            PlatformCombinations combinations = null)
        {
            combinations ??= PlatformCombinations.Empty;

            var hasCombinations = !combinations.IsEmpty;
            var tuples = combinations.Expand();

            var platforms = new List<GraphicsTestPlatform>();
            foreach (var schema in schemata ?? Array.Empty<PlatformSchema>())
            {
                if (schema == null)
                    continue;

                var basePlatform = new GraphicsTestPlatform(currentPlatform, schema);
                foreach (var args in fixtureArgSets)
                {
                    var fixtureArgs = args ?? Array.Empty<Enum>();
                    var withFixtureArgs = new GraphicsTestPlatform(basePlatform, fixtureArgs);

                    // Baseline first: when every combined characteristic is a NEW dimension for this
                    // platform, the un-expanded platform is emitted too, so its bundles stay
                    // self-complete and the variants carry only their genuine overrides (the bundle
                    // builder assigns each reference image to the first platform that finds it).
                    // When a combined characteristic OVERRIDES an existing dimension, the baseline is
                    // intentionally omitted: its value was explicitly replaced from the command line.
                    if (!hasCombinations || AllCombinedTypesAreNewDimensions(withFixtureArgs, combinations))
                        AddIfMissing(platforms, withFixtureArgs);

                    if (!hasCombinations)
                        continue;

                    foreach (var tuple in tuples)
                        AddIfMissing(platforms, CreateExpandedPlatform(basePlatform, fixtureArgs, tuple));
                }
            }

            return platforms;
        }

        static void AddIfMissing(List<GraphicsTestPlatform> platforms, GraphicsTestPlatform platform)
        {
            if (!platforms.Contains(platform))
                platforms.Add(platform);
        }

        static bool AllCombinedTypesAreNewDimensions(GraphicsTestPlatform platform, PlatformCombinations combinations)
        {
            foreach (var type in combinations.Characteristics)
            {
                if (platform.Data.ContainsKey(type))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Combines <paramref name="basePlatform"/>, one fixture-argument set, and one combination tuple
        /// into a build platform. Precedence: fixture arguments keep their pre-existing semantics (they
        /// add new dimensions but never overwrite the base platform), the tuple then sets its values for
        /// every dimension the fixture did not contribute — overriding values the base platform derived
        /// from the current environment. A fixture argument that was itself ignored (because the base
        /// platform already had that dimension) does not block a tuple override for that dimension.
        /// </summary>
        internal static GraphicsTestPlatform CreateExpandedPlatform(
            GraphicsTestPlatform basePlatform,
            Enum[] fixtureArgs,
            Enum[] tuple)
        {
            fixtureArgs ??= Array.Empty<Enum>();
            var withFixtureArgs = new GraphicsTestPlatform(basePlatform, fixtureArgs);
            if (tuple == null || tuple.Length == 0)
                return withFixtureArgs;

            var fixtureContributedTypes = new HashSet<Type>();
            foreach (var arg in fixtureArgs)
            {
                if (!basePlatform.Data.ContainsKey(arg.GetType()))
                    fixtureContributedTypes.Add(arg.GetType());
            }

            var mergedValues = new Dictionary<Type, Enum>(withFixtureArgs.Data);
            var types = new List<Type>(withFixtureArgs.Schema.Types);
            foreach (var value in tuple)
            {
                var type = value.GetType();
                if (fixtureContributedTypes.Contains(type))
                    continue;

                mergedValues[type] = value;
                if (!types.Contains(type))
                    types.Add(type);
            }

            var schema = new PlatformSchema(
                withFixtureArgs.Schema.name,
                withFixtureArgs.Schema.rootPath,
                types.ToArray());
            var values = new Enum[mergedValues.Count];
            mergedValues.Values.CopyTo(values, 0);
            return new GraphicsTestPlatform(schema, values);
        }
    }
}
