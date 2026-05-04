using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// An indexed structure for fast ignore pattern lookups.
    /// Provides O(1) exact match, O(k) prefix/suffix match where k = pattern length,
    /// instead of O(m) where m = total number of patterns.
    /// </summary>
    class IgnoreDataIndex
    {
        // O(1) lookup for exact matches
        readonly Dictionary<string, List<IgnoreGraphicsTestData>> m_ExactMatches;

        // Patterns that need prefix matching (test name starts with pattern)
        readonly List<IgnoreGraphicsTestData> m_PrefixPatterns;

        // Patterns that need suffix matching (test name ends with pattern)
        readonly List<IgnoreGraphicsTestData> m_SuffixPatterns;

        // Patterns that need regex matching
        readonly List<IgnoreGraphicsTestData> m_RegexPatterns;

        readonly int m_TotalCount;

        public IgnoreDataIndex(IList<IgnoreGraphicsTestData> ignoreData)
        {
            if (ignoreData == null || ignoreData.Count == 0)
            {
                m_ExactMatches = null;
                m_PrefixPatterns = null;
                m_SuffixPatterns = null;
                m_RegexPatterns = null;
                m_TotalCount = 0;
                return;
            }

            m_TotalCount = ignoreData.Count;
            m_ExactMatches = new Dictionary<string, List<IgnoreGraphicsTestData>>();
            m_PrefixPatterns = new List<IgnoreGraphicsTestData>();
            m_SuffixPatterns = new List<IgnoreGraphicsTestData>();
            m_RegexPatterns = new List<IgnoreGraphicsTestData>();

            foreach (var data in ignoreData)
            {
                switch (data.matchMode)
                {
                    case IgnoreGraphicsTestMode.MatchExact:
                        if (!m_ExactMatches.TryGetValue(data.pattern, out var list))
                        {
                            list = new List<IgnoreGraphicsTestData>(1);
                            m_ExactMatches[data.pattern] = list;
                        }
                        list.Add(data);
                        break;

                    case IgnoreGraphicsTestMode.MatchStart:
                        m_PrefixPatterns.Add(data);
                        break;

                    case IgnoreGraphicsTestMode.MatchEnd:
                        m_SuffixPatterns.Add(data);
                        break;

                    case IgnoreGraphicsTestMode.MatchRegex:
                    case IgnoreGraphicsTestMode.MatchRegexIgnoreCase:
                        m_RegexPatterns.Add(data);
                        break;
                }
            }
        }

        public IgnoreGraphicsTestData[] GetMatches(string fullName)
        {
            if (m_TotalCount == 0)
                return Array.Empty<IgnoreGraphicsTestData>();

            List<IgnoreGraphicsTestData> matches = new();

            // O(1) exact match lookup
            if (m_ExactMatches.Count > 0 && m_ExactMatches.TryGetValue(fullName, out var exactList))
            {
                matches = new List<IgnoreGraphicsTestData>(exactList);
            }

            // Prefix matches - still O(p) where p = prefix patterns count, but typically small
            foreach (var data in m_PrefixPatterns)
            {
                if (fullName.StartsWith(data.pattern, StringComparison.Ordinal))
                {
                    matches.Add(data);
                }
            }

            // Suffix matches
            foreach (var data in m_SuffixPatterns)
            {
                if (fullName.EndsWith(data.pattern, StringComparison.Ordinal))
                {
                    matches.Add(data);
                }
            }

            // Regex matches - use cached compiled regex
            foreach (var data in m_RegexPatterns)
            {
                if (!data.MatchesPattern(fullName))
                    continue;
                matches.Add(data);
            }

            return matches.ToArray();
        }
    }
}
