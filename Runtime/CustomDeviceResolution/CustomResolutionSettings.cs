namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// This class is used to specify the custom resolution settings for a test.
    /// It contains an array of <see cref="CustomResolutionFields"/> that specify the resolution settings for different nodes.
    /// </summary>
    [System.Serializable]
    [CreateAssetMenu(
        fileName = "Custom Resolutions",
        menuName = "Graphics Test Framework/Custom Resolutions",
        order = 100
    )]
    public class CustomResolutionSettings : ScriptableObject
    {
        /// <summary>
        /// The array of custom resolution fields.
        /// Each field specifies the resolution settings for a specific platform.
        /// </summary>
        public CustomResolutionFields[] fields;
    }
}
