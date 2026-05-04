using System;
using UnityEngine.Serialization;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Settings to control how the camera capture frames.
    /// </summary>
    [Serializable]
    public class CameraCaptureSettings
    {
        /// <summary>
        /// Width of the texture2D that contains the captured frame
        /// </summary>
        public int targetWidth = 1920;

        /// <summary>
        /// Height of the texture2D that contains the captured frame
        /// </summary>
        public int targetHeight = 1080;

        /// <summary>
        /// Whether the default HDR format of the platforms is used for the camera RenderTexture. The final texture is blitted into the LDR format.
        /// </summary>
        public bool useHDR;

        /// <summary>
        /// The number of samples taken for smoothing.
        /// </summary>
        public int msaaSamples = 1;
    }
}
