using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics.TestCases
{
    /// <summary>
    /// Default implementation of the <see cref="GraphicsTestCaseSource"/> class.
    /// This class is used to create a default graphics test case.
    /// </summary>
    public class DefaultGraphicsTestCaseSource : GraphicsTestCaseSource
    {
        ///<inheritdoc/>
        public override IEnumerable<GraphicsTestCase> GetTestCases(IMethodInfo methodInfo, ITest suite)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

            if (suite == null)
                throw new ArgumentNullException(nameof(suite));

            return GetTestCasesIterator(methodInfo, suite);
        }

        static IEnumerable<GraphicsTestCase> GetTestCasesIterator(IMethodInfo methodInfo, ITest suite)
        {
            yield return new GraphicsTestCase(methodInfo.Name, methodInfo, suite);
        }
    }
}
