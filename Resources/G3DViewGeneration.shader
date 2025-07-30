Shader "G3D/ViewGeneration"
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
            Name "G3DViewGeneration"

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

            // 0 = left camera, 1 = right camera
            Texture2D texture0;
            SamplerState samplertexture0;
            Texture2D texture1;
            SamplerState samplertexture1;

            Texture2D _depthMosaic;
            SamplerState sampler_depthMosaic;


            float4x4 rightViewProjMatrix;
            float4x4 leftViewProjMatrix;

            float4x4 inverseProjMatrix0;
            float4x4 inverseProjMatrix1;
            float4x4 inverseProjMatrix2;
            float4x4 inverseProjMatrix3;
            float4x4 inverseProjMatrix4;
            float4x4 inverseProjMatrix5;
            float4x4 inverseProjMatrix6;
            float4x4 inverseProjMatrix7;
            float4x4 inverseProjMatrix8;
            float4x4 inverseProjMatrix9;
            float4x4 inverseProjMatrix10;
            float4x4 inverseProjMatrix11;
            float4x4 inverseProjMatrix12;
            float4x4 inverseProjMatrix13;
            float4x4 inverseProjMatrix14;
            float4x4 inverseProjMatrix15;

            float4x4 viewMatrix0;
            float4x4 viewMatrix1;
            float4x4 viewMatrix2;
            float4x4 viewMatrix3;
            float4x4 viewMatrix4;
            float4x4 viewMatrix5;
            float4x4 viewMatrix6;
            float4x4 viewMatrix7;
            float4x4 viewMatrix8;
            float4x4 viewMatrix9;
            float4x4 viewMatrix10;
            float4x4 viewMatrix11;
            float4x4 viewMatrix12;
            float4x4 viewMatrix13;
            float4x4 viewMatrix14;
            float4x4 viewMatrix15;



            int grid_size_x;
            int grid_size_y;

            float4x4 getInverseViewProjectionMatrix(int viewIndex) {
                switch (viewIndex) {
                    case 0: return inverseProjMatrix0;
                    case 1: return inverseProjMatrix1;
                    case 2: return inverseProjMatrix2;
                    case 3: return inverseProjMatrix3;
                    case 4: return inverseProjMatrix4;
                    case 5: return inverseProjMatrix5;
                    case 6: return inverseProjMatrix6;
                    case 7: return inverseProjMatrix7;
                    case 8: return inverseProjMatrix8;
                    case 9: return inverseProjMatrix9;
                    case 10: return inverseProjMatrix10;
                    case 11: return inverseProjMatrix11;
                    case 12: return inverseProjMatrix12;
                    case 13: return inverseProjMatrix13;
                    case 14: return inverseProjMatrix14;
                    case 15: return inverseProjMatrix15;
                    default: return float4x4(1.0f, 1.0f, 1.0f, 1.0f,
                                             1.0f, 1.0f, 1.0f, 1.0f,
                                             1.0f, 1.0f, 1.0f, 1.0f,
                                             1.0f, 1.0f, 1.0f, 1.0f);
                }
            }

            float4x4 getViewMatrix(int viewIndex) {
                switch (viewIndex) {
                    case 0: return viewMatrix0;
                    case 1: return viewMatrix1;
                    case 2: return viewMatrix2;
                    case 3: return viewMatrix3;
                    case 4: return viewMatrix4;
                    case 5: return viewMatrix5;
                    case 6: return viewMatrix6;
                    case 7: return viewMatrix7;
                    case 8: return viewMatrix8;
                    case 9: return viewMatrix9;
                    case 10: return viewMatrix10;
                    case 11: return viewMatrix11;
                    case 12: return viewMatrix12;
                    case 13: return viewMatrix13;
                    case 14: return viewMatrix14;
                    case 15: return viewMatrix15;
                    default: return float4x4(1.0f, 0.0f, 0.0f, 0.0f,
                                             0.0f, 1.0f, 0.0f, 0.0f,
                                             0.0f, 0.0f, 1.0f, 0.0f,
                                             0.0f, 0.0f, 0.0f, 1.0f);
                }
            }

            // Set the texture array as a shader input
            float getCameraLogDepth(float2 fullScreenUV, int viewIndex) {
                float2 fragmentUV = calculateUVForMosaic(viewIndex, fullScreenUV, grid_size_y, grid_size_x);
                return _depthMosaic.Sample(sampler_depthMosaic, fragmentUV).r;
                
                {

                    // Test if bluring the depth map removes the banding artifacts
                    // const float offset[] = {0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0};
                    // const float weight[] = {0.191, 0.15, 0.092, 0.044, 0.017, 0.005, 0.001};
                    // float4 FragmentColor = float4(0.0f, 0.0f, 0.0f, 0.0f);
                    // // 2619 x 1387
                    // //(1.0, 0.0) -> horizontal blur
                    // //(0.0, 1.0) -> vertical blur
                    
                    // float hstep = 1.0 / 2619;
                    // float vstep = 1.0 / 1387;
                    
                    // for (int i = 1; i < 5; i++) {
                    //     FragmentColor +=
                    //     _depthMosaic.Sample(sampler_depthMosaic, fragmentUV + float2(hstep*offset[i], vstep*offset[i]))*weight[i] +
                    //     _depthMosaic.Sample(sampler_depthMosaic, fragmentUV - float2(hstep*offset[i], vstep*offset[i]))*weight[i];      
                    // }
                    // float4 ppColour = _depthMosaic.Sample(sampler_depthMosaic, fragmentUV) * weight[0];
                    // ppColour += FragmentColor;
                    
                    
                    // return ppColour.r;//_depthMosaic.Sample(sampler_depthMosaic, fragmentUV).r;
                }
            }

            // here UV is treated as a full screen UV coordinate
            float2 calculateProjectedFragmentPosition(float2 uv, int viewIndex, float4x4 viewProjectionMatrix) {
                float logDepth = getCameraLogDepth(uv, viewIndex);
                // Sample the depth from the Camera depth texture.
                float deviceDepth = logDepth;

                // Reconstruct the world space positions.
                float3 worldPos = ComputeWorldSpacePosition(uv, deviceDepth, getInverseViewProjectionMatrix(viewIndex));

                float3 NDC = ComputeNormalizedDeviceCoordinatesWithZ(worldPos, viewProjectionMatrix); // convert from clip space to NDC coordinates
                
                return float2(NDC.xy); // return the difference between the shifted and original x coordinate
            }
            
            float LinearEyeDepthViewBased(float2 uv, int viewIndex)
            {
                float logDepth = getCameraLogDepth(uv, viewIndex);
                // Sample the depth from the Camera depth texture.
                float deviceDepth = logDepth;
                float3 worldPos = ComputeWorldSpacePosition(uv, deviceDepth, getInverseViewProjectionMatrix(viewIndex));
                float eyeDepth = LinearEyeDepth(worldPos, getViewMatrix(viewIndex));
                return eyeDepth;
            }

            /// <summary>
            /// creates a grid of views with a given grid size.
            /// each view is offset by a given disparity value.
            /// the upper left corner is the left most camera.
            /// the lower right corner is the right most camera.
            /// </summary>
            float4 fragHDRP (v2f i) : SV_Target
            {
                float2 cellCoordinates = getCellCoordinates(i.uv, grid_size_x, grid_size_y);
                uint viewIndex = getViewIndex(cellCoordinates, grid_size_x, grid_size_y);
                float2 cellTexCoords = getCellTexCoords(cellCoordinates);

                // first and last image in the grid are the left and right camera
                uint gridCount = grid_size_x * grid_size_y;
                if (viewIndex == 0) {
                    return texture0.Sample(samplertexture0, cellTexCoords); // sample the left camera texture
                }
                if (viewIndex == gridCount - 1) {
                    return texture1.Sample(samplertexture1, cellTexCoords); // sample the right camera texture
                }

                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                
                float2 shiftedLeftTexcoords = calculateProjectedFragmentPosition(cellTexCoords, viewIndex, leftViewProjMatrix); // convert to pixel space
                float originalLeftDepth = LinearEyeDepthViewBased(shiftedLeftTexcoords, 0); // sample the depth of the original left camera
                float actualDepth = LinearEyeDepthViewBased(cellTexCoords, viewIndex); // sample the depth of the shifted left camera

                float2 shiftedRightTexcoords = calculateProjectedFragmentPosition(cellTexCoords, viewIndex, rightViewProjMatrix); // convert to pixel space
                float originalRightDepth = LinearEyeDepthViewBased(shiftedRightTexcoords, gridCount - 1); // sample the depth of the original right camera


                uint discardFragmentLeft = 0;
                if(abs(originalLeftDepth - actualDepth) > 0.1) {
                    discardFragmentLeft = 1; // discard if the depth of the shifted left camera is too far away from the original left camera depth
                }

                if(shiftedLeftTexcoords.x < 0 || shiftedLeftTexcoords.x > 1.0f) {
                    discardFragmentLeft = 1; // discard if the tex coord is out of bounds
                }


                uint discardFragmentRight = 0;
                if(abs(originalRightDepth - actualDepth) > 0.1) {
                    discardFragmentRight = 1; // discard if the depth of the shifted right camera is too far away from the original right camera depth
                }

                if(shiftedRightTexcoords.x < 0 || shiftedRightTexcoords.x > 1.0f) {
                    discardFragmentRight = 1; // discard if the tex coord is out of bounds
                }

                if (discardFragmentLeft == 1 && discardFragmentRight == 1) {
                    discard;
                }
                if (discardFragmentLeft == 1 && discardFragmentRight == 0) {
                    return texture1.Sample(samplertexture1, shiftedRightTexcoords); // sample the right camera texture
                }
                if (discardFragmentLeft == 0 && discardFragmentRight == 1) {
                    return texture0.Sample(samplertexture0, shiftedLeftTexcoords); // sample the left camera texture
                }

                return texture1.Sample(samplertexture1, shiftedRightTexcoords);
            }
            ENDHLSL
        }
    }
}