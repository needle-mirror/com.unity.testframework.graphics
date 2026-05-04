using System.Collections.Generic;
using System.Text;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEditor.TestTools.Graphics.Filtering
{
    static class TestCaseFiltering
    {
        internal static IEnumerable<GraphicsTestCase> ApplyIgnoreAttributesForPlatform(
            IEnumerable<GraphicsTestPlatform> platforms,
            IEnumerable<GraphicsTestCase> testCases
        )
        {
            var sb = new StringBuilder();
            foreach (var testCase in testCases)
            {
                if (testCase?.IgnoreData != null)
                {
                    GraphicsTestPlatform[] graphicsTestPlatforms;
                    if (platforms is GraphicsTestPlatform[] arr)
                        graphicsTestPlatforms = arr;
                    else
                        graphicsTestPlatforms = new List<GraphicsTestPlatform>(platforms).ToArray();
                    IgnoreGraphicsTestData matchingAttribute = null;
                    foreach (var a in testCase.IgnoreData)
                    {
                        var allMatch = true;
                        foreach (var p in graphicsTestPlatforms)
                        {
                            if (a.ShouldIgnoreTestCase(testCase, p))
                                continue;
                            allMatch = false;
                            break;
                        }
                        if (allMatch && !a.ShouldOverrideIgnore)
                        {
                            matchingAttribute = a;
                            break;
                        }
                    }

                    if (matchingAttribute != null)
                    {
                        testCase.ShouldBeIgnored = true;
                        testCase.IgnoreReason = matchingAttribute.reason;

                        sb.AppendLine(
                            $"Will not build ignored test case {testCase.FullName} because \"{testCase.IgnoreReason}\"."
                        );
                    }
                }
                yield return testCase;
            }

            GraphicsTestLogger.Log(sb.ToString());
        }
    }
}
