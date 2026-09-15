using System;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Marks a member of an <see cref="PlatformDimensionKind.Identity"/> dimension that names no
    /// machine — typically a "None"/"Unknown" sentinel reported when the dimension does not apply.
    /// A schema whose identity dimensions all report such a value has nothing to identify, so it
    /// resolves reference images after the configuration schemata rather than before them.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ElideFromPlatformPathAttribute"/> on purpose. That attribute decides
    /// whether a value is rendered into the reference-image path; this one decides whether the value
    /// says anything about what the machine is. A sentinel usually wants both, but they answer
    /// different questions and a dimension is free to carry one without the other.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class NotAnIdentityAttribute : Attribute
    {
    }
}
