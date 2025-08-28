Shader "G3D/SMAA" {
    SubShader {
        PackageRequirements {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }
        Tags {
            "RenderType"="Opaque"
            "RenderPipeline" = "HDRenderPipeline"
        }

        Pass {
            ZTest Always Cull Off ZWrite Off

            HLSLPROGRAM
            // #include "G3DHLSLShaderBasics.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #define SMAA_RT_METRICS float4(1.0 / 1980.0, 1.0 / 1080.0, 1980.0, 1080.0)
            #define SMAA_HLSL_4
            #define SMAA_PRESET_MEDIUM
            #include "SMAA.hlsl"

            struct VertAttributes {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
                float4 smaaOffsets[3] : TEXCOORD1;
            };

            Texture2D _mainTex;
            SamplerState sampler_mainTex;

            v2f vert(VertAttributes input) {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

                SMAAEdgeDetectionVS(output.uv, output.smaaOffsets);

                return output;
            }
            
            float4 frag(v2f i) : SV_Target {
                float2 edges = SMAAColorEdgeDetectionPS(
                    i.uv,
                    i.smaaOffsets,
                    _mainTex
                );

                return float4(edges, 1.0, 1.0);
            }
            ENDHLSL
        }
    }
}
