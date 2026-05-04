using System.ComponentModel;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEngine.TestTools.Graphics.Tests")]
[assembly: InternalsVisibleTo("UnityEditor.TestTools.Graphics.Tests")]
[assembly: InternalsVisibleTo("Unity.TestProjects.Graphics.Tests.Runtime")]
[assembly: InternalsVisibleTo("Unity.TestProjects.Graphics.Tests.Editor")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}
