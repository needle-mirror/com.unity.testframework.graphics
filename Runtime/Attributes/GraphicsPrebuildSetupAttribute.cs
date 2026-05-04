using System;
using System.Collections.Generic;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute to mark a method or class as a pre-build setup step for graphics tests.
    /// This attribute should be used on a test class or test method.
    /// </summary>
    /// <remarks>
    /// This attribute will be used to run setup actions before building the graphics test project.
    /// The setup actions will be run in the order they are defined.
    /// The order is used to determine the order in which the pre-build steps are run.
    /// Lower numbers are run first.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public abstract class GraphicsPrebuildSetupAttribute : Attribute
    {
        /// <summary>
        /// The order in which to run the setup action.
        /// Lower numbers are run first.
        /// </summary>
        readonly int m_Order;

        internal SetupAction SetupAction => new(Setup, GetType(), m_Order);

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsPrebuildSetupAttribute"/> class.
        /// </summary>
        protected GraphicsPrebuildSetupAttribute() { }

        /// <summary>
        /// Creates a new instance of the <see cref="GraphicsPrebuildSetupAttribute"/> class.
        /// </summary>
        /// <param name="order">The order in which to run the setup action.</param>
        protected GraphicsPrebuildSetupAttribute(int order)
        {
            m_Order = order;
        }

        /// <summary>
        /// Override this method to implement the setup action.
        /// </summary>
        /// <remarks>
        /// This method will be called before building the graphics test project.
        /// </remarks>
        protected abstract void Setup();
    }

    /// <summary>
    /// Class to hold the setup action and its order.
    /// </summary>
    public record SetupAction
    {
        /// <summary>
        /// The action to be performed.
        /// </summary>
        public Action Action { get; set; }

        /// <summary>
        /// The order in which to run the action.
        /// Lower numbers are run first.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// The type that declared the action.
        /// This is used to identify the action in the setup process.
        /// </summary>
        public Type DeclaringType { get; set; }

        internal SetupAction(Action action, Type declaringType)
        {
            Action = action;
            DeclaringType = declaringType;
        }

        internal SetupAction(Action action, Type declaringType, int order)
        {
            Action = action;
            DeclaringType = declaringType;
            Order = order;
        }

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString() => DeclaringType.ToString();
    }

    class SetupActionEqualityComparer : IEqualityComparer<SetupAction>
    {
        public bool Equals(SetupAction a1, SetupAction a2)
        {
            return a1?.DeclaringType == a2?.DeclaringType;
        }

        public int GetHashCode(SetupAction action) => action?.DeclaringType?.GetHashCode() ?? 0;
    }
}
