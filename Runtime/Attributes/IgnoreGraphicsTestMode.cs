namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Enumeration for the different modes of ignoring graphics tests.
    /// </summary>
    public enum IgnoreGraphicsTestMode
    {
        /// <summary>
        /// Ignore the test case if the pattern matches the test case name exactly.
        /// </summary>
        MatchExact,

        /// <summary>
        /// Ignore the test case if the pattern matches the start of the test case name.
        /// </summary>
        MatchStart,

        /// <summary>
        /// Ignore the test case if the pattern matches the end of the test case name.
        /// </summary>
        MatchEnd,

        /// <summary>
        /// Ignore the test case if the pattern matches the test case name using a regular expression.
        /// </summary>
        MatchRegex,

        /// <summary>
        /// Ignore the test case if the pattern matches the test case name using a regular expression, ignoring case.
        /// </summary>
        MatchRegexIgnoreCase,
    }
}
