using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// A class that holds data about ignoring test cases.
    /// </summary>
    [Serializable]
    public record IgnoreGraphicsTestData
    {
        /// <summary>
        /// The pattern to match against test cases.
        /// </summary>
        public string pattern;

        /// <summary>
        /// The reason the test is ignored.
        /// </summary>
        public string reason;

        /// <summary>
        /// Whether the ignore data is to be applied inclusively or exclusively.
        /// </summary>
        /// <remarks>
        /// Exclusive ignore data excludes the matching nodes
        /// and so runs on all but the declared platform combinations.
        /// Inclusive ignore data includes the matching test cases
        /// and so run on only the declared platform combinations.
        /// </remarks>
        public bool inclusive;

        /// <summary>
        /// Whether to allow this Ignore to be overriden through the settings
        /// </summary>
        public bool allowOverride;

        /// <summary>
        /// The match mode for this ignore data.
        /// This affects how the pattern is matched against test names.
        /// </summary>
        public IgnoreGraphicsTestMode matchMode;

        /// <summary>
        /// The nodes this ignore will match against.
        /// These nodes and any subset of them will be a match.
        /// </summary>
        public GraphicsTestPlatform[] m_Platforms { get; set; }

        // Cached compiled regex for pattern matching - avoids creating new Regex on each MatchesPattern call
        [NonSerialized]
        Regex m_CachedRegex;

        internal bool ShouldOverrideIgnore => allowOverride && HasOverriddenIgnore;

        internal static bool HasOverriddenIgnore
        {
            get => GraphicsTestBuildSettings.LoadOrDefault().OverrideIgnoreAttributes;
            set => GraphicsTestBuildSettings.LoadOrDefault().OverrideIgnoreAttributes = value;
        }

        /// <summary>
        /// Creates an instance of IgnoreGraphicsTestData.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason for ignoring the test case.</param>
        /// <param name="inclusive">Whether to include or exclude the test case based on the pattern.</param>
        /// <param name="matchMode">The mode to use for matching the pattern against the test case name.</param>
        /// <param name="allowOverride">Whether to allow overriding the ignore attribute through the command line.
        /// Set to false the test could never succeed if not ignored (for example, if the platform is incompatible).</param>
        /// <param name="data">The list of enums to use for generating the matching nodes for this ignore data.</param>
        public IgnoreGraphicsTestData(
            string pattern,
            string reason,
            bool inclusive,
            bool allowOverride,
            IgnoreGraphicsTestMode matchMode,
            List<Enum> data
        )
        {
            this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
            this.reason = reason ?? throw new ArgumentNullException(nameof(reason));
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            this.inclusive = inclusive;
            this.allowOverride = allowOverride;
            this.matchMode = matchMode;
            var combined = GraphicsTestPlatform.Combine(data);
            var platforms = new GraphicsTestPlatform[combined.Count];
            for (var i = 0; i < combined.Count; i++)
                platforms[i] = combined[i];
            m_Platforms = platforms;
        }

        internal bool ShouldIgnoreTestCase(GraphicsTestCase testCase, GraphicsTestPlatform platform)
        {
            if (testCase == null)
                return false;

            return MatchesPattern(testCase.FullName) && MatchesPlatform(platform) ^ inclusive;
        }

        internal bool MatchesPattern(string name)
        {
            switch (matchMode)
            {
                case IgnoreGraphicsTestMode.MatchExact:
                    return name == pattern;
                case IgnoreGraphicsTestMode.MatchStart:
                    return name.StartsWith(pattern);
                case IgnoreGraphicsTestMode.MatchEnd:
                    return name.EndsWith(pattern);
                case IgnoreGraphicsTestMode.MatchRegex:
                    m_CachedRegex ??= new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                    return m_CachedRegex.IsMatch(name);
                case IgnoreGraphicsTestMode.MatchRegexIgnoreCase:
                    m_CachedRegex ??= new Regex(
                        pattern,
                        RegexOptions.IgnoreCase | RegexOptions.Compiled,
                        TimeSpan.FromMilliseconds(100)
                    );
                    return m_CachedRegex.IsMatch(name);
                default:
                    throw new IndexOutOfRangeException($"Invalid match mode: {matchMode}");
            }
        }

        bool MatchesPlatform(GraphicsTestPlatform platform) =>
            m_Platforms != null && m_Platforms.Length > 0 && Array.Exists(m_Platforms, p => p >= platform);
    }
}
