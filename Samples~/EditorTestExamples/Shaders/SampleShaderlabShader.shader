Shader "SampleTest/ShaderlabShader"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "UnityCG.cginc"

            float4 _Color;

            float4 Vert (float4 position : POSITION) : SV_POSITION
            {
                return UnityObjectToClipPos(position);
            }

            float4 Frag (float4 position : SV_POSITION) : SV_TARGET
            {
                return float4(_Color);
            }

            ENDCG
        }
    }
}
