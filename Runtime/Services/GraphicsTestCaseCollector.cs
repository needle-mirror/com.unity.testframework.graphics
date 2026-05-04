using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
#endif

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Collects all graphics test cases in the project.
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class GraphicsTestCaseCollector : IGraphicsTestCaseCollector
    {
        internal static IGraphicsTestCaseCollector Instance { get; set; }

        readonly Dictionary<string, GraphicsTestCase> m_TestCases = new();
        readonly List<SetupAction> m_PrebuildSetupActions = new();

        // Subscribe to events in the Editor
#if UNITY_EDITOR
        static GraphicsTestCaseCollector()
        {
            Instance = new GraphicsTestCaseCollector();
        }
#endif

        // Subscribe to events in the Player
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void Initialize()
        {
            Instance = new GraphicsTestCaseCollector();
        }

        GraphicsTestCaseCollector()
        {
            GraphicsTestAttributeBase.TestCaseCreated += OnGraphicsTestCaseCreated;
#if UNITY_EDITOR
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            Refresh();
#endif
        }

        /// <inheritdoc/>
        public int TestCaseCount { get; private set; }

        /// <inheritdoc/>
        public IEnumerable<GraphicsTestCase> GetAllTestCases()
        {
            var sorted = new List<GraphicsTestCase>(m_TestCases.Values);
            sorted.Sort((a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            return sorted;
        }

        /// <inheritdoc/>
        public GraphicsTestCase GetTestCaseByName(string fullName)
        {
            return m_TestCases.GetValueOrDefault(fullName);
        }

        /// <inheritdoc/>
        public GraphicsTestCase GetTestCase(ITest test)
        {
            return m_TestCases.GetValueOrDefault(test.FullName);
        }

        /// <inheritdoc/>
        public IList<GraphicsTestCase> GetAllTestCasesFromTestList(IEnumerable<ITest> testList)
        {
            var testCases = new List<GraphicsTestCase>();
            foreach (var test in testList)
            {
                var testCase = GetTestCase(test);
                if (testCase != null)
                    testCases.Add(testCase);
            }

            return testCases;
        }

        /// <inheritdoc/>
        public IEnumerable<SetupAction> GetAllSetupActions()
        {
            var comparer = new SetupActionEqualityComparer();
            var seen = new HashSet<SetupAction>(comparer);
            var unique = new List<SetupAction>();
            foreach (var action in m_PrebuildSetupActions)
            {
                if (seen.Add(action))
                    unique.Add(action);
            }
            unique.Sort((a, b) => a.Order.CompareTo(b.Order));
            return unique;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Refreshes the test case collector.
        /// This is useful for reloading test cases after assembly reload.
        /// </summary>
        static void Refresh()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RetrieveTestList(UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode, _ => { });
            api.RetrieveTestList(UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode, _ => { });
        }

        void OnAfterAssemblyReload()
        {
            TestCaseCount = 0;
            m_TestCases.Clear();
            m_PrebuildSetupActions.Clear();
            Refresh();
        }
#endif

        void OnGraphicsTestCaseCreated(object sender, GraphicsTestCaseCreatedArgs e)
        {
            if (e == null || e.TestCase == null)
            {
                Debug.LogWarning("GraphicsTestCaseCollector: Test case is null. Skipping test case.");
                return;
            }

            if (string.IsNullOrEmpty(e.TestCase.FullName))
            {
                Debug.LogWarning(
                    $"GraphicsTestCaseCollector: Test case {e.TestCase} full name is null or empty. Skipping test case."
                );
                return;
            }

            if (!m_TestCases.TryAdd(e.TestCase.FullName, e.TestCase))
                return;
            m_PrebuildSetupActions.AddRange(e.SetupActions);
            TestCaseCount++;
        }
    }
}
