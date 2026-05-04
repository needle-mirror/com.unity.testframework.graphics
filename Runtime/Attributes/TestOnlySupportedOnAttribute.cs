using System;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test as only supported on the specified platforms.
    /// Tests decorated with this attribute will be skipped on all platforms except those listed.
    /// Unlike <see cref="IgnoreGraphicsTestAttribute"/>, this ignore cannot be overridden,
    /// since the test would never succeed on an incompatible platform.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TestOnlySupportedOnAttribute : IgnoreGraphicsTestAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestOnlySupportedOnAttribute"/> class.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason the test is restricted to these platforms.</param>
        /// <param name="platforms">Platform enums on which the test can run. Each must be declared as a <see cref="IPlatformNode.DataType"/>.</param>
        public TestOnlySupportedOnAttribute(string pattern, string reason, params object[] platforms)
            : base(pattern, reason, true, false, IgnoreGraphicsTestMode.MatchRegex, platforms) { }
    }
}
