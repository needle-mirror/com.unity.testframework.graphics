using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Unity's command-line arguments that force how a run renders, and the value each one forces. The
    /// Editor's build-time check and <see cref="VerifyRunConfigurationAttribute"/> both read them.
    /// </summary>
    static class ForcedGraphicsArguments
    {
        // Graphics API arguments, which force the API of the Editor and of players alike, except on platforms
        // whose players ignore them (ENABLE_FORCE_GFX_RENDERER). Mapping mirrors ParseGfxDeviceArgs in
        // Runtime/GfxDevice/GfxDeviceSetup.cpp, including the aliases and versioned forms.
        internal static readonly Dictionary<string, GraphicsDeviceType> k_GraphicsApiArguments = new()
        {
            { "-force-d3d11", GraphicsDeviceType.Direct3D11 },
            { "-force-d3d12", GraphicsDeviceType.Direct3D12 },
            { "-force-vulkan", GraphicsDeviceType.Vulkan },
            { "-force-metal", GraphicsDeviceType.Metal },
            { "-force-glcore", GraphicsDeviceType.OpenGLCore },
            { "-force-opengl", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore32", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore33", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore40", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore41", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore42", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore43", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore44", GraphicsDeviceType.OpenGLCore },
            { "-force-glcore45", GraphicsDeviceType.OpenGLCore },
            { "-force-gles", GraphicsDeviceType.OpenGLES3 },
            { "-force-gles20", GraphicsDeviceType.OpenGLES3 },
            { "-force-gles30", GraphicsDeviceType.OpenGLES3 },
            { "-force-gles31", GraphicsDeviceType.OpenGLES3 },
            { "-force-gles31aep", GraphicsDeviceType.OpenGLES3 },
            { "-force-gles32", GraphicsDeviceType.OpenGLES3 },
        };

        // On Xbox consoles, -force-d3d12 gives the console's own Direct3D 12 device, which reports another type.
        static readonly GraphicsDeviceType[] k_XboxD3D12Devices =
        {
            GraphicsDeviceType.XboxOneD3D12,
            GraphicsDeviceType.GameCoreXboxOne,
            GraphicsDeviceType.GameCoreXboxSeries,
        };

        /// <summary>
        /// Whether a device of type <paramref name="deviceType"/> is what the graphics API
        /// <paramref name="argument"/> asks for.
        /// </summary>
        internal static bool IsAskedBy(string argument, GraphicsDeviceType deviceType) =>
            k_GraphicsApiArguments[argument] == deviceType
            || (argument == "-force-d3d12" && Array.IndexOf(k_XboxD3D12Devices, deviceType) >= 0);

        // Bool-style threading-mode flags. Mapping mirrors CalculateGfxDeviceThreadingMode
        // in Runtime/GfxDevice/GfxDeviceSetup.cpp.
        internal static readonly Dictionary<string, RenderingThreadingMode> k_ThreadingArguments = new()
        {
            { "-force-gfx-direct", RenderingThreadingMode.Direct },
            { "-force-gfx-st", RenderingThreadingMode.SingleThreaded },
            { "-force-gfx-mt", RenderingThreadingMode.MultiThreaded },
        };

        // Value-style flag: -force-gfx-jobs <value>. Mapping mirrors CalculateGfxDeviceThreadingMode
        // in Runtime/GfxDevice/GfxDeviceSetup.cpp.
        internal const string k_GfxJobsArgument = "-force-gfx-jobs";
        internal static readonly Dictionary<string, RenderingThreadingMode> k_GfxJobsValues =
            new(StringComparer.OrdinalIgnoreCase)
        {
            { "native", RenderingThreadingMode.NativeGraphicsJobs },
            { "legacy", RenderingThreadingMode.LegacyJobified },
            { "split", RenderingThreadingMode.NativeGraphicsJobsSplitThreading },
            { "off", RenderingThreadingMode.MultiThreaded },
        };

        // Asks Direct3D 11 and 12 for WARP, the software rasterizer, instead of a GPU.
        internal const string k_WarpArgument = "-force-driver-type-warp";

        /// <summary>
        /// Every threading-mode argument, the flags and <c>-force-gfx-jobs</c>.
        /// </summary>
        internal static IReadOnlyList<string> ThreadingArgumentNames =>
            new List<string>(k_ThreadingArguments.Keys) { k_GfxJobsArgument };

        /// <summary>
        /// The arguments on <paramref name="commandLine"/> that write one of <paramref name="names"/> in a form
        /// Unity doesn't document, each with the documented form to write instead: a double dash, as in
        /// <c>--force-d3d12</c>, or a value joined with <c>=</c>, as in <c>-force-gfx-jobs=split</c>.
        /// </summary>
        /// <remarks>
        /// The manual documents <c>-name value</c>. The engine reads the other forms for some arguments only: the
        /// parser behind the graphics API and WARP arguments accepts a double dash, the boot.config parser behind
        /// the threading arguments doesn't, and neither accepts <c>=</c>. Rejecting them keeps what the framework
        /// reads the same as what the engine applies.
        /// </remarks>
        internal static List<(string Found, string Documented)> FindUndocumentedForms(
            CommandLineReader commandLine, IEnumerable<string> names)
        {
            var found = new List<(string, string)>();
            var arguments = commandLine.GetRawArguments();
            if (arguments == null)
                return found;

            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("-"))
                    continue;

                var doubleDash = argument.StartsWith("--");
                var body = argument.Substring(doubleDash ? 2 : 1);
                var equals = body.IndexOf('=');
                if (!doubleDash && equals < 0)
                    continue;

                var key = equals < 0 ? body : body.Substring(0, equals);
                foreach (var name in names)
                {
                    if (!string.Equals(key, name.Substring(1), StringComparison.OrdinalIgnoreCase))
                        continue;

                    var documented = equals < 0 ? name : $"{name} {body.Substring(equals + 1)}".TrimEnd();
                    found.Add((argument, documented));
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Says what <see cref="FindUndocumentedForms"/> found, and what to write instead.
        /// </summary>
        internal static string DescribeUndocumentedForms(IEnumerable<(string Found, string Documented)> forms)
        {
            var sentences = new List<string>();
            foreach (var (found, documented) in forms)
                sentences.Add($"{found} isn't the documented form of the argument, and the engine ignores such forms "
                    + $"for some arguments. Write {documented} instead.");
            return string.Join(" ", sentences);
        }
    }
}
