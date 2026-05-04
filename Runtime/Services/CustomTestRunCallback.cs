using System;
using System.Reflection;
using NUnit.Framework.Interfaces;
using UnityEngine.TestRunner;

// SRPs now control screen space UI overlays rendering by default, for all types of UI (IMGUI, uGUI, UIToolkit).
// UTF-generated scene contains a IMGUI component whose OnGUI() callback triggers dynamic mem alloc while running SRPs tests,
// therefore creating false negatives when we check memory allocation in SRPs.
// To prevent that, this script temporarily disables this IMGUI component (no more OnGUI()) while running SRP tests
// to re-enable it only at the end of the test run to correctly display the test results.
[assembly: TestRunCallback(typeof(UnityEngine.TestTools.Graphics.CustomTestRunCallback))]

namespace UnityEngine.TestTools.Graphics
{
    class CustomTestRunCallback : ITestRunCallback
    {
        readonly ShaderWarningTestRunCallback m_ShaderWarningCallback = new();

        // Retrieve through reflection UTF types and method
        static readonly Type k_UTFPlayModeTestControllerType = Type.GetType(
            "UnityEngine.TestTools.TestRunner.PlaymodeTestsController, UnityEngine.TestRunner, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        ); // type: PlayModeRunner
        static readonly Type k_UTFPlayModeCallbackRunnerType = Type.GetType(
            "UnityEngine.TestTools.TestRunner.Callbacks.PlayModeRunnerCallback, UnityEngine.TestRunner, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        ); // type: PlayModeRunnerCallback
        static readonly MethodInfo k_UTFIsControllerOnSceneMethodInfo = k_UTFPlayModeTestControllerType?.GetMethod(
            "IsControllerOnScene",
            BindingFlags.Static | BindingFlags.NonPublic
        ); // type: IsControllerOnScene
        static readonly MethodInfo k_UTFGetControllerMethodInfo = k_UTFPlayModeTestControllerType?.GetMethod(
            "GetController",
            BindingFlags.Static | BindingFlags.NonPublic
        ); // type: GetController

        void EnableOnGUICallbackInUTF(bool enable)
        {
            // Invoke static IsControllerOnScene method to check whether UTF PlayModeTestsController exists (PlayMode, Standalone)
            var isControllerOnScene = (bool)k_UTFIsControllerOnSceneMethodInfo?.Invoke(null, null);

            if (!isControllerOnScene)
                return;
            // Invoke static GetController method to obtain existing UTF PlayModeTestsController
            var playModeTestControllerObj = k_UTFGetControllerMethodInfo?.Invoke(null, null);

            if (playModeTestControllerObj is not MonoBehaviour monoBehaviour || k_UTFPlayModeCallbackRunnerType == null)
                return;
            // Retrieve IMGUI component from UTF PlayModeTestController
            var imguiComponent = (MonoBehaviour)monoBehaviour.GetComponent(k_UTFPlayModeCallbackRunnerType);

            if (imguiComponent != null)
            {
                imguiComponent.enabled = enable;
            }
        }

        // UTF run is about to start, disabling OnGUI() to prevent mem alloc in render loop
        public void RunStarted(ITest testsToRun)
        {
            EnableOnGUICallbackInUTF(false);
            m_ShaderWarningCallback.RunStarted(testsToRun);
        }

        // UTF run has just finished, enabling OnGUI() back to display test results
        public void RunFinished(ITestResult testResults)
        {
            EnableOnGUICallbackInUTF(true);
            m_ShaderWarningCallback.RunFinished(testResults);
        }

        public void TestStarted(ITest test) { }

        public void TestFinished(ITestResult result) { }
    }
}
