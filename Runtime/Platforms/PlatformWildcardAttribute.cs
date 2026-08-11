using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Marks an enum value as a wildcard that matches any other value of its platform node.
    /// Platform combination expands it to every concrete value of the enum (excluding the
    /// default value and any other wildcards), so a single value can match all of them.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PlatformWildcardAttribute : Attribute
    {
    }
}
