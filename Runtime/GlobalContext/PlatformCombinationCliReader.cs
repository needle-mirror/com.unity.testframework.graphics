using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Reads the derived <c>-combine&lt;Node&gt;s</c> command-line arguments that request reference-image
    /// content to be built for multiple values of a platform characteristic in a single build, e.g.
    /// <c>-combineGraphicsVendors=AMD,Nvidia</c>. One argument exists per registered
    /// <see cref="IPlatformNode"/>, derived from the node's <see cref="IPlatformNode.Name"/>.
    /// Parsing is fail-fast: an unknown value, an empty value, or an unrecognized <c>-combine...</c>
    /// argument throws so a misconfigured build fails instead of silently building the wrong content.
    /// </summary>
    class PlatformCombinationCliReader
    {
        internal const string k_ArgumentPrefix = "-combine";

        readonly CommandLineReader m_Reader;

        internal PlatformCombinationCliReader()
            : this(RuntimeSettings.CommandLineReader) { }

        internal PlatformCombinationCliReader(CommandLineReader reader) => m_Reader = reader;

        /// <summary>
        /// The derived argument name for <paramref name="node"/>, e.g. the GraphicsVendor node maps to
        /// <c>-combineGraphicsVendors</c>. Argument matching is case-insensitive.
        /// </summary>
        internal static string GetArgumentName(IPlatformNode node) => $"{k_ArgumentPrefix}{node.Name}s";

        /// <summary>
        /// Reads every registered node's combine argument from the command line.
        /// </summary>
        /// <returns>
        /// The requested values per platform characteristic; <see cref="PlatformCombinations.IsEmpty"/>
        /// when no combine argument is present. Values are deduplicated by enum value (aliases collapse)
        /// and keep their CLI order.
        /// </returns>
        internal PlatformCombinations ReadCombinations()
        {
            var nodes = PlatformNodeRegistry.GetOrderedNodes();
            ValidateNoUnknownCombineArguments(nodes);

            var combinations = new Dictionary<Type, IReadOnlyList<Enum>>();
            foreach (var node in nodes)
            {
                var argName = GetArgumentName(node);
                if (!m_Reader.TryGetArgumentValue(argName, out var raw))
                    continue;

                if (string.IsNullOrWhiteSpace(raw))
                    throw new ArgumentException(
                        $"'{argName}' was passed without a value. Expected a comma-separated list of "
                        + $"{node.Name} values, e.g. {argName}={string.Join(",", Enum.GetNames(node.DataType))}."
                    );

                var values = ParseValues(node.DataType, argName, raw);
                combinations[node.DataType] = values;
                GraphicsTestLogger.Log(
                    LogType.Log,
                    $"PlatformCombinationCliReader: '{argName}' requests building for {node.Name} "
                        + $"values: {string.Join(", ", values)}."
                );
            }

            return new PlatformCombinations(combinations);
        }

        static IReadOnlyList<Enum> ParseValues(Type dataType, string argName, string raw)
        {
            var values = new List<Enum>();
            foreach (var rawToken in raw.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                var token = rawToken.Trim();
                if (token.Length == 0)
                    continue;

                object parsed;
                try
                {
                    parsed = Enum.Parse(dataType, token, ignoreCase: true);
                }
                catch (Exception e) when (e is ArgumentException or OverflowException)
                {
                    parsed = null;
                }

                // IsDefined rejects tokens Parse accepts but that aren't named members (bare integers).
                if (parsed == null || !Enum.IsDefined(dataType, parsed))
                    throw new ArgumentException(
                        $"'{argName}': unknown {dataType.Name} value '{token}'. "
                            + $"Valid values: {string.Join(", ", Enum.GetNames(dataType))}."
                    );

                // Enum.Equals compares by underlying value, so aliases (e.g. values sharing an id)
                // collapse to whichever name was passed first.
                var value = (Enum)parsed;
                if (!values.Contains(value))
                    values.Add(value);
            }

            if (values.Count == 0)
                throw new ArgumentException(
                    $"'{argName}' was passed without any parsable values. Expected a comma-separated "
                        + $"list of {dataType.Name} values, e.g. {argName}={string.Join(",", Enum.GetNames(dataType))}."
                );

            return values;
        }

        /// <summary>
        /// Fails fast when the command line carries a <c>-combine...</c> argument that doesn't match any
        /// registered node's derived argument name — a typo would otherwise be a silent no-op.
        /// </summary>
        void ValidateNoUnknownCombineArguments(IReadOnlyList<IPlatformNode> nodes)
        {
            var knownNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in nodes)
                knownNames.Add(GetArgumentName(node));

            foreach (var argName in m_Reader.GetArgumentNamesWithPrefix(k_ArgumentPrefix))
            {
                if (!knownNames.Contains(argName))
                    throw new ArgumentException(
                        $"Unrecognized platform combination argument '{argName}'. "
                            + $"Valid arguments: {string.Join(", ", knownNames)}."
                    );
            }
        }
    }
}
