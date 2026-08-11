using System;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Marks a platform-dimension enum member (typically a "None"/"Unknown" sentinel) so it
    /// does not contribute a segment to the reference-image <see cref="PlatformPath"/>.
    /// The value still participates in platform equality and <see cref="GraphicsTestPlatform.GetValue{T}"/>;
    /// it is simply elided from the output folder tree, mirroring how
    /// <see cref="System.Runtime.InteropServices.Architecture"/> is handled.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ElideFromPlatformPathAttribute : Attribute
    {
    }
}
