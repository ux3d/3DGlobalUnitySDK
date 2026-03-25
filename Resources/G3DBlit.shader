Shader "G3D/G3DBlit"
{
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }
        Tags { "RenderType"="Opaque" "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "G3DBlit"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #include "G3DHLSLShaderBasics.hlsl"
            #include "G3DHLSLCommonFunctions.hlsl"

            #pragma vertex vert
            #pragma fragment fragHDRP

            // 0 = left camera, 1 = right camera
            Texture2D _mainTex;
            SamplerState sampler_mainTex;

            float4 fragHDRP (v2f i) : SV_Target
            {
                return _mainTex.Sample(sampler_mainTex, i.uv); // sample the main texture
            }
            ENDHLSL
        }
    }
}