using System;
using UnityEngine.TestTools.Graphics.Platforms;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Contributes <see cref="PlatformSchema"/>s to the build from anywhere in the project, packages
    /// included, so a package can ship reference images without editing the project's settings asset.
    /// </summary>
    [Serializable]
    [CreateAssetMenu(
        fileName = "GraphicsTestImageBuildSettings.asset",
        menuName = "Graphics Test Framework/Image Build Settings"
    )]
    public class GraphicsTestImageBuildSettings : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Platform schemata that define how to locate reference images for different platforms.")]
        PlatformSchema[] m_PlatformSchemata = Array.Empty<PlatformSchema>();

        /// <summary>
        /// Where to locate reference images. An empty asset contributes nothing and is never
        /// auto-populated with defaults.
        /// </summary>
        public PlatformSchema[] PlatformSchemata
        {
            get => m_PlatformSchemata;
            set => m_PlatformSchemata = value ?? Array.Empty<PlatformSchema>();
        }
    }
}
