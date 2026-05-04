using System;
using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute to mark a test case as ignored for specific nodes or configurations.
    /// </summary>
    /// <remarks>
    /// This attribute can be used to ignore a test case based on the provided pattern.
    /// The pattern is matched against the test's full name and the current platform, as well as the global context values.
    /// The test case will be ignored if the pattern matches and the platform is included in the specified nodes (or is a subset of the specified nodes).
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class IgnoreGraphicsTestAttribute : Attribute, ITestAction
    {
        /// <summary>
        /// Contains the data for the Ignore Attribute.
        /// </summary>
        internal readonly IList<IgnoreGraphicsTestData> m_IgnoreData;

        /// <inheritdoc cref="ITestAction.Targets"/>
        public ActionTargets Targets => ActionTargets.Test | ActionTargets.Suite;

        /// <summary>
        /// Ignore a graphics test case based on the provided pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason for ignoring the test case.</param>
        /// <param name="isInclusive">Whether to include or exclude the test case based on the pattern.</param>
        /// <param name="matchMode">The mode to use for matching the pattern against the test case name.</param>
        /// <param name="allowOverrideIgnore">Whether to allow overriding the ignore attribute through the command line.
        /// Set to false the test could never succeed if not ignored (for example, if the platform is incompatible).</param>
        /// <param name="platforms">The platform enums to match against. These must be enums and each declared as a <see cref="IPlatformNode.DataType"/></param>
        public IgnoreGraphicsTestAttribute(
            string pattern,
            string reason,
            bool isInclusive,
            bool allowOverrideIgnore,
            IgnoreGraphicsTestMode matchMode,
            params object[] platforms
        )
        {
            if (platforms == null)
                throw new ArgumentNullException(nameof(platforms));

            var enumList = new List<Enum>(platforms.Length);
            foreach (var t in platforms)
            {
                if (t is Enum e)
                    enumList.Add(e);
                else
                    throw new ArgumentException(
                        $"Platform value '{t}' (type: {t?.GetType().Name ?? "null"}) is not an Enum.",
                        nameof(platforms)
                    );
            }

            m_IgnoreData = new List<IgnoreGraphicsTestData>(1)
            {
                new(pattern, reason, isInclusive, allowOverrideIgnore, matchMode, enumList),
            };
        }

        /// <summary>
        /// Ignore a graphics test case based on the provided pattern.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason for ignoring the test case.</param>
        /// <param name="platforms">The platform enums to match against. These must be enums and each declared as a <see cref="IPlatformNode.DataType"/></param>
        public IgnoreGraphicsTestAttribute(string pattern, string reason, params object[] platforms)
            : this(pattern, reason, false, true, IgnoreGraphicsTestMode.MatchRegex, platforms) { }
        
        /// <summary>
        /// Ignore a graphics test case based on the provided pattern, with an option to specify whether the ignore can be overridden.
        /// </summary>
        /// <param name="pattern">The pattern to match against the test case name.</param>
        /// <param name="reason">The reason for ignoring the test case.</param>
        /// <param name="isInclusive">Whether to include or exclude the test case based on the pattern.</param>
        /// <param name="platforms">The platform enums to match against. These must be enums and each declared as a <see cref="IPlatformNode.DataType"/></param>
        public IgnoreGraphicsTestAttribute(string pattern, string reason, bool isInclusive, params object[] platforms)
            : this(pattern, reason, isInclusive, true, IgnoreGraphicsTestMode.MatchRegex, platforms) { }

        /// <summary>
        /// Ignore a graphics test case based on the provided data source.
        /// </summary>
        /// <param name="ignoreDataSource">
        /// A type that implements <see cref="IList{IgnoreGraphicsTestData}"/> and has
        /// a public parameterless constructor.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="ignoreDataSource"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// The type does not implement <see cref="IList{IgnoreGraphicsTestData}"/>, or
        /// it cannot be instantiated.
        /// </exception>
        public IgnoreGraphicsTestAttribute(Type ignoreDataSource)
        {
            if (ignoreDataSource == null)
                throw new ArgumentNullException(nameof(ignoreDataSource));

            if (!typeof(IList<IgnoreGraphicsTestData>).IsAssignableFrom(ignoreDataSource))
            {
                throw new ArgumentException(
                    $"Type '{ignoreDataSource.FullName}' does not implement IList<IgnoreGraphicsTestData>.",
                    nameof(ignoreDataSource)
                );
            }

            try
            {
                m_IgnoreData = (IList<IgnoreGraphicsTestData>)Activator.CreateInstance(ignoreDataSource);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Failed to create instance of '{ignoreDataSource.FullName}'. " +
                    "Ensure it has a public parameterless constructor.",
                    nameof(ignoreDataSource),
                    ex
                );
            }
        }

        /// <summary>
        /// Ignore a graphics test case based on the provided data source.
        /// </summary>
        /// <param name="ignoreDataSource">
        /// The assembly-qualified type name of a class that implements
        /// <see cref="IList{IgnoreGraphicsTestData}"/>.
        /// </param>
        /// <exception cref="ArgumentException">The type could not be resolved.</exception>
        public IgnoreGraphicsTestAttribute(string ignoreDataSource)
            : this(
                Type.GetType(ignoreDataSource)
                ?? throw new ArgumentException(
                    $"Could not resolve type '{ignoreDataSource}'. " +
                    "Ensure the assembly-qualified name is correct and the assembly is loaded.",
                    nameof(ignoreDataSource)
                )
            )
        { }

        /// <inheritdoc cref="ITestAction.BeforeTest"/>
        public void BeforeTest(ITest test)
        {
            if (m_IgnoreData == null || m_IgnoreData.Count == 0)
                return;

            var testCase = GraphicsTestCaseCollector.Instance.GetTestCase(test);
            var currentPlatform = GraphicsTestPlatform.Current;

            for (var i = 0; i < m_IgnoreData.Count; i++)
            {
                var ignoreData = m_IgnoreData[i];
                if (!ignoreData.ShouldOverrideIgnore && ignoreData.ShouldIgnoreTestCase(testCase, currentPlatform))
                {
                    Assert.Ignore(
                        $"Test {test.FullName} has been ignored on platform: {currentPlatform}.\n\tReason: {ignoreData}"
                    );
                    return;
                }
            }
        }

        /// <inheritdoc cref="ITestAction.AfterTest"/>
        public void AfterTest(ITest test)
        {
            if (m_IgnoreData == null || m_IgnoreData.Count == 0)
                return;

            if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Passed)
                return;

            var testCase = GraphicsTestCaseCollector.Instance.GetTestCase(test);
            var currentPlatform = GraphicsTestPlatform.Current;

            for (var i = 0; i < m_IgnoreData.Count; i++)
            {
                var ignoreData = m_IgnoreData[i];
                if (!ignoreData.ShouldOverrideIgnore || !ignoreData.ShouldIgnoreTestCase(testCase, currentPlatform))
                    continue;
                Assert.Inconclusive(
                    $"Ignore Override: Test {test.FullName} was ignored by attribute on platform {currentPlatform} when it would have passed.\n\tReason: {ignoreData}"
                );
                return;
            }
        }
    }
}
