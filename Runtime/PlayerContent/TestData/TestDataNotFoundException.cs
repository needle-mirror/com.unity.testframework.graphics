using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Thrown when a test data asset declared with <see cref="RequireTestDataAttribute"/> cannot be
    /// loaded. Tests must always have access to their declared assets, so a miss fails the test;
    /// the message states what was declared, what was built, and what was searched.
    /// </summary>
    public sealed class TestDataNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new instance with the load diagnostics as the message.
        /// </summary>
        /// <param name="message">The diagnostics describing the failed lookup.</param>
        public TestDataNotFoundException(string message)
            : base(message) { }
    }
}
