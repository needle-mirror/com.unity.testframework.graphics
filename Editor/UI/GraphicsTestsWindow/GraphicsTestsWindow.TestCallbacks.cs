using System.Collections.Concurrent;
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.Graphics.Services;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics.UI
{
    sealed partial class GraphicsTestsWindow
    {
        readonly ConcurrentDictionary<string, TestStatus> m_TestResults = new();

        void SetupTestCallbacks()
        {
            TestListener.s_OnTestResultReceived += OnTestResultReceived;
            TestListener.s_OnTestRunFinished += OnTestRunFinishedReceived;
        }

        void TearDownTestCallbacks()
        {
            TestListener.s_OnTestResultReceived -= OnTestResultReceived;
            TestListener.s_OnTestRunFinished -= OnTestRunFinishedReceived;
        }

        void OnTestResultReceived(object sender, OnTestResultReceivedArgs args)
        {
            GraphicsTestLogger.DebugLog(
                $"Received test result: {args.TestResult.FullName} - {args.TestResult.ResultState.Status}"
            );
            m_TestResults[args.TestResult.FullName] = args.TestResult.ResultState.Status;
            m_ShouldUpdateImageComparisonView = true;
        }

        void OnTestRunFinishedReceived(object sender, OnTestRunFinishedArgs args)
        {
            SaveTestResults();
            m_ShouldUpdateImageComparisonView = true;
        }
    }
}
