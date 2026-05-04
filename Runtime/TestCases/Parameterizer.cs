using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace UnityEngine.TestTools.Graphics
{
    abstract class Parameterizer
    {
        protected abstract HashSet<TestCaseData> Parameterize(
            TestCaseData originalTestCase,
            IMethodInfo method
        );

        internal abstract bool CanParameterize(IMethodInfo method);

        internal HashSet<TestCaseData> ParameterizeTestCases(
            TestCaseData originalTestCase,
            IMethodInfo method
        )
        {
            var testCases = Parameterize(originalTestCase, method);

            foreach (var testCase in testCases)
            {
                if (!string.IsNullOrEmpty(testCase.TestName))
                    continue;

                var args = testCase.Arguments;
                var paramArgs = new object[args.Length - 1];
                for (var i = 1; i < args.Length; i++)
                    paramArgs[i - 1] = args[i];
                testCase.TestName =
                    $"{(originalTestCase.Arguments[0] as GraphicsTestCase)?.FullName}{GenerateParametricName(paramArgs)}";
            }

            return testCases;
        }

        internal static HashSet<TestCaseData> GenerateSerialParametricTestCases(
            List<List<object>> arguments,
            TestCaseData testCase
        )
        {
            if (arguments.Count == 0)
                return new HashSet<TestCaseData> { testCase };

            HashSet<TestCaseData> newData = new HashSet<TestCaseData>();

            foreach (var argument in arguments)
            {
                var args = testCase.Arguments;
                var combined = new object[args.Length + argument.Count];
                for (var i = 0; i < args.Length; i++)
                    combined[i] = args[i];
                for (var i = 0; i < argument.Count; i++)
                    combined[args.Length + i] = argument[i];
                newData.Add(new TestCaseData(combined));
            }

            return newData;
        }

        internal HashSet<TestCaseData> GenerateCombinatorialParametricTestCases(
            List<List<object>> arguments,
            TestCaseData originalTestCase
        )
        {
            if (arguments.Count == 0)
                return new HashSet<TestCaseData> { originalTestCase };

            var initialDataset = new HashSet<TestCaseData> { originalTestCase };
            foreach (List<object> parameter in arguments)
            {
                var newData = new HashSet<TestCaseData>();

                foreach (var existingTestCase in initialDataset)
                {
                    foreach (var value in parameter)
                    {
                        var args = existingTestCase.Arguments;
                        var combined = new object[args.Length + 1];
                        for (var i = 0; i < args.Length; i++)
                            combined[i] = args[i];
                        combined[args.Length] = value;
                        newData.Add(new TestCaseData(combined));
                    }
                }
                initialDataset = newData;
            }
            return initialDataset;
        }

        string GenerateParametricName(object[] arguments)
        {
            if (arguments.Length == 0)
                return string.Empty;

            return "(" + string.Join(',', arguments) + ")";
        }

        protected List<IParameterInfo> GetParametersWithAttribute<T>(IMethodInfo method)
            where T : System.Attribute
        {
            var result = new List<IParameterInfo>();
            foreach (var p in method.GetParameters())
            {
                var attrs = p.GetCustomAttributes<T>(false);
                if (attrs != null && attrs.Length > 0)
                    result.Add(p);
            }
            return result;
        }
    }
}
