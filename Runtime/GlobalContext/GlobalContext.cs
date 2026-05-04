using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// <para>
    /// Abstract base class for activatable platform nodes (mutable contexts).
    /// Extend this class to create a context that can be changed at runtime via <see cref="Activate"/>.
    /// </para>
    /// <para>
    /// Simple contexts with no side effects need no overrides:
    /// <c>public class MyContext : GlobalContext&lt;MyEnum&gt; { }</c>
    /// </para>
    /// </summary>
    /// <remarks>
    /// Contexts with custom activation logic override <see cref="Activate"/> and optionally <see cref="Current"/>:
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyContext : GlobalContext&lt;MyEnum&gt;
    /// {
    ///     public override Enum Current => ComputeState();
    ///     public override void Activate(Enum value) { /* custom logic */ }
    /// }
    /// </code>
    /// </example>
    /// <typeparam name="TEnum">The flags enum type representing the context values.</typeparam>
    public abstract class GlobalContext<TEnum> : IPlatformNode
        where TEnum : struct, Enum
    {
        TEnum m_Current;

        /// <inheritdoc/>
        public Type DataType => typeof(TEnum);

        /// <inheritdoc/>
        public virtual Enum Current => m_Current;

        /// <inheritdoc/>
        public virtual Enum Build => Current;

        /// <inheritdoc/>
        public virtual void Activate(Enum value)
        {
            if (value.GetType() != typeof(TEnum))
                throw new ArgumentException($"Invalid enum type. Expected {typeof(TEnum)}, but got {value.GetType()}.");
            m_Current = (TEnum)value;
        }
    }
}
