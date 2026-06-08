namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Default naming strategy that uses the parameterized test name as the reference image root.
    /// </summary>
    internal class ParameterizedTestNameNamingStrategy : IReferenceImageNamingStrategy
    {
        public static readonly ParameterizedTestNameNamingStrategy Instance = new();

        private ParameterizedTestNameNamingStrategy() { }

        public IReferenceImageFileDescriptor CreateDescriptor(
            GraphicsTestCase rawCase,
            string parameterizedTestName,
            ImageExtension extension,
            TextureFormat format)
        {
            return new ReferenceImageFileDescriptor(parameterizedTestName, extension, format);
        }
    }
}
