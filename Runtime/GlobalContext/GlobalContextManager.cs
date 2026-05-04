using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides access to activatable platform nodes (<see cref="GlobalContext{TEnum}"/>).
    /// Contexts are auto-discovered by <see cref="PlatformNodeRegistry"/> at startup.
    /// Use <see cref="Get{T}"/> to retrieve a context, then call <see cref="IPlatformNode.Activate"/>
    /// to change its state.
    /// </summary>
    public static class GlobalContextManager
    {
        /// <summary>
        /// Retrieves the singleton instance of the specified platform node type.
        /// The node must have been auto-discovered by <see cref="PlatformNodeRegistry"/>.
        /// </summary>
        /// <typeparam name="T">The concrete platform node type.</typeparam>
        /// <returns>The singleton instance, or null if the type was not discovered.</returns>
        public static T Get<T>() where T : class, IPlatformNode
        {
            return PlatformNodeRegistry.GetNode<T>();
        }

        /// <summary>
        /// Asserts that the specified context's current value equals the expected value.
        /// </summary>
        /// <typeparam name="T">The concrete platform node type.</typeparam>
        /// <param name="expected">The expected enum value.</param>
        public static void AssertContextIs<T>(Enum expected) where T : class, IPlatformNode
        {
            var node = Get<T>();
            Assert.That(node?.Current, Is.EqualTo(expected), $"Expected {expected} but was {node?.Current}");
        }

        #region Obsolete API

        [Obsolete("Use GlobalContextManager.Get<T>() instead. Registration is no longer needed.")]
        internal static readonly Dictionary<Type, IGlobalContextProvider> k_ContextProviders = new();

        /// <summary>
        /// Registers a global context of the specified type.
        /// </summary>
        /// <param name="contextType">The type of the context to register. Must implement <see cref="IGlobalContextProvider"/>.</param>
        /// <returns>The registered context provider instance.</returns>
        [Obsolete("Use GlobalContextManager.Get<T>() instead. Registration is no longer needed — nodes are auto-discovered.")]
        public static IGlobalContextProvider RegisterGlobalContext(Type contextType)
        {
            if (k_ContextProviders.TryGetValue(contextType, out var existing))
                return existing;

            if (!typeof(IGlobalContextProvider).IsAssignableFrom(contextType))
                throw new ArgumentException("Context type must implement IGlobalContextProvider");

            var context = Activator.CreateInstance(contextType) as IGlobalContextProvider;
            k_ContextProviders.Add(contextType, context);
            context.OnContextRegistered();
            return context;
        }

        /// <summary>
        /// Unregisters a global context of the specified type.
        /// </summary>
        /// <param name="contextType">The type of the context to unregister.</param>
        [Obsolete("Unregistration is no longer needed. Just restore previous state via Activate().")]
        public static void UnregisterGlobalContext(Type contextType)
        {
            if (!k_ContextProviders.TryGetValue(contextType, out var context))
                return;

            context.OnContextUnregistered();
            k_ContextProviders.Remove(contextType);
        }

        /// <summary>
        /// Retrieves the global context of the specified type.
        /// </summary>
        /// <typeparam name="TContext">The context provider type to retrieve.</typeparam>
        /// <returns>The context provider instance, or the default value if not registered.</returns>
        [Obsolete("Use GlobalContextManager.Get<T>() instead.")]
        public static TContext GetGlobalContext<TContext>()
            where TContext : IGlobalContextProvider
        {
            if (k_ContextProviders.TryGetValue(typeof(TContext), out var provider))
                return (TContext)provider;

            return default;
        }

        /// <summary>
        /// Checks if a global context of the specified type is registered.
        /// </summary>
        /// <param name="contextType">The type of the context to check.</param>
        /// <returns>True if a context of the specified type is registered; otherwise, false.</returns>
        [Obsolete("Registration is no longer needed. Use GlobalContextManager.Get<T>() != null to check if a node exists.")]
        public static bool IsGlobalContextRegistered(Type contextType)
        {
            return k_ContextProviders.ContainsKey(contextType);
        }

        /// <summary>
        /// Asserts that the context of the specified type matches the provided value.
        /// </summary>
        /// <typeparam name="TContext">The context provider type to check.</typeparam>
        /// <typeparam name="TValue">The enum type of the expected value.</typeparam>
        /// <param name="expected">The expected enum value.</param>
        [Obsolete("Use AssertContextIs<T>(Enum expected) instead — only one type parameter needed.")]
        public static void AssertContextIs<TContext, TValue>(TValue expected)
            where TContext : IGlobalContextProvider
            where TValue : Enum
        {
            var actual = GetGlobalContext<TContext>()?.Context ?? -1;
            var actualValue = (TValue)(actual as object);
            Assert.That(actualValue, Is.EqualTo(expected), $"Expected {expected} but was {actualValue}");
        }

        #endregion Obsolete API
    }
}
