using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    class CommandLineReader
    {
        readonly Dictionary<string, string> m_ArgumentCache;
        readonly ICommandLineProvider m_CommandLineProvider;

        internal CommandLineReader(ICommandLineProvider commandLineProvider)
        {
            m_CommandLineProvider = commandLineProvider;
            m_ArgumentCache = new Dictionary<string, string>();
        }

        internal CommandLineReader()
            : this(new EnvironmentCommandLineReader()) { }

        /// <summary>
        /// Reads <paramref name="argName"/> from the command line, distinguishing the three states a flag
        /// can be in: absent (returns <c>false</c>), present without a value (returns <c>true</c> with an
        /// empty <paramref name="value"/>), and present with a value (returns <c>true</c> with the trimmed
        /// value). A bare <c>-flag</c>, a trailing <c>-flag=</c>, or a flag that is the last argument all
        /// count as "present without a value".
        /// </summary>
        /// <param name="argName">The CLI argument to read.</param>
        /// <param name="value">The trimmed value when present, or <see cref="string.Empty"/> when the flag
        /// is present without one. Set to <c>null</c> when the flag is absent.</param>
        /// <returns><c>true</c> if the flag was present on the command line, <c>false</c> otherwise.</returns>
        internal bool TryGetArgumentValue(string argName, out string value)
        {
            // FindCommandLineArgument returns null only for "absent", so a non-null result means present
            // even when the value is empty. Deriving both presence and value from the same lookup keeps
            // them from ever disagreeing, with a single cache as the source of truth.
            value = FindCommandLineArgument(argName);
            return value != null;
        }

        internal bool CommandLineArgumentExists(string argName) =>
            FindCommandLineArgument(argName) != null;

        /// <summary>
        /// Returns the trimmed value of <paramref name="argName"/>, <see cref="string.Empty"/> when the
        /// flag is present but has no value, or <c>null</c> when the flag is absent. Results are memoized.
        /// </summary>
        internal string FindCommandLineArgument(string argName)
        {
            if (m_ArgumentCache.TryGetValue(argName, out var value))
                return value;

            value = FindCommandLineArgument(m_CommandLineProvider.GetCommandLineArgs(), argName);
            m_ArgumentCache.Add(argName, value);
            return value;
        }

        internal bool HasMultipleArgumentsThatMatch(string filter)
        {
            var count = 0;
            foreach (var m in m_CommandLineProvider.GetCommandLineArgs())
            {
                if (m.Contains(filter, StringComparison.InvariantCultureIgnoreCase))
                {
                    count++;
                    if (count > 1)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Turns <paramref name="field"/> on if <paramref name="argName"/> is present on the command line.
        /// Uses logical-OR semantics: a missing flag never clears an already-enabled value, so this is
        /// the right helper for opt-in feature toggles whose persisted state can be additionally enabled
        /// at runtime via CLI.
        /// </summary>
        /// <param name="field">The field to OR the CLI presence into.</param>
        /// <param name="argName">The CLI flag to look for (e.g. <c>-my-flag</c>).</param>
        /// <returns><c>true</c> if the flag was present on the command line, <c>false</c> otherwise.</returns>
        internal bool SetFlagIfPresent(ref bool field, string argName)
        {
            var present = CommandLineArgumentExists(argName);
            if (present && !field)
                GraphicsTestLogger.Log(
                    LogType.Log,
                    $"CommandLineReader: command-line flag '{argName}' is present; enabling the corresponding setting."
                );
            field |= present;
            return present;
        }

        /// <summary>
        /// If <paramref name="argName"/> is present on the command line with a non-empty value, parses that
        /// value via <paramref name="parser"/> and assigns the result to <paramref name="field"/>. Leaves
        /// <paramref name="field"/> untouched when the argument is missing or has an empty value. Any
        /// exception thrown by <paramref name="parser"/> is logged via <see cref="GraphicsTestLogger"/>
        /// and rethrown so callers fail fast on malformed CLI input.
        /// </summary>
        /// <typeparam name="T">The type produced by <paramref name="parser"/>.</typeparam>
        /// <param name="field">The destination field, written only on a successful parse.</param>
        /// <param name="argName">The CLI argument to read (supports <c>-arg=value</c> and <c>-arg value</c>).</param>
        /// <param name="parser">A function that converts the raw CLI string to <typeparamref name="T"/>.</param>
        /// <returns><c>true</c> if the field was updated, <c>false</c> if the argument was absent or empty.</returns>
        internal bool UpdateFromArgument<T>(ref T field, string argName, Func<string, T> parser)
        {
            var raw = FindCommandLineArgument(argName);
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            try
            {
                var parsed = parser(raw);
                if (!EqualityComparer<T>.Default.Equals(field, parsed))
                    GraphicsTestLogger.Log(
                        LogType.Log,
                        $"CommandLineReader: applying command-line argument '{argName}' with value '{parsed}'."
                    );
                field = parsed;
                return true;
            }
            catch (Exception e)
            {
                GraphicsTestLogger.LogException(e);
                throw;
            }
        }

        // Returns null when argName is absent, so callers can tell that apart from a flag that is present
        // but carries no value (string.Empty).
        static string FindCommandLineArgument(string[] args, string argName)
        {
            string argValue;

            if (args == null || args.Length == 0)
                return null;

            var filterArg = Array.Find(args, arg =>
                arg.Equals(argName, StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith($"{argName}=", StringComparison.OrdinalIgnoreCase));

            if (filterArg == null)
                return null;

            if (filterArg.Contains("=") && !filterArg.EndsWith('='))
            {
                argValue = filterArg.Split('=', 2)[1];
            }
            else
            {
                var index = Array.IndexOf(args, filterArg);
                if (index < 0 || index == args.Length - 1) // present, but no value follows it
                {
                    return string.Empty;
                }

                argValue = args[index + 1];
            }

            return argValue.Trim();
        }
    }
}
