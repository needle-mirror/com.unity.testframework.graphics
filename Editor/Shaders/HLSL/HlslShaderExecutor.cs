using System;
using System.Reflection.Emit;
using UnityEditor.TestTools.Graphics;
using UnityEngine.Rendering;
using static UnityEngine.Graphics;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class HlslShaderExecutor<T> : IShaderExecutor<T>
    {
        internal IAssetService AssetService { get; set; } = new AssetDatabaseService();

        static readonly int k_TestResult = Shader.PropertyToID("testResult");
        const int k_KernelIndex = 0;

        public T ExecuteShader(ShaderHandle handle)
        {
            var kernelName = handle.name;

            var computeShader = AssetService.LoadAssetAtPath<ComputeShader>(handle.relativePath);
            Debug.Assert(computeShader != null, $"Failed to load test shader {kernelName} from {handle.relativePath}");

            var cmd = new CommandBuffer { name = "ShaderTest" };
            var testResult = new ComputeBuffer(1, TypeSize<T>.k_Size);
            try
            {
                cmd.SetComputeBufferParam(computeShader, k_KernelIndex, k_TestResult, testResult);
                cmd.DispatchCompute(computeShader, k_KernelIndex, 1, 1, 1);
                ExecuteCommandBuffer(cmd);

                var result = new T[1];
                var request = AsyncGPUReadback.Request(testResult);
                request.WaitForCompletion();

                if (request.hasError)
                    throw new InvalidOperationException($"GPU readback failed for compute shader '{kernelName}' at '{handle.relativePath}'.");

                testResult.GetData(result);
                return result[0];
            }
            finally
            {
                testResult.Dispose();
                cmd.Release();
            }
        }

        // https://stackoverflow.com/questions/18167216/size-of-generic-structure
        static class TypeSize<TS>
        {
            internal static readonly int k_Size;

            static TypeSize()
            {
                var dm = new DynamicMethod("SizeOfType", typeof(int), new Type[] { });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Sizeof, typeof(TS));
                il.Emit(OpCodes.Ret);
                k_Size = (int)dm.Invoke(null, null);
            }
        }
    }
}
