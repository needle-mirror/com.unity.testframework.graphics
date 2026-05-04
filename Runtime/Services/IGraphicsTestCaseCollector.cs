using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Interface for Graphics Test Case Collectors
    /// </summary>
    public interface IGraphicsTestCaseCollector
    {
        /// <summary>
        /// How many test cases the collector has stored so far.
        /// </summary>
        int TestCaseCount { get; }

        /// <summary>
        /// All the test cases the collector has stored.
        /// </summary>
        /// <returns>
        /// An <c>IEnumerable</c> of all the test cases.
        /// </returns>
        IEnumerable<GraphicsTestCase> GetAllTestCases();

        /// <summary>
        /// All the setup actions the collector has stored
        /// </summary>
        /// <returns>
        /// An <c>IEnumerable</c> of all the setup actions.
        /// </returns>
        IEnumerable<SetupAction> GetAllSetupActions();

        /// <summary>
        /// Finds a test case based on a matching ITest object
        /// </summary>
        /// <param name="test">
        /// The ITest object to match
        /// </param>
        /// <returns>
        /// The matching Graphics Test Case, or null if not found.
        /// </returns>
        GraphicsTestCase GetTestCase(ITest test);

        /// <summary>
        /// Finds a test case by its full name.
        /// </summary>
        /// <param name="fullName">
        /// The full name (including assembly, class, etc.) of the test case.
        /// </param>
        /// <returns>
        /// The matching Graphics Test Case, or null if not found.
        /// </returns>
        GraphicsTestCase GetTestCaseByName(string fullName);

        /// <summary>
        /// Finds all matching test cases based on a list of ITest objects. <see cref="GetTestCase"/>
        /// </summary>
        /// <param name="testList">
        /// The list of tests to match.
        /// </param>
        /// <returns>
        /// The list of test cases found.
        /// </returns>
        IList<GraphicsTestCase> GetAllTestCasesFromTestList(IEnumerable<ITest> testList);
    }
}
