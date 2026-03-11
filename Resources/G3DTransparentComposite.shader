Shader "G3D/G3DTransparentComposite"
{
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }
        Tags { "RenderType"="Transparent" "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "G3DTransparentComposite"

            ZWrite Off
            ZTest Always
            // Alpha-over blending: composite the transparent layer on top of the opaque mosaic tile.
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
            #include "G3DHLSLShaderBasics.hlsl"
            #include "G3DHLSLCommonFunctions.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            Texture2D _mainTex;
            SamplerState sampler_mainTex;

            float4 frag(v2f i) : SV_Target
            {
                float4 color = _mainTex.Sample(sampler_mainTex, i.uv);
                // Pixels with zero alpha are fully transparent (e.g. where no transparent
                // object was rendered), so they leave the opaque content untouched.
                return color;
            }
            ENDHLSL
        }
    }
}
