#if UNITY_6000_6_OR_NEWER
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    static class ManagedApiValidationInitializer
    {
        static bool s_Registered;
        static readonly ManagedApiValidationWrapper s_Wrapper = new ManagedApiValidationWrapper();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterRuntime() => Register();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void RegisterEditor() => Register();
#endif

        static void Register()
        {
            if (s_Registered)
                return;

            TestCommandWrapperRegistry.Register(s_Wrapper);
            s_Registered = true;
        }
    }
}
#endif
