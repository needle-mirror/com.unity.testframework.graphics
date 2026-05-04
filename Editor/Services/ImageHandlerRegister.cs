using UnityEditor.Networking.PlayerConnection;
using UnityEngine.TestTools.Graphics;

namespace UnityEditor.TestTools.Graphics
{
    [InitializeOnLoad]
    static class ImageHandlerRegister
    {
        static ImageHandlerRegister()
        {
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(ImageMessage.MessageId, ImageHandler.instance.HandleImageEvent);

            AssemblyReloadEvents.beforeAssemblyReload += Unregister;
        }

        static void Unregister()
        {
            EditorConnection.instance.Unregister(ImageMessage.MessageId, ImageHandler.instance.HandleImageEvent);
            AssemblyReloadEvents.beforeAssemblyReload -= Unregister;
        }
    }
}
