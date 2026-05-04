namespace UnityEngine.TestTools.Graphics.Shaders
{
    /// <summary>
    /// Interface for shader executors
    /// </summary>
    /// <typeparam name="T">
    /// The type of the shader result data
    /// </typeparam>
    public interface IShaderExecutor<out T>
    {
        /// <summary>
        /// Executes the given shader and returns the result
        /// </summary>
        /// <param name="shader">
        /// The handle to the shader to execute
        /// </param>
        /// <returns>
        /// The shader execution result data
        /// </returns>
        T ExecuteShader(ShaderHandle shader);
    }
}
