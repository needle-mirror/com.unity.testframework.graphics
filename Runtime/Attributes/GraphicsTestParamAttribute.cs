using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Supplies one set of additional arguments for a graphics test method, producing one
    /// parameterized variant per graphics test case. Multiple instances create multiple variants.
    /// </summary>
    /// <remarks>
    /// The decorated method must also carry <see cref="GraphicsTestAttribute"/> (or a derived attribute).
    /// The first parameter of the method is always the <see cref="GraphicsTestCase"/>; subsequent
    /// parameters receive the values supplied here.
    /// </remarks>
    /// <example>
    /// <code>
    /// [GraphicsTest]
    /// [GraphicsTestParam(1)]
    /// [GraphicsTestParam(2)]
    /// public void MyTest(GraphicsTestCase tc, int quality) { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GraphicsTestParamAttribute : Attribute, IGraphicsTestArgProvider
    {
        /// <summary>The argument values for this test variant.</summary>
        public object[] Arguments { get; }

        /// <summary>
        /// Optional display name override for the generated test.
        /// When <c>null</c>, the framework auto-generates a name from the arguments.
        /// </summary>
        public string TestName { get; set; }

        /// <summary>Optional description attached to the generated test.</summary>
        public string Description { get; set; }

        /// <summary>
        /// When non-null, the test variant is marked ignored with this string as the reason.
        /// </summary>
        public string Ignore { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestParamAttribute"/> class.
        /// </summary>
        /// <param name="args">The arguments to pass to the test case.</param>
        public GraphicsTestParamAttribute(params object[] args)
        {
            Arguments = args;
        }

        IEnumerable<object[]> IGraphicsTestArgProvider.GetArgSets()
        {
            yield return Arguments;
        }
    }
}
