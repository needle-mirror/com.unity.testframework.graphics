using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Supplies additional argument sets from an external source type for graphics test parameterization.
    /// The source type must implement <see cref="IEnumerable{T}"/> of <c>object[]</c>.
    /// </summary>
    /// <remarks>
    /// The decorated method must also carry <see cref="GraphicsTestAttribute"/> (or a derived attribute).
    /// Each <c>object[]</c> yielded by the source produces one parameterized variant per graphics test case.
    /// </remarks>
    /// <example>
    /// <code>
    /// [GraphicsTest]
    /// [GraphicsTestParamSource(typeof(MyParamSource))]
    /// public void MyTest(GraphicsTestCase tc, int quality) { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class GraphicsTestParamSourceAttribute : Attribute, IGraphicsTestArgProvider
    {
        readonly IEnumerable<object[]> m_Source;

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestParamSourceAttribute"/> class.
        /// </summary>
        /// <param name="source">
        /// A type that implements <see cref="IEnumerable{T}"/> of <c>object[]</c>.
        /// An instance is created via <see cref="Activator.CreateInstance(Type)"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> does not implement <c>IEnumerable&lt;object[]&gt;</c>.
        /// </exception>
        public GraphicsTestParamSourceAttribute(Type source)
        {
            m_Source = Activator.CreateInstance(source) as IEnumerable<object[]>
                ?? throw new ArgumentException(
                    $"Type '{source}' must implement IEnumerable<object[]>.", nameof(source));
        }

        IEnumerable<object[]> IGraphicsTestArgProvider.GetArgSets() => m_Source;
    }
}
