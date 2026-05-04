using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools.Graphics.TestCases;

namespace UnityEngine.TestTools.Graphics
{
    class GraphicsTestCaseData
    {
        internal IMethodInfo MethodInfo { get; }
        internal TextureFormat TextureFormat { get; }
        internal ImageExtension Extension { get; }
        internal Object ExpectedResult { get; }
        internal bool HasExpectedResult { get; }
        internal IList<SetupAction> SetupActions { get; }
        internal IList<IgnoreGraphicsTestData> IgnoreTestData { get; }
        internal IList<GraphicsTestCase> RawTestCases { get; }
        internal IList<GraphicsTestCase> GraphicsTestCases { get; }
        ITest TestSuite { get; }

        readonly Parameterizer[] m_Parameterizers;
        readonly IReferenceImageNamingStrategy m_ReferenceImageNamingStrategy;

        static readonly Parameterizer[] k_DefaultParameterizers =
        {
            new GraphicsTestParamParameterizer(),
            new ValueSourceAttributeParameterizer(),
            new ValuesAttributeParameterizer(),
        };

        internal GraphicsTestCaseData(
            IMethodInfo method,
            ITest parent,
            TextureFormat format,
            ImageExtension extension,
            bool hasExpectedResult,
            Object expectedResult,
            IList<SetupAction> setupActions,
            IList<IgnoreGraphicsTestData> ignoreTestData,
            IList<GraphicsTestCase> rawTestCases,
            Parameterizer[] parameterizers = null,
            ReferenceImageRootSource referenceImageRootSource = ReferenceImageRootSource.ParameterizedTestName,
            Type referenceImageNamingStrategyType = null
        )
        {
            MethodInfo = method;
            TestSuite = parent;
            TextureFormat = format;
            Extension = extension;
            ExpectedResult = expectedResult;
            HasExpectedResult = hasExpectedResult;
            SetupActions = setupActions;
            IgnoreTestData = ignoreTestData;
            RawTestCases = rawTestCases;
            m_Parameterizers = parameterizers ?? k_DefaultParameterizers;
            m_ReferenceImageNamingStrategy = CreateNamingStrategy(referenceImageNamingStrategyType, referenceImageRootSource);
            GraphicsTestCases = ParameterizeTestCases();
        }

        public GraphicsTestCaseData(
            IMethodInfo testMethod,
            ITest parent,
            GraphicsTestCaseSource source,
            TextureFormat format,
            ImageExtension extension,
            Parameterizer[] parameterizers = null,
            ReferenceImageRootSource referenceImageRootSource = ReferenceImageRootSource.ParameterizedTestName,
            Type referenceImageNamingStrategyType = null
        )
            : this(
                testMethod,
                parent,
                format,
                extension,
                GetHasExpectedResult(testMethod),
                GetExpectedResult(testMethod),
                GetSetupActions(testMethod),
                GetIgnoreData(testMethod),
                source.GetTestCases(testMethod, parent) as IList<GraphicsTestCase>
                    ?? new List<GraphicsTestCase>(source.GetTestCases(testMethod, parent)),
                parameterizers,
                referenceImageRootSource,
                referenceImageNamingStrategyType
            )
        {
            if (HasExpectedResult && testMethod.ReturnType.Type != typeof(IEnumerator))
            {
                GraphicsTestLogger.LogWarning(
                    $"Graphics tests with an expected result are not yet supported for synchronous tests. Method {testMethod.Name} must return IEnumerator, void, or Task."
                );
            }
        }

        static bool GetHasExpectedResult(IMethodInfo method)
        {
            return method.ReturnType.Type != typeof(void) && method.ReturnType.Type != typeof(Task);
        }

        static Object GetExpectedResult(IMethodInfo method)
        {
            return GetHasExpectedResult(method) ? new Object() : null;
        }

        static IList<IgnoreGraphicsTestData> GetIgnoreData(IMethodInfo method)
        {
#if UNITY_EDITOR
            var result = new List<IgnoreGraphicsTestData>();

            foreach (var attr in method.GetCustomAttributes<IgnoreGraphicsTestAttribute>(true))
            {
                if (attr.m_IgnoreData != null)
                    result.AddRange(attr.m_IgnoreData);
            }

            var declaringType = method.MethodInfo.DeclaringType;
            if (declaringType != null)
            {
                foreach (var attr in declaringType.GetCustomAttributes<IgnoreGraphicsTestAttribute>())
                {
                    if (attr.m_IgnoreData != null)
                        result.AddRange(attr.m_IgnoreData);
                }
            }

            return result;
#else
            return new List<IgnoreGraphicsTestData>();
#endif
        }

        static IList<SetupAction> GetSetupActions(IMethodInfo method)
        {
#if UNITY_EDITOR
            var result = new List<SetupAction>();

            foreach (var attr in Attribute.GetCustomAttributes(method.MethodInfo, true))
            {
                if (attr is GraphicsPrebuildSetupAttribute setupAttr)
                    result.Add(setupAttr.SetupAction);
            }

            var declaringType = method.MethodInfo.DeclaringType;
            if (declaringType != null)
            {
                foreach (var attr in Attribute.GetCustomAttributes(declaringType, true))
                {
                    if (attr is GraphicsPrebuildSetupAttribute setupAttr)
                        result.Add(setupAttr.SetupAction);
                }
            }

            return result;
#else
            return new List<SetupAction>();
#endif
        }

        static IReferenceImageNamingStrategy CreateNamingStrategy(Type strategyType, ReferenceImageRootSource rootSource)
        {
            IReferenceImageNamingStrategy strategy = ParameterizedTestNameNamingStrategy.Instance;

            if (strategyType != null)
            {
                try
                {
                    var instance = Activator.CreateInstance(strategyType);
                    if (instance is IReferenceImageNamingStrategy customStrategy)
                    {
                        strategy = customStrategy;
                    }
                    else
                    {
                        GraphicsTestLogger.LogWarning(
                            $"Type {strategyType.FullName} does not implement {nameof(IReferenceImageNamingStrategy)}.");
                    }
                }
                catch (Exception ex)
                {
                    GraphicsTestLogger.LogWarning(
$"Could not create reference image naming strategy {strategyType.FullName}: {ex}");
                }
            }
            else if (rootSource == ReferenceImageRootSource.SceneAssetFileStem)
            {
                strategy = SceneAssetFileStemNamingStrategy.Instance;
            }

            return strategy;
        }

        IList<GraphicsTestCase> ParameterizeTestCases()
        {
            if (RawTestCases == null || RawTestCases.Count == 0)
                return Array.Empty<GraphicsTestCase>();

            // Find valid parameterizer once - it doesn't change per test case
            Parameterizer validParameterizer = null;
            foreach (var t in m_Parameterizers)
            {
                if (!t.CanParameterize(MethodInfo))
                    continue;
                validParameterizer = t;
                break;
            }

            // Pre-calculate capacity if possible
            var finalTestCases = new List<GraphicsTestCase>(RawTestCases.Count);

            // Cache these lookups outside the loop
            var methodName = MethodInfo?.MethodInfo?.Name ?? string.Empty;

            // Build indexed lookup for ignore data - O(1) exact match, faster prefix/suffix
            var ignoreIndex = new IgnoreDataIndex(IgnoreTestData);

            foreach (var testCase in RawTestCases)
            {
                var testCaseName = testCase.Name;
                var testCaseFullName = testCase.FullName;

                if (validParameterizer != null)
                {
                    var originalTestCaseData = new TestCaseData(new object[] { testCase })
                    {
                        ExpectedResult = ExpectedResult,
                        HasExpectedResult = HasExpectedResult,
                        TestName = testCaseFullName,
                    };
                    var testCaseData = validParameterizer.ParameterizeTestCases(originalTestCaseData, MethodInfo);
                    testCaseData.Remove(originalTestCaseData);

                    foreach (var data in testCaseData)
                    {
                        var extraArgs = data.Arguments.Length > 1 ? data.Arguments[1..] : null;
                        finalTestCases.Add(
                            CreateParameterizedTestCase(
                                testCase,
                                testCaseName,
                                data.TestName,
                                methodName,
                                ignoreIndex,
                                extraArgs
                            )
                        );
                    }
                }
                else
                {
                    finalTestCases.Add(
                        CreateParameterizedTestCase(
                            testCase,
                            testCaseName,
                            testCaseFullName,
                            methodName,
                            ignoreIndex,
                            null
                        )
                    );
                }
            }

            return finalTestCases;
        }

        IReferenceImageFileDescriptor CreateReferenceImageDescriptor(GraphicsTestCase rawCase, string parameterizedTestName)
        {
            var result = m_ReferenceImageNamingStrategy.CreateDescriptor(
                rawCase,
                parameterizedTestName,
                Extension,
                TextureFormat);

            if (result == null)
            {
                GraphicsTestLogger.LogWarning(
                    $"Reference image naming strategy {m_ReferenceImageNamingStrategy.GetType().FullName} returned null; using default naming.");
                result = ParameterizedTestNameNamingStrategy.Instance.CreateDescriptor(
                    rawCase,
                    parameterizedTestName,
                    Extension,
                    TextureFormat);
            }

            return result;
        }

        GraphicsTestCase CreateParameterizedTestCase(
            GraphicsTestCase testCase,
            string testCaseName,
            string fullName,
            string methodName,
            IgnoreDataIndex ignoreIndex,
            object[] additionalArgs
        )
        {
            var idx = fullName.LastIndexOf(testCaseName, StringComparison.Ordinal);
            var testName = idx >= 0 ? fullName.Substring(idx) : fullName;
            var imageParts = CreateReferenceImageDescriptor(testCase, testName);

            var result = testCase with
            {
                Name = testName,
                FileName = testName.ToValidPath(),
                FullName = fullName,
                Fixture = TestSuite,
                ReferenceImageDescriptor = imageParts,
                ReferenceImage = new ReferenceImage(
                    imageParts.BuildDefaultName(),
                    imageParts.Format,
                    imageParts.Extension
                ),
                IgnoreData = ignoreIndex.GetMatches(fullName),
            };

            var args = additionalArgs != null ? PrependToArray(result, additionalArgs) : new object[] { result };

            result.TestData = new TestCaseData(args)
            {
                ExpectedResult = ExpectedResult,
                HasExpectedResult = HasExpectedResult,
                TestName = $"{methodName}.{testName}",
            };

            return result;
        }

        static object[] PrependToArray(object first, object[] rest)
        {
            var result = new object[rest.Length + 1];
            result[0] = first;
            Array.Copy(rest, 0, result, 1, rest.Length);
            return result;
        }
    }
}
