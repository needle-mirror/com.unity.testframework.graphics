using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEngine.TestTools.Graphics.Platforms;
using UnityEngine.TestTools.Graphics.TestCases;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Base class for attributes that define graphics tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public abstract class GraphicsTestAttributeBase : Attribute, ITestBuilder, IImplyFixture, ITestAction
    {
        static readonly NUnitTestCaseBuilder k_Builder = new();
        static readonly Dictionary<Type, GraphicsTestCaseSource> k_SourceCache = new();
        static readonly Dictionary<MethodIdentifier, GraphicsTestCaseData> k_TestCaseCache = new();
        readonly GraphicsTestCaseSource m_Source;

        /// <summary>
        /// The type of texture to use for the reference image.
        /// </summary>
        /// <remarks>
        /// This is used to determine how the reference image is to be loaded and used.
        /// </remarks>
        public TextureFormat TextureFormat { get; init; }

        /// <summary>
        /// The extension of the reference image. If not set, the default is "png".
        /// </summary>
        /// <remarks>
        /// This is only used for the reference image asset path.
        /// </remarks>
        public ImageExtension ImageExtension { get; init; } = ImageExtension.PNG;

        /// <summary>
        /// Controls how the reference image file stem is chosen when the test method is parameterized.
        /// Ignored when <see cref="ReferenceImageNamingStrategyType"/> is set and successfully returns a descriptor.
        /// </summary>
        /// <remarks>
        /// <see cref="ReferenceImageRootSource.SceneAssetFileStem"/> makes every parameter combination for the same
        /// scene-based case use one reference image named after the scene file.
        /// </remarks>
        public ReferenceImageRootSource ReferenceImageRootSource { get; init; } = ReferenceImageRootSource.ParameterizedTestName;

        /// <summary>
        /// Optional type implementing <see cref="IReferenceImageNamingStrategy"/> with a public parameterless constructor.
        /// When set, takes precedence over <see cref="ReferenceImageRootSource"/>.
        /// </summary>
        public Type ReferenceImageNamingStrategyType { get; init; }

        /// <summary>
        /// Event that is raised when a new graphics test case is created.
        /// This event can be used to perform additional setup actions for the test case or to process the test case in some way.
        /// </summary>
        /// <remarks>
        /// This event is raised after the test case is created but before it is executed.
        /// The event handler receives the test case and the setup actions as parameters.
        /// </remarks>
        public static event EventHandler<GraphicsTestCaseCreatedArgs> TestCaseCreated = delegate { };

#if UNITY_EDITOR
        static GraphicsTestAttributeBase()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        static void OnAfterAssemblyReload()
        {
            k_SourceCache.Clear();
            k_TestCaseCache.Clear();
        }
#endif

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsTestAttributeBase"/> class.
        /// </summary>
        /// <param name="sourceType">The source type to use for generating test cases.</param>
        /// <remarks>
        /// This constructor uses the specified source type to generate test cases.
        /// The source type must implement the <see cref="GraphicsTestCaseSource"/> interface.
        /// </remarks>
        protected GraphicsTestAttributeBase(Type sourceType)
        {
            m_Source = k_SourceCache.TryGetValue(sourceType, out var value)
                ? value
                : Activator.CreateInstance(sourceType) as GraphicsTestCaseSource;
            k_SourceCache[sourceType] =
                m_Source
                ?? throw new ArgumentException(
                    $"The source type '{sourceType}' does not implement the {nameof(GraphicsTestCaseSource)} interface or could not be instantiated."
                );
        }

        IEnumerable<TestMethod> Build(IMethodInfo method, Test suite)
        {
            var identifier = MethodIdentifier.FromIMethodInfo(method, suite);
            if (!k_TestCaseCache.TryGetValue(identifier, out var testCaseData))
            {
                testCaseData = new GraphicsTestCaseData(
                    method,
                    suite,
                    m_Source,
                    TextureFormat,
                    ImageExtension,
                    parameterizers: null,
                    referenceImageRootSource: ReferenceImageRootSource,
                    referenceImageNamingStrategyType: ReferenceImageNamingStrategyType);
                k_TestCaseCache[identifier] = testCaseData;
            }

            var testCases = testCaseData.GraphicsTestCases;
            var setupActions = testCaseData.SetupActions;
            foreach (var testCase in testCases)
            {
                var test = k_Builder.BuildTestMethod(method, suite, testCase.TestData);
                test.Name = testCase.Name;

                NotifyTestCaseCreated(testCase, setupActions);
                yield return test;
            }
        }

        void NotifyTestCaseCreated(GraphicsTestCase newTestCase, IEnumerable<SetupAction> setupActions = null)
        {
            GraphicsTestCaseCreatedArgs args = new()
            {
                TestCase = newTestCase,
                SetupActions = setupActions ?? Array.Empty<SetupAction>(),
            };

            OnGraphicsTestCaseCreated(this, args);
        }

        static Test SetSuiteProperties(Test suite, GraphicsTestPlatform platform)
        {
            foreach (var data in platform.Data)
            {
                suite.Properties.Set(data.Key.AssemblyQualifiedName, data.Value);
            }

            foreach (var fixtureData in (suite.Fixture as TestFixture)?.Arguments ?? Array.Empty<object>())
            {
                suite.Properties.Set(fixtureData.GetType().AssemblyQualifiedName, fixtureData);
            }

            return suite;
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            try
            {
                suite = SetSuiteProperties(suite, GraphicsTestPlatform.Current);
                return Build(method, suite);
            }
            catch (Exception ex)
            {
                GraphicsTestLogger.Log(LogType.Error, $"Failed to generate graphics test cases: {ex}.");
                throw;
            }
        }

        static void OnGraphicsTestCaseCreated(object sender, GraphicsTestCaseCreatedArgs e)
        {
            TestCaseCreated(sender, e);
        }

        static string CreateHyperLink(string testCase)
        {
            return $"<a href=\" \" gtf=\"{testCase}\">View in the Graphics Tests Window</a>";
        }

        /// <inheritdoc />
        public void BeforeTest(ITest test)
        {
            TestContext.WriteLine(CreateHyperLink(test.FullName));
        }

        /// <inheritdoc />
        public void AfterTest(ITest test) { }

        /// <inheritdoc />
        public ActionTargets Targets { get; } = ActionTargets.Test;
    }

    /// <summary>
    /// Event arguments for the <see cref="GraphicsTestAttribute.TestCaseCreated"/> event.
    /// This class contains the test case and the setup actions associated with the test case.
    /// </summary>
    public class GraphicsTestCaseCreatedArgs : EventArgs
    {
        /// <summary>
        /// The test case that was created. The type of the test case will depend on the type returned by the test case source.
        /// </summary>
        public GraphicsTestCase TestCase { get; init; }

        /// <summary>
        /// The setup actions associated with the test case.
        /// </summary>
        public IEnumerable<SetupAction> SetupActions { get; init; }
    }
}
