using System.Collections;

namespace UnityEngine.TestTools.Graphics
{
    /// <summary>
    /// Provides helpers to ensure every shader variant needed by a scene is
    /// compiled and uploaded to the GPU before an image-comparison capture.
    /// In the Editor the helpers temporarily enable asynchronous shader
    /// compilation, yield frames so the driver can finish in the background,
    /// then restore the original settings.  In a Player build every method is
    /// a no-op because all variants are pre-compiled at build time.
    /// </summary>
    public static class ShaderWarmup
    {
        const int k_DefaultWarmupFrames = 1;

        const int k_DefaultTimeoutFrames = 300;

        /// <summary>
        /// Yields frames until every in-flight shader compilation task has
        /// finished.  In the Editor the method temporarily enables
        /// <c>ShaderUtil.allowAsyncCompilation</c> and
        /// <c>EditorSettings.asyncShaderCompilation</c> so variants that are
        /// triggered during the warm-up frames compile on background threads
        /// rather than blocking the render thread one by one.
        /// Call this from any test coroutine before
        /// <see cref="ImageAssert.AreEqual(Texture2D,Camera,ImageComparisonSettings,string,bool)"/>
        /// to eliminate image differences caused by shader compilation timing.
        /// </summary>
        /// <param name="warmupFrames">
        /// Number of frames to render before polling the compiler.  These
        /// frames give the render pipeline a chance to touch every pass and
        /// trigger compilation for all the shader variants the scene needs.
        /// </param>
        /// <param name="timeoutFrames">
        /// Maximum number of additional frames to wait while the compiler is
        /// still busy.  A warning is logged if the timeout is reached.
        /// </param>
        /// <returns>An enumerator suitable for <c>yield return</c>.</returns>
        public static IEnumerator WaitForCompilation(
            int warmupFrames = k_DefaultWarmupFrames,
            int timeoutFrames = k_DefaultTimeoutFrames)
        {
#if UNITY_EDITOR
            var savedAllowAsync = UnityEditor.ShaderUtil.allowAsyncCompilation;
            var savedEditorAsync = UnityEditor.EditorSettings.asyncShaderCompilation;

            UnityEditor.ShaderUtil.allowAsyncCompilation = true;
            UnityEditor.EditorSettings.asyncShaderCompilation = true;

            for (var i = 0; i < warmupFrames; i++)
                yield return new WaitForEndOfFrame();

            var waited = 0;
            while (UnityEditor.ShaderUtil.anythingCompiling && waited++ < timeoutFrames)
                yield return new WaitForEndOfFrame();

            if (UnityEditor.ShaderUtil.anythingCompiling)
                GraphicsTestLogger.Log(LogType.Warning,
                    $"ShaderWarmup: shader compilation did not finish within {timeoutFrames} frames.");

            UnityEditor.ShaderUtil.allowAsyncCompilation = savedAllowAsync;
            UnityEditor.EditorSettings.asyncShaderCompilation = savedEditorAsync;
#else
            yield break;
#endif
        }
    }
}
