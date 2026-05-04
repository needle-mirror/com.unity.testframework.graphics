#if UNITY_6000_6_OR_NEWER
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    static class D3D12ValidationInitializer
    {
        static bool s_Registered;
        static readonly D3D12ValidationWrapper s_Wrapper = new D3D12ValidationWrapper();

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
