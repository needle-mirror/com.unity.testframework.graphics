using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.TestTools.Graphics")]
[assembly: InternalsVisibleTo("UnityEngine.TestTools.Graphics.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.TestTools.Graphics.Tests")]
[assembly: InternalsVisibleTo("UnityEngine.TestTools.Graphics.Contexts")]
[assembly: InternalsVisibleTo("Unity.Testing.VisualEffectGraph.PerformanceRuntimeTests")]
[assembly: InternalsVisibleTo("Unity.TestProjects.Graphics.Tests.Runtime")]
[assembly: InternalsVisibleTo("Unity.TestProjects.Graphics.Tests.Editor")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")] // For Moq

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
