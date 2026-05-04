namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class contains extension methods for the string class.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Replaces invalid characters in a string to make it a valid path.
        /// </summary>
        /// <param name="value">The string to make a valid path.</param>
        /// <returns>A valid path string.</returns>
        /// <remarks>
        /// Replacement rules are intentionally asymmetric to produce readable, filesystem-safe paths:
        /// <list type="bullet">
        /// <item><description><c>(</c> is replaced with <c>_</c> (preserves word boundary)</description></item>
        /// <item><description><c>)</c> is removed entirely (closing paren adds no value)</description></item>
        /// <item><description><c>"</c> is removed entirely</description></item>
        /// <item><description><c>,</c> is replaced with <c>-</c> (preserves visual separator)</description></item>
        /// </list>
        /// </remarks>
        public static string ToValidPath(this string value) =>
            value.Replace('(', '_').Replace(")", "").Replace("\"", "").Replace(',', '-');

        /// <summary>
        /// Replaces backslashes with forward slashes in the given string
        /// </summary>
        /// <param name="value">
        /// The string to sanitize
        /// </param>
        /// <returns>
        /// The string with backslashes replaced with forward slashes
        /// </returns>
        public static string SanitizeBackslashes(this string value) => value.Replace('\\', '/');

        /// <summary>
        /// Converts a string to a URL-safe Base64 string.
        /// </summary>
        /// <param name="value">
        /// The string to convert to a URL-safe Base64 string.
        /// </param>
        /// <returns>
        /// The URL-safe Base64 string.
        /// </returns>
        public static string ToUrlSafeBase64(this string value)
        {
            return value.Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Converts a URL-safe Base64 string to a regular Base64 string.
        /// </summary>
        /// <param name="value">
        /// The URL-safe Base64 string to convert to a regular Base64 string.
        /// </param>
        /// <returns>
        /// The regular Base64 string.
        /// </returns>
        public static string FromUrlSafeBase64(this string value)
        {
            return value.Replace('-', '+').Replace('_', '/');
        }
    }
}
