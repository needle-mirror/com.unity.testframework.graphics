using System;
using UnityEngine.TestTools.Graphics.TestCases;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute that allows to generate test cases based on the graphics test cases provided by <see cref="DefaultGraphicsTestCaseSource"/>.
    /// </summary>
    /// <remarks>
    /// This attribute is used to mark a method as a graphics test.
    /// </remarks>
    public sealed class GraphicsTestAttribute : GraphicsTestAttributeBase
    {
        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestAttribute"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor uses the <see cref="DefaultGraphicsTestCaseSource"/> as the source for generating test cases.
        /// </remarks>
        public GraphicsTestAttribute()
            : base(typeof(DefaultGraphicsTestCaseSource)) { }
    }
}
