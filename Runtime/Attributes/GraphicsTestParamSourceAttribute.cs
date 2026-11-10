using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Supplies additional argument sets from an external source for graphics test parameterization.
    /// The source is a type, or a static method, property or field of the test's class, that provides
    /// an <see cref="IEnumerable{T}"/> of <c>object[]</c> or of <see cref="TestCaseData"/>.
    /// </summary>
    /// <remarks>
    /// The decorated method must also carry <see cref="GraphicsTestAttribute"/> (or a derived attribute).
    /// Each row yielded by the source produces one parameterized variant per graphics test case.
    /// When the source yields <see cref="TestCaseData"/>, the categories, run state and other properties
    /// of each row carry over to its variants, so <c>SetCategory</c>, <c>Explicit</c> and <c>Ignore</c>
    /// work as they do with NUnit's <c>TestCaseSource</c>. The test name of a row is not used; variants are
    /// named from their arguments.
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
        readonly IEnumerable<TestCaseData> m_Source;
        readonly string m_SourceName;

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestParamSourceAttribute"/> class.
        /// </summary>
        /// <param name="source">
        /// A type that implements <see cref="IEnumerable{T}"/> of <c>object[]</c> or of <see cref="TestCaseData"/>.
        /// An instance is created via <see cref="Activator.CreateInstance(Type)"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="source"/> implements neither <c>IEnumerable&lt;object[]&gt;</c> nor
        /// <c>IEnumerable&lt;TestCaseData&gt;</c>.
        /// </exception>
        public GraphicsTestParamSourceAttribute(Type source)
        {
            m_Source = AsTestCaseData(Activator.CreateInstance(source), $"Type '{source}'", nameof(source));
        }

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestParamSourceAttribute"/> class that reads its rows
        /// from a static member of the test's class.
        /// </summary>
        /// <param name="sourceName">
        /// The name of a static method without parameters, property or field of the test's class whose value is an
        /// <see cref="IEnumerable{T}"/> of <c>object[]</c> or of <see cref="TestCaseData"/>.
        /// </param>
        public GraphicsTestParamSourceAttribute(string sourceName)
        {
            m_SourceName = sourceName;
        }

        // NUnit's ValueSource resolves the member on the test's class
        IEnumerable<TestCaseData> IGraphicsTestArgProvider.GetArgSets(IMethodInfo method) =>
            m_Source ?? AsTestCaseData(new ValueSourceAttribute(m_SourceName).GetData(method.GetParameters()[0]), $"Member '{m_SourceName}'", "sourceName");

        static IEnumerable<TestCaseData> AsTestCaseData(object source, string description, string paramName) =>
            source switch
            {
                IEnumerable<TestCaseData> cases => cases,
                IEnumerable<object[]> rows => ToTestCaseData(rows),
                _ => throw new ArgumentException(
                    $"{description} must implement IEnumerable<object[]> or IEnumerable<TestCaseData>.", paramName),
            };

        static IEnumerable<TestCaseData> ToTestCaseData(IEnumerable<object[]> rows)
        {
            foreach (var row in rows)
            {
                yield return new TestCaseData(row);
            }
        }
    }
}
