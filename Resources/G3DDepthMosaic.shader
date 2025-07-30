Shader "G3D/DepthMosaic"
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
            Name "G3DDepthMosaic"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "G3DHLSLCommonFunctions.hlsl"

            #pragma vertex vert
            #pragma fragment fragHDRP


            Texture2D _depthMap0;
            SamplerState sampler_depthMap0;
            Texture2D _depthMap1;
            SamplerState sampler_depthMap1;
            Texture2D _depthMap2;
            SamplerState sampler_depthMap2;
            Texture2D _depthMap3;
            SamplerState sampler_depthMap3;
            Texture2D _depthMap4;
            SamplerState sampler_depthMap4;
            Texture2D _depthMap5;
            SamplerState sampler_depthMap5;
            Texture2D _depthMap6;
            SamplerState sampler_depthMap6;
            Texture2D _depthMap7;
            SamplerState sampler_depthMap7;
            Texture2D _depthMap8;
            SamplerState sampler_depthMap8;
            Texture2D _depthMap9;
            SamplerState sampler_depthMap9;
            Texture2D _depthMap10;
            SamplerState sampler_depthMap10;
            Texture2D _depthMap11;
            SamplerState sampler_depthMap11;
            Texture2D _depthMap12;
            SamplerState sampler_depthMap12;
            Texture2D _depthMap13;
            SamplerState sampler_depthMap13;
            Texture2D _depthMap14;
            SamplerState sampler_depthMap14;
            Texture2D _depthMap15;
            SamplerState sampler_depthMap15;

            int grid_size_x;
            int grid_size_y;

            // Set the texture array as a shader input
            float getCameraLogDepth(float2 uv, int cameraIndex) {
                switch(cameraIndex) {
                    case 0:
                        return _depthMap0.Sample( sampler_depthMap0, uv).r;
                    case 1:
                        return _depthMap1.Sample( sampler_depthMap1, uv).r;
                    case 2:
                        return _depthMap2.Sample( sampler_depthMap2, uv).r;
                    case 3:
                        return _depthMap3.Sample( sampler_depthMap3, uv).r;
                    case 4: 
                        return _depthMap4.Sample( sampler_depthMap4, uv).r;
                    case 5:
                        return _depthMap5.Sample( sampler_depthMap5, uv).r;
                    case 6:
                        return _depthMap6.Sample( sampler_depthMap6, uv).r;
                    case 7:
                        return _depthMap7.Sample( sampler_depthMap7, uv).r;
                    case 8:
                        return _depthMap8.Sample( sampler_depthMap8, uv).r;
                    case 9:
                        return _depthMap9.Sample( sampler_depthMap9, uv).r;  
                    case 10:
                        return _depthMap10.Sample( sampler_depthMap10, uv).r;  
                    case 11:
                        return _depthMap11.Sample( sampler_depthMap11, uv).r;  
                    case 12:
                        return _depthMap12.Sample( sampler_depthMap12, uv).r;  
                    case 13:
                        return _depthMap13.Sample( sampler_depthMap13, uv).r;  
                    case 14:
                        return _depthMap14.Sample( sampler_depthMap14, uv).r;  
                    case 15:
                        return _depthMap15.Sample( sampler_depthMap15, uv).r;  
                    default:
                        return _depthMap0.Sample( sampler_depthMap0, uv).r; // use left camera depth map as default
                }

            }

            /// <summary>
            /// combines the original depthMaps into one depth map that contains all views
            /// </summary>
            float4 fragHDRP (v2f i) : SV_Target
            {
                float2 cellCoordinates = getCellCoordinates(i.uv, grid_size_x, grid_size_y);
                uint viewIndex = getViewIndex(cellCoordinates, grid_size_x, grid_size_y);
                float2 cellTexCoords = getCellTexCoords(cellCoordinates);

                float depth = getCameraLogDepth(cellTexCoords, viewIndex);
                return float4(depth, depth, depth, 1.0f); // return the depth value as color
            }
            ENDHLSL
        }
    }
}