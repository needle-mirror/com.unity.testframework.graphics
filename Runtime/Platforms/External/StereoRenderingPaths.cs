using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.TestTools.Graphics.Platforms
{
    /// <summary>
    /// Enumeration of stereo rendering paths.
    /// This is used to specify the stereo rendering paths supported by the platform.
    /// It contains options for multi-pass, single-pass, and instancing rendering paths.
    /// </summary>
    [Flags]
    public enum StereoRenderingPaths
    {
        /// <summary>
        /// No stereo rendering path.
        /// </summary>
        None = 0,

        /// <summary>
        /// Multi-pass stereo rendering path.
        /// </summary>
        MultiPass = 1 << 0,

        /// <summary>
        /// Single-pass stereo rendering path.
        /// </summary>
        SinglePass = 1 << 1,

        /// <summary>
        /// Instancing stereo rendering path.
        /// </summary>
        Instancing = 1 << 2,
    }

#if UNITY_EDITOR
    static class StereoRenderingPathExtensions
    {
        internal static StereoRenderingPaths ToStereoRenderingPaths(this StereoRenderingPath stereoRenderingPath)
        {
            switch (stereoRenderingPath)
            {
                case StereoRenderingPath.MultiPass:
                    return StereoRenderingPaths.MultiPass;
                case StereoRenderingPath.SinglePass:
                    return StereoRenderingPaths.SinglePass;
                case StereoRenderingPath.Instancing:
                    return StereoRenderingPaths.Instancing;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stereoRenderingPath), stereoRenderingPath, null);
            }
        }
    }
#endif
}
