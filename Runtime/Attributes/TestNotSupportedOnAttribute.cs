using System;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks a test as unsupported on the specified platforms.
    /// Tests decorated with this attribute will be skipped on the listed platforms.
    /// Unlike <see cref="IgnoreGraphicsTestAttribute"/>, this ignore cannot be overridden,
    /// since the test would never succeed on an incompatible platform.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class TestNotSupportedOnAttribute : IgnoreGraphicsTestAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestNotSupportedOnAttribute"/> class.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason the test is unsupported.</param>
        /// <param name="platforms">Platform enums on which the test cannot run. Each must be declared as a <see cref="IPlatformNode.DataType"/>.</param>
        public TestNotSupportedOnAttribute(string pattern, string reason, params object[] platforms)
            : base(pattern, reason, false, false, IgnoreGraphicsTestMode.MatchRegex, platforms) { }
    }
}
