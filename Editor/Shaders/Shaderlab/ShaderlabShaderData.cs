namespace UnityEngine.TestTools.Graphics.Shaders
{
    /// <summary>
    /// Data structure to hold Shaderlab shader execution results
    /// </summary>
    public class ShaderlabShaderData
    {
        /// <summary>
        /// Fragment shader output as Texture2D
        /// </summary>
        public readonly Texture2D Fragment;

        /// <summary>
        /// Vertex shader output as array of Vector3
        /// </summary>
        public Vector3[] Vertex;

        /// <summary>
        /// Creates a new ShaderlabShaderData instance
        /// </summary>
        /// <param name="fragment">
        /// Fragment shader output as Texture2D
        /// </param>
        /// <param name="vertex">
        /// Vertex shader output as array of Vector3
        /// </param>
        public ShaderlabShaderData(Texture2D fragment, Vector3[] vertex)
        {
            Fragment = fragment;
            Vertex = vertex;
        }
    }
}
