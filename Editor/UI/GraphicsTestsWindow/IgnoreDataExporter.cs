using System;
using System.Collections.Generic;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    /// <summary>
    /// Generates CSV exports of ignore/disabled test data.
    /// </summary>
    static class IgnoreDataExporter
    {
        const string k_CsvHeader = "fullName,name,platforms,reasons,inclusive";

        /// <summary>
        /// Generates a CSV string from all test cases that have ignore data.
        /// Each row contains the test's full name, name, ignored platforms, reasons, and inclusive flags.
        /// Values containing commas or quotes are properly escaped.
        /// </summary>
        internal static string GenerateIgnoreDataCsv(IEnumerable<GraphicsTestCase> testCases)
        {
            var csvLines = new List<string>();

            foreach (var testCase in testCases)
            {
                if (testCase.IgnoreData == null)
                    continue;

                var platformSet = new HashSet<string>();
                foreach (var ignore in testCase.IgnoreData)
                {
                    if (ignore.m_Platforms != null)
                    {
                        foreach (var platform in ignore.m_Platforms)
                            platformSet.Add(platform.ToString());
                    }
                }
                var platforms = new List<string>(platformSet);
                platforms.Sort(StringComparer.Ordinal);

                if (platforms.Count == 0)
                    continue;

                var reasonSet = new HashSet<string>();
                foreach (var ignore in testCase.IgnoreData)
                    reasonSet.Add(ignore.reason);
                var reasons = new List<string>(reasonSet);
                reasons.Sort(StringComparer.Ordinal);

                var inclusive = new List<bool>();
                foreach (var ignore in testCase.IgnoreData)
                    inclusive.Add(ignore.inclusive);

                var fullName = EscapeCsvField(testCase.FullName);
                var name = EscapeCsvField(testCase.Name);
                var platformsStr = string.Join(";", platforms);
                var reasonsStr = EscapeCsvField(string.Join(";", reasons));
                var inclusiveStr = string.Join(";", inclusive);

                csvLines.Add($"{fullName},{name},{platformsStr},{reasonsStr},{inclusiveStr}");
            }

            return k_CsvHeader + "\n" + string.Join("\n", csvLines);
        }

        static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";

            // If the field contains commas, quotes, or newlines, wrap in quotes and escape internal quotes
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
                return "\"" + field.Replace("\"", "\"\"") + "\"";

            return "\"" + field + "\"";
        }
    }
}
