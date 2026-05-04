using System;
using System.Runtime.InteropServices;

namespace UnityEngine.TestTools.Graphics.Platforms
{
    class ArchitectureNode : IPlatformNode
    {
        static TestSettingsReader TestSettingsReader { get; set; } = new();
        public Type DataType { get; } = typeof(Architecture);
        public Enum Current
        {
            get
            {
                var currentArchitecture = RuntimeInformation.OSArchitecture;

                // Apple M1 processor is ARM64 but does not appear as such in RuntimeInformation.OSArchitecture on some testing environments
                if (
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    && SystemInfo.processorType.Contains("Apple M", StringComparison.InvariantCultureIgnoreCase)
                )
                {
                    currentArchitecture = Architecture.Arm64;
                }

                return currentArchitecture;
            }
        }
#if UNITY_EDITOR
        public Enum Build => TestSettingsReader.TryGetTestSettings()?.Architecture ?? Current;
#else
        public Enum Build => Current;
#endif
    }
}
