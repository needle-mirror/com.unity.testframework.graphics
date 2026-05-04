using UnityEngine.Serialization;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class is used to specify the custom resolution settings for a test.
    /// </summary>
    [System.Serializable]
    public class CustomResolutionFields
    {
        /// <summary>
        /// The platform for which the resolution is set.
        /// </summary>
        [FormerlySerializedAs("Platform")]
        public RuntimePlatform platform;

        /// <summary>
        /// The resolution width.
        /// </summary>
        [FormerlySerializedAs("Width")]
        public int width = 1920;

        /// <summary>
        /// The resolution height.
        /// </summary>
        [FormerlySerializedAs("Height")]
        public int height = 1080;

        /// <summary>
        /// Whether the resolution is fullscreen or not.
        /// </summary>
        public bool isFullScreen = true;
    }
}
