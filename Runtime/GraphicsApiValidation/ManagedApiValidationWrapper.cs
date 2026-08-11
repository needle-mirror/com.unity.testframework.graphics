#if UNITY_6000_6_OR_NEWER
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using UnityEngine.TestRunner.NUnitExtensions.Runner;

namespace UnityEngine.TestTools.Graphics.GraphicsApiValidation
{
    internal class ManagedApiValidationWrapper : ITestCommandWrapper
    {
        public int Order => 1000;

        public bool ShouldWrap(TestMethod test) => ManagedApiValidationCommand.IsEnabled;

        public TestCommand Wrap(TestCommand command) => new ManagedApiValidationCommand(command);
    }
}
#endif
