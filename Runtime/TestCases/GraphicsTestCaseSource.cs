using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics.TestCases
{
    /// <summary>
    /// Abstract class for creating graphics test cases. Inherit from this class to create a custom test case source.
    /// </summary>
    public abstract class GraphicsTestCaseSource
    {
        /// <summary>
        /// Gets the test cases for the specified method.
        /// This method is used to create a graphics test case.
        /// </summary>
        /// <param name="methodInfo">
        /// The method info for the test case.
        /// </param>
        /// <param name="suite">
        /// The suite this test case belongs to.
        /// </param>
        /// <returns>
        /// An enumerable collection of graphics test cases.
        /// </returns>
        public abstract IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite);
    }
}
