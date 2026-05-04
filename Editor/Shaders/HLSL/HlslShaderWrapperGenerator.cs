using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.TestTools.Graphics.Shaders
{
    class HlslShaderWrapperGenerator : ShaderWrapperGenerator
    {
        readonly Type m_Type;

        public HlslShaderWrapperGenerator(ShaderHandle shaderHandle, Type type, string act)
            : base(shaderHandle, act)
        {
            m_Type = type;
        }

        string GenerateProperties()
        {
            if (ShaderProperties.Count == 0)
            {
                return string.Empty;
            }

            var lines = new List<string>();
            foreach (var p in ShaderProperties)
                lines.Add($"#define {p.Key} {p.Value}");
            return string.Join("\n", lines);
        }

        string GenerateDependencies()
        {
            if (Dependencies.Count == 0)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (var d in Dependencies)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append($"#include \"{d}\"");
            }
            return sb.ToString();
        }

        string GenerateResultVariable(Type type)
        {
            var name = type switch
            {
                not null when type == typeof(float) => "float",
                not null when type == typeof(Vector2) => "float2",
                not null when type == typeof(Vector3) => "float3",
                not null when type == typeof(Vector4) => "float4",
                not null when type == typeof(Matrix4x4) => "float4x4",
                not null when type == typeof(double) => "double",
                not null when type == typeof(uint) => "uint",
                not null when type == typeof(int) => "int",
                _ => throw new NotSupportedException($"Type {type?.Name} is not supported."),
            };
            return $"RWStructuredBuffer<{name}> testResult;";
        }

        string GenerateArrange()
        {
            if (Arrange.Length == 0)
                return string.Empty;
            var sb = new StringBuilder();
            foreach (var line in Arrange)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.Append(line);
            }
            return sb.ToString();
        }

        string GenerateAct()
        {
            return string.IsNullOrEmpty(Act) ? string.Empty : $"testResult[id.x] = {Act};";
        }

        public override string GenerateShaderWrapper()
        {
            var arrange = GenerateArrange();
            var act = GenerateAct();
            var body = arrange.Length > 0 && act.Length > 0
                ? $"{arrange}\n    {act}"
                : $"{arrange}{act}";

            return @$"#pragma kernel CSMain

{GenerateDependencies()}
{GenerateProperties()}
{GenerateResultVariable(m_Type)}

[numthreads(1, 1, 1)]
void CSMain (uint3 id : SV_DispatchThreadID)
{{
    {body}
}}";
        }
    }
}
