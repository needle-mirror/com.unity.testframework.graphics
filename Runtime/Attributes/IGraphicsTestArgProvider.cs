using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Implemented by attributes that supply additional argument sets for graphics test parameterization.
    /// Each <see cref="GetArgSets"/> call returns one or more <c>object[]</c> rows, where each row
    /// becomes one test-case variant per graphics test case produced by the test source.
    /// </summary>
    internal interface IGraphicsTestArgProvider
    {
        IEnumerable<object[]> GetArgSets();
    }
}
