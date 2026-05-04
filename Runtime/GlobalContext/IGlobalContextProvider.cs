using System;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Interface for providing a global context. This is used to provide a context that is shared across all tests.
    /// </summary>
    [Obsolete("Extend GlobalContext<TEnum> instead. See IPlatformNode.Activate for the new activation pattern.")]
    public interface IGlobalContextProvider
    {
        /// <summary>
        /// The context value. This should be the value of some enum or other constant that represents the context. Cast to an int.
        /// </summary>
        int Context { get; }

        /// <summary>
        /// The type of the context value.
        /// </summary>
        Type ContextType { get; }

        /// <summary>
        /// Activates the given context value.
        /// </summary>
        /// <param name="context">
        /// The context value. This should be the value of some enum or other constant that represents the context. Cast to an int.
        /// </param>
        void ActivateContext(int context);

        /// <summary>
        /// An event that is called when the context is registered.
        /// </summary>
        void OnContextRegistered();

        /// <summary>
        /// An event that is called when the context is unregistered.
        /// </summary>
        void OnContextUnregistered();
    }
}
