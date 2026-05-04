using System;
using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.TestRunner;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;

[assembly: TestRunCallback(typeof(UnityEditor.TestTools.Graphics.Services.TestListener))]

namespace UnityEditor.TestTools.Graphics.Services
{
    [InitializeOnLoad]
    class TestListener : ITestRunCallback
    {
        internal delegate void OnTestResultReceivedDelegate(object sender, OnTestResultReceivedArgs args);
        internal static event OnTestResultReceivedDelegate s_OnTestResultReceived = delegate { };
        internal delegate void OnTestRunFinishedDelegate(object sender, OnTestRunFinishedArgs args);
        internal static event OnTestRunFinishedDelegate s_OnTestRunFinished = delegate { };
        static Guid runStartedMessageId => new("6a7f53dd-4672-461d-a7b5-9467e9393fd3");
        static Guid runFinishedMessageId => new("ffb622fc-34ad-4901-8d7b-47fb04b0bdd4");
        static Guid testStartedMessageId => new("b54d241e-d88d-4dba-8c8f-ee415d11c030");
        static Guid testFinishedMessageId => new("72f7b7f4-6829-4cd1-afde-78872b9d5adc");
        static Guid quitPlayerMessageId => new("ab44bfe0-bb50-4ee6-9977-69d2ea6bb3a0");
        static Guid playerAliveHeartbeat => new("8c0c307b-f7fd-4216-8623-35b4b3f55fb6");

        static TestListener()
        {
            EditorConnection.instance.Register(testFinishedMessageId, OnTestFinishedMessageReceived);
            EditorConnection.instance.Register(runFinishedMessageId, OnTestRunFinishedMessageReceived);
            AssemblyReloadEvents.beforeAssemblyReload += UnregisterConnectionHandlers;
        }

        static void UnregisterConnectionHandlers()
        {
            EditorConnection.instance.Unregister(testFinishedMessageId, OnTestFinishedMessageReceived);
            EditorConnection.instance.Unregister(runFinishedMessageId, OnTestRunFinishedMessageReceived);
            AssemblyReloadEvents.beforeAssemblyReload -= UnregisterConnectionHandlers;
        }

        static void OnTestFinishedMessageReceived(MessageEventArgs args)
        {
            var json = System.Text.Encoding.UTF8.GetString(args.data);
            var result = JsonUtility.FromJson<RemoteTestResultDataWithTestData>(json);

            if (result?.results == null)
                return;

            foreach (var testResult in result.results)
            {
                s_OnTestResultReceived.Invoke(null, new OnTestResultReceivedArgs { TestResult = testResult });
            }
        }

        static void OnTestRunFinishedMessageReceived(MessageEventArgs args)
        {
            var json = System.Text.Encoding.UTF8.GetString(args.data);
            var result = JsonUtility.FromJson<RemoteTestResultDataWithTestData>(json);
            s_OnTestRunFinished.Invoke(null, new OnTestRunFinishedArgs());
        }

        public void RunStarted(ITest testsToRun)
        {
            GraphicsTestLogger.Log("Test run started.");
            GraphicsTestLogger.Log($"Running tests on: {GraphicsTestPlatform.Current.PrintPlatformInfo()}");
        }

        public void RunFinished(ITestResult testResults)
        {
            GraphicsTestLogger.Log("Test run finished.");
            s_OnTestRunFinished.Invoke(this, new OnTestRunFinishedArgs { TestResult = testResults });
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result)
        {
            s_OnTestResultReceived.Invoke(this, new OnTestResultReceivedArgs { TestResult = result });
        }
    }

    [Serializable]
    class RemoteTestResultDataWithTestData
    {
        public RemoteTestResultData[] results;
    }

    [Serializable]
    class RemoteTestResultData : ITestResult
    {
        public string testId;
        public string name;
        public string fullName;
        public string resultState;
        public TestStatus testStatus;
        public double duration;
        public DateTime startTime;
        public DateTime endTime;
        public string message;
        public string stackTrace;
        public int assertCount;
        public int failCount;
        public int passCount;
        public int skipCount;
        public int inconclusiveCount;
        public bool hasChildren;
        public string output;
        public string xml;
        public string[] childrenIds;

        public TNode ToXml(bool recursive)
        {
            throw new NotImplementedException();
        }

        public TNode AddToXml(TNode parentNode, bool recursive)
        {
            throw new NotImplementedException();
        }

        public ResultState ResultState => new(testStatus);
        public string Name => name;
        public string FullName => fullName;
        public double Duration => duration;
        public DateTime StartTime => startTime;
        public DateTime EndTime => endTime;
        public string Message => message;
        public string StackTrace => stackTrace;
        public int AssertCount => assertCount;
        public int FailCount => failCount;
        public int PassCount => passCount;
        public int SkipCount => skipCount;
        public int InconclusiveCount => inconclusiveCount;
        public bool HasChildren => hasChildren;
        public IEnumerable<ITestResult> Children => throw new NotImplementedException();
        public ITest Test => throw new NotImplementedException();
        public string Output => output;
    }

    class OnTestResultReceivedArgs
    {
        internal ITestResult TestResult { get; set; }
    }

    class OnTestRunFinishedArgs
    {
        internal ITestResult TestResult { get; set; }
    }
}
