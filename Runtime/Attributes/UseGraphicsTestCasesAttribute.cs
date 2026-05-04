using System;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Attribute to specify that a test should use graphics test cases.
    /// </summary>
    /// <remarks>
    /// This attribute is obsolete and will be removed in a future version. Use the SceneGraphicsTestAttribute(scenePaths) (where scenePaths is a list of direct paths to scenes or directories of scenes). Then change GraphicsTestCase to SceneGraphicsTestCase (which will provide testCase.ScenePath) instead.
    /// </remarks>
    [Obsolete(
        "Use the SceneGraphicsTestAttribute(scenePaths) (where scenePaths is a list of direct paths to scenes or directories of scenes). Then change GraphicsTestCase to SceneGraphicsTestCase (which will provide testCase.ScenePath) instead."
    )]
    public class UseGraphicsTestCasesAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the <see cref="UseGraphicsTestCasesAttribute"/> class.
        /// </summary>
        public UseGraphicsTestCasesAttribute()
        {
            throw new NotSupportedException(
                "UseGraphicsTestCasesAttribute is obsolete. Use SceneGraphicsTestAttribute instead."
            );
        }

        /// <summary>
        /// Creates a new instance of the <see cref="UseGraphicsTestCasesAttribute"/> class.
        /// </summary>
        /// <param name="referenceImagePath">
        /// The path to the reference image. This path is relative to the project folder and must be in a path that is indexed by the Unity Asset Database.
        /// </param>
        public UseGraphicsTestCasesAttribute(string referenceImagePath)
        {
            throw new NotSupportedException(
                "UseGraphicsTestCasesAttribute is obsolete. Use SceneGraphicsTestAttribute instead."
            );
        }

        /// <summary>
        /// The current colorspace.
        /// </summary>
        [Obsolete("Use GraphicsTestPlatform.Current.ColorSpace instead.")]
        public static ColorSpace ColorSpace => GraphicsTestPlatform.Current.GetValue<ColorSpace>();

        /// <summary>
        /// The current runtime platform.
        /// </summary>
        [Obsolete("Use GraphicsTestPlatform.Current.Platform instead.")]
        public static RuntimePlatform Platform => GraphicsTestPlatform.Current.GetValue<RuntimePlatform>();

        /// <summary>
        /// The current graphics device type.
        /// </summary>
        [Obsolete("Use GraphicsTestPlatform.Current.GraphicsDevice instead.")]
        public static GraphicsDeviceType GraphicsDevice => GraphicsTestPlatform.Current.GetValue<GraphicsDeviceType>();

        /// <summary>
        /// The current xr device.
        /// </summary>
        [Obsolete("Use GraphicsTestPlatform.Current.XrDevice instead.")]
        public static string LoadedXRDevice => GraphicsTestPlatform.Current.GetValue<XrDevice>().ToString();
    }
}
