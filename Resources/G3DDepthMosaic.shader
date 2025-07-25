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

            struct VertAttributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
            };

            v2f vert(VertAttributes input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

                return output;

            }

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
                {
                    // the text coords of the original left and right view are from 0 - 1
                    // the tex coords of the texel we are currently rendering are also from 0 - 1
                    // but we want to create a grid of views, so we need to transform the tex coords
                    // to the grid size.
                    // basically we want to figure out in which grid cell the current texel is, then convert the texel coords to the grid cell coords.
                    // example assuming a grid size of 3x3:
                    // original tex coords: 0.8, 0.5
                    // step 1: transform the tex coords to the grid size by multiplying with grid size
                    //    -> e.g. original x coord 0.8 turns to 0.8 * 3 = 2.4
                    // step 2: figure out the grid cell by taking the integer part of the transformed tex coords
                    //    -> e.g. 2.4 turns to 2
                    // step 3: subtract the integer part from the transformed tex coords to get the texel coords in the grid cell
                    //   -> e.g. 2.4 - 2 = 0.4 -> final texel coords in the grid cell are 0.4, 0.5
                }
                float2 cellCoordinates = float2(i.uv.x, 1.0 - i.uv.y); // flip y coordiate to have cell index 0 in upper left corner
                cellCoordinates = float2(cellCoordinates.x * grid_size_x, cellCoordinates.y * grid_size_y);
                uint viewIndex = uint(cellCoordinates.x) + grid_size_x * uint(cellCoordinates.y);
                // texcoords in this texcels coordinate system (from 0 - 1)
                float2 actualTexCoords = float2(cellCoordinates.x - float(int(cellCoordinates.x)), cellCoordinates.y - float(int(cellCoordinates.y)));
                actualTexCoords.y = 1.0 - actualTexCoords.y; // flip y coordinate to match original tex coords

                float depth = getCameraLogDepth(actualTexCoords, viewIndex);
                return float4(depth, depth, depth, 1.0f); // return the depth value as color
            }
            ENDHLSL
        }
    }
}