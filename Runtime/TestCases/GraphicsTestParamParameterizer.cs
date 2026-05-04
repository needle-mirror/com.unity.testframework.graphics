using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Unified parameterizer for <see cref="GraphicsTestParamAttribute"/> and
    /// <see cref="GraphicsTestParamSourceAttribute"/>. Replaces the former
    /// GTestCaseAttributeParameterizer and GTestCaseSourceAttributeParameterizer.
    /// </summary>
    class GraphicsTestParamParameterizer : Parameterizer
    {
        internal override bool CanParameterize(IMethodInfo method)
        {
            foreach (var attr in Attribute.GetCustomAttributes(method.MethodInfo))
            {
                if (attr is IGraphicsTestArgProvider)
                    return true;
            }

            return false;
        }

        protected override HashSet<TestCaseData> Parameterize(TestCaseData originalTestCase, IMethodInfo method)
        {
            var result = new HashSet<TestCaseData>();
            var baseArgs = originalTestCase.Arguments;

            foreach (var attr in Attribute.GetCustomAttributes(method.MethodInfo))
            {
                if (attr is not IGraphicsTestArgProvider provider)
                    continue;

                string overrideName = (attr as GraphicsTestParamAttribute)?.TestName;

                foreach (var argSet in provider.GetArgSets())
                {
                    var combined = new object[baseArgs.Length + argSet.Length];
                    Array.Copy(baseArgs, combined, baseArgs.Length);
                    Array.Copy(argSet, 0, combined, baseArgs.Length, argSet.Length);

                    var tcd = new TestCaseData(combined);
                    if (!string.IsNullOrEmpty(overrideName))
                        tcd.TestName = overrideName;
                    result.Add(tcd);
                }
            }

            return result.Count > 0 ? result : new HashSet<TestCaseData> { originalTestCase };
        }
    }
}
