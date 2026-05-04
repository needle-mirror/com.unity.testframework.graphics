using System;
using UnityEditor.TestTools.Graphics.UI;

namespace UnityEditor.TestTools.Graphics.Services
{
    [InitializeOnLoad]
    static class LinkWatcher
    {
        static readonly Action<EditorWindow, HyperLinkClickedEventArgs> s_Handler = OnHyperLinkClicked;

        static LinkWatcher()
        {
            EditorGUI.hyperLinkClicked += s_Handler;
            AssemblyReloadEvents.beforeAssemblyReload += Unregister;
        }

        static void Unregister()
        {
            EditorGUI.hyperLinkClicked -= s_Handler;
            AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
        }

        static void OnHyperLinkClicked(EditorWindow window, HyperLinkClickedEventArgs args)
        {
            if (args.hyperLinkData.TryGetValue("gtf", out var value))
            {
                var wnd = GraphicsTestsWindow.CreateOrShowWindow();
                wnd?.SelectTest(value);
            }
        }
    }
}
