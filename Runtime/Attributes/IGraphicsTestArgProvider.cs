using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Implemented by attributes that supply additional argument sets for graphics test parameterization.
    /// Each <see cref="GetArgSets"/> call returns one or more rows, where each row becomes one test-case
    /// variant per graphics test case produced by the test source. The run state and properties of a row,
    /// such as its categories, carry over to the variant.
    /// </summary>
    internal interface IGraphicsTestArgProvider
    {
        IEnumerable<TestCaseData> GetArgSets(IMethodInfo method);
    }
}
