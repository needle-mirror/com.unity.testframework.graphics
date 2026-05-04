using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Interface for creating new platform nodes for <see cref="Platforms.GraphicsTestPlatform"/>.
    /// Read-only nodes only need to implement <see cref="DataType"/> and <see cref="Current"/>.
    /// Activatable nodes (mutable contexts) should extend <see cref="GlobalContext{TEnum}"/> instead
    /// of implementing this interface directly.
    /// </summary>
    public interface IPlatformNode
    {
        /// <summary>
        /// The name of this platform node.
        /// </summary>
        string Name => DataType.Name;

        /// <summary>
        /// The data type that this node will use. This must be an Enum type.
        /// </summary>
        Type DataType { get; }

        /// <summary>
        /// Retrieves the current platform state at any given time.
        /// This will be used to determine the current platform state if this node is used.
        /// </summary>
        Enum Current { get; }

        /// <summary>
        /// Retrieves the current build platform state at any given time.
        /// This will be used to determine which platform state to build for if this node is used.
        /// </summary>
        Enum Build => Current;

        /// <summary>
        /// Activates the given context value. Override in mutable context nodes
        /// (via <see cref="GlobalContext{TEnum}"/>) to change platform state at runtime.
        /// Read-only nodes should not override this method.
        /// </summary>
        /// <param name="value">The enum value to activate.</param>
        void Activate(Enum value) { }
    }
}
