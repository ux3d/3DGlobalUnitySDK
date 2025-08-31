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
            Name "EdgeDetection"
            ZTest Never Cull Off ZWrite Off

            HLSLPROGRAM
            // #include "G3DHLSLShaderBasics.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #define SMAA_HLSL_4
            #define SMAA_PRESET_MEDIUM
            // #define SMAA_RT_METRICS float4(1.0 / 1980.0, 1.0 / 1080.0, 1980.0, 1080.0)
            uniform float4 SMAA_RT_METRICS;
            #include "SMAA/SMAA.hlsl"

            struct VertAttributes {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
                float4 smaaOffsets[3] : TEXCOORD1;
            };

            Texture2D ColorTex;

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
                    ColorTex
                );
                return float4(edges, 0.0, 0.0);
            }
            ENDHLSL
        }

        Pass {
            Name "BlendingWeightCalculation"
            ZTest Never Cull Off ZWrite Off

            HLSLPROGRAM
            // #include "G3DHLSLShaderBasics.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #define SMAA_HLSL_4
            #define SMAA_PRESET_MEDIUM
            // #define SMAA_RT_METRICS float4(1.0 / 1980.0, 1.0 / 1080.0, 1980.0, 1080.0)
            uniform float4 SMAA_RT_METRICS;
            #include "SMAA/SMAA.hlsl"

            struct VertAttributes {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
                float2 smaaPixcoord : TEXCOORD1;
                float4 smaaOffsets[3] : TEXCOORD2;
            };

            Texture2D edgesTex;
            Texture2D areaTex;
            Texture2D searchTex;

            v2f vert(VertAttributes input) {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

                SMAABlendingWeightCalculationVS(output.uv, output.smaaPixcoord, output.smaaOffsets);

                return output;
            }
            
            float4 frag(v2f i) : SV_Target {
                return SMAABlendingWeightCalculationPS(
                    i.uv,
                    i.smaaPixcoord,
                    i.smaaOffsets,
                    edgesTex,
                    areaTex,
                    searchTex,
                    float4(0.0, 0.0, 0.0, 0.0)
                );

                // return float4(i.uv, 1.0, 1.0);
                // return ColorTex.Sample(sampler_MainTex, i.uv);
                // return AreaTex.Sample(sampler_AreaTex, i.uv);
                // return AreaTex.Sample(SMAALinearClampSampler, i.uv);
                // return float4(SearchTex.Sample(SMAAPointClampSampler, i.uv).xxx, 1.0);
            }
            ENDHLSL
        }

        Pass {
            Name "NeighborhoodBlending"
            ZTest Never Cull Off ZWrite Off

            HLSLPROGRAM
            // #include "G3DHLSLShaderBasics.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.0

            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            
            #define SMAA_HLSL_4
            #define SMAA_PRESET_MEDIUM
            // #define SMAA_RT_METRICS float4(1.0 / 1980.0, 1.0 / 1080.0, 1980.0, 1080.0)
            uniform float4 SMAA_RT_METRICS;
            #include "SMAA/SMAA.hlsl"

            struct VertAttributes {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
                float4 smaaOffset : TEXCOORD1;
            };

            Texture2D ColorTex;
            Texture2D blendTex;

            v2f vert(VertAttributes input) {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

                SMAANeighborhoodBlendingVS(output.uv, output.smaaOffset);

                return output;
            }
            
            float4 frag(v2f i) : SV_Target {
                return SMAANeighborhoodBlendingPS(
                    i.uv,
                    i.smaaOffset,
                    ColorTex,
                    blendTex
                );
            }
            ENDHLSL
        }
    }
}
