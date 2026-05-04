namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class is used to specify the settings for image comparison tests.
    /// It contains an instance of <see cref="ImageComparisonSettings"/> that specifies the settings for image comparison.
    /// </summary>
    public class GraphicsTestSettings : MonoBehaviour
    {
        /// <summary>
        /// The settings for image comparison tests.
        /// This includes settings for the image comparison, such as the tolerance and the output format.
        /// </summary>
        public ImageComparisonSettings ImageComparisonSettings = new ImageComparisonSettings();
    }
}
