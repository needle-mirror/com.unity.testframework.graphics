using UnityEngine.Rendering;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class ShaderlabShaderExecutor<T> : IShaderExecutor<T>
        where T : ShaderlabShaderData
    {
        const int k_RenderTextureWidth = 256;
        const int k_RenderTextureHeight = 256;
        const int k_RenderTextureDepthBits = 16;
        public T ExecuteShader(ShaderHandle shader)
        {
            if (typeof(T) == typeof(ShaderlabShaderData))
                return (T)ExecuteShaderInternal(shader);
            Debug.LogWarning($"ShaderlabShaderExecutor does not support type {typeof(T).Name}");
            return null;
        }

        ShaderlabShaderData ExecuteShaderInternal(ShaderHandle shader)
        {
            var shaderlabShader = Shader.Find(shader.relativePath);
            Debug.Assert(shaderlabShader != null, $"Failed to load test shader {shader.path} from {shader.path}");

            var go = MeshGenerator.GenerateQuad(shaderlabShader);
            var quad = go.GetComponent<MeshFilter>().sharedMesh;
            var vertices = quad.vertices;

            var cameraGameObject = new GameObject("Main Camera");
            var renderTexture = RenderTexture.GetTemporary(k_RenderTextureWidth, k_RenderTextureHeight, k_RenderTextureDepthBits, RenderTextureFormat.ARGB32);
            var texture = new Texture2D(renderTexture.width, renderTexture.height);

            try
            {
                var cam = cameraGameObject.AddComponent<Camera>();

                cam.orthographic = true;
                cam.orthographicSize = quad.bounds.extents.x;
                cam.transform.position = new Vector3(
                    cam.transform.position.x,
                    cam.transform.position.y,
                    cam.transform.position.z - 1
                );
                cam.clearFlags = CameraClearFlags.Nothing;

                cam.targetTexture = renderTexture;
                cam.Render();

                var request = AsyncGPUReadback.Request(renderTexture);
                request.WaitForCompletion();

                if (!request.hasError)
                {
                    var data = request.GetData<byte>();
                    texture.SetPixelData(data, 0);
                    texture.Apply();
                    cam.targetTexture = null;
                }
                else
                {
                    Debug.LogError("GPU readback error occurred.");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(cameraGameObject);
                Object.DestroyImmediate(quad);
                RenderTexture.ReleaseTemporary(renderTexture);
                RenderTexture.active = null;
            }

            return new ShaderlabShaderData(texture, vertices);
        }
    }
}
