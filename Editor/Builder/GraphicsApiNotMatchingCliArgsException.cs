using System;

namespace UnityEditor.TestTools.Graphics.Builder
{
    /// <summary>
    /// Exception thrown when the graphics API and command-line arguments don't match
    /// </summary>
    public class GraphicsApiNotMatchingCliArgsException : Exception
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GraphicsApiNotMatchingCliArgsException() { }

        /// <summary>
        /// Constructor with message
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        public GraphicsApiNotMatchingCliArgsException(string message)
            : base(message) { }

        /// <summary>
        /// Constructor with message and inner exception
        /// </summary>
        /// <param name="message">
        /// The message that describes the error.
        /// </param>
        /// <param name="innerException">
        /// The inner exception that is the cause of the current exception, or a null reference if no inner exception is specified.
        /// </param>
        public GraphicsApiNotMatchingCliArgsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
