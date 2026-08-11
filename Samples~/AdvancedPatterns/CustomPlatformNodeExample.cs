using System;
using UnityEngine;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Platforms;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GraphicsTestFrameworkProject.Samples.AdvancedPatterns
{
    /*
    -- Tutorial [1] --
    This sample demonstrates how to implement a read-only IPlatformNode.

    IPlatformNode extends GraphicsTestPlatform with new dimensions that the framework
    uses for:
      - Reference image path resolution (each node value becomes a path segment)
      - Platform-specific test filtering (IgnoreGraphicsTest, TestNotSupportedOn, etc.)
      - Test result organization and reporting

    Nodes are auto-discovered by PlatformNodeRegistry via reflection at startup.
    Any non-abstract class implementing IPlatformNode in a loaded assembly is registered.

    Read-only nodes (IPlatformNode) report state but cannot change it.
    Mutable nodes (GlobalContext<TEnum>) can also activate different states.
    Choose IPlatformNode when the value is determined by the environment and not
    changeable by tests (e.g., GPU vendor, OS version, CPU architecture).
    */

    /*
    -- Tutorial [2] --
    Step 1: Define an enum representing the dimension's possible values.
    This enum becomes the DataType of the node and a segment in reference image paths.

    Mark a sentinel value that means "absent" or "not applicable" (here, Unknown) with
    [ElideFromPlatformPath] so it does not add a segment to reference image paths. The value
    still participates in platform equality, GetValue<T>(), and platform filtering; only the
    folder segment is omitted. This keeps the common case out of a redundant subfolder
    (for example ".../Direct3D11" instead of ".../Direct3D11/Unknown").

    To read the full path including elided segments (for example when debugging), use
    GraphicsTestPlatform.ResultsPathWithElided or AllResultsPathsWithElided.
    */

    public enum GpuMemoryTier
    {
        [ElideFromPlatformPath]
        Unknown = 0,
        Low = 1,
        Medium = 2,
        High = 3,

        // Wildcard value: in a filter attribute this matches any concrete tier (Low, Medium,
        // or High). See Tutorial [7]. A node never reports this as its Current value.
        [PlatformWildcard]
        Any = 4,
    }

    /*
    -- Tutorial [3] --
    Step 2: Implement IPlatformNode.
    At minimum, provide DataType and Current. The Name property defaults to the
    enum type name (here, "GpuMemoryTier").

    Current is queried at runtime to determine the active platform state.
    Build is queried at Editor time to determine which platform to build for;
    it defaults to Current but can be overridden for Editor vs Player differences.
    */

    public class GpuMemoryTierNode : IPlatformNode
    {
        const int k_LowMemoryThresholdMb = 2048;
        const int k_HighMemoryThresholdMb = 8192;

        public Type DataType { get; } = typeof(GpuMemoryTier);

        public Enum Current => ClassifyGpuMemory(SystemInfo.graphicsMemorySize);

        /*
        -- Tutorial [4] --
        Override Build to provide Editor-specific logic.
        In the Editor, the build target may differ from the current machine.
        For read-only environmental nodes like GPU memory, Build typically
        equals Current since we can only detect the local GPU.
        For nodes that depend on build settings (like graphics API), you would
        query PlayerSettings or EditorUserBuildSettings here.
        */

#if UNITY_EDITOR
        public Enum Build => Current;
#endif

        static GpuMemoryTier ClassifyGpuMemory(int graphicsMemorySizeMb)
        {
            if (graphicsMemorySizeMb <= 0)
                return GpuMemoryTier.Unknown;
            if (graphicsMemorySizeMb < k_LowMemoryThresholdMb)
                return GpuMemoryTier.Low;
            if (graphicsMemorySizeMb < k_HighMemoryThresholdMb)
                return GpuMemoryTier.Medium;
            return GpuMemoryTier.High;
        }
    }

    /*
    -- Tutorial [5] --
    Another example: a node that detects a project-specific feature flag.
    This could be driven by a command-line argument, scripting define, or
    ScriptableObject configuration.

    The Activate method has a default empty implementation on IPlatformNode,
    so read-only nodes don't need to override it.
    */

    public enum CustomFeatureFlag
    {
        Disabled = 0,
        Enabled = 1,
    }

    public class CustomFeatureFlagNode : IPlatformNode
    {
        const string k_CommandLineArg = "-enable-custom-feature";

        public Type DataType { get; } = typeof(CustomFeatureFlag);

        public Enum Current => IsFeatureEnabled()
            ? CustomFeatureFlag.Enabled
            : CustomFeatureFlag.Disabled;

        static bool IsFeatureEnabled()
        {
            var args = Environment.GetCommandLineArgs();
            foreach (var arg in args)
            {
                if (arg.Equals(k_CommandLineArg, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    /*
    -- Tutorial [6] --
    Once registered, the new node's enum values can be used in platform filter attributes:

      [TestNotSupportedOn("", "Requires high GPU memory", GpuMemoryTier.Low)]
      [IgnoreGraphicsTest("HeavyScene", "Flaky on low memory GPUs", GpuMemoryTier.Low)]

    The node also becomes part of the reference image path.
    For example, if the schema includes GpuMemoryTier:
      ReferenceImages/Linear/WindowsEditor/Direct3D11/High/TestName.png

    No additional registration code is needed. PlatformNodeRegistry discovers
    all IPlatformNode implementations at startup via AppDomain.CurrentDomain.GetAssemblies().
    */

    /*
    -- Tutorial [7] --
    A node value can be flagged with [PlatformWildcard] to act as a "match any" value for its
    dimension. During platform combination the framework expands the wildcard to every concrete
    value of the enum, excluding the default (0) value and any other wildcards.

    GpuMemoryTier.Any (defined above) expands to { Low, Medium, High }, so this single ignore:

      [IgnoreGraphicsTest("HeavyScene", "Flaky on any GPU with detectable memory. JIRA-1234", GpuMemoryTier.Any)]

    is equivalent to listing Low, Medium, and High explicitly - and automatically covers any
    new tier added to the enum later, without editing the attribute.

    A wildcard is only meaningful in filter attributes: a node never returns it from Current, so
    it never appears in a reference image path. This is how, for example, a single ignore can
    apply across every XR loader regardless of which one is active.
    */
}
