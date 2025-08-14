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
            #include "G3DHLSLShaderBasics.hlsl"
            #include "G3DHLSLCommonFunctions.hlsl"

            #pragma vertex vert
            #pragma fragment fragHDRP

            // 0 = left camera, 1 = right camera
            Texture2D _leftCamTex;
            SamplerState sampler_leftCamTex;
            Texture2D _middleCamTex;
            SamplerState sampler_middleCamTex;
            Texture2D _rightCamTex;
            SamplerState sampler_rightCamTex;

            float4x4 rightViewProjMatrix;
            float4x4 middleViewProjMatrix;
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

            Texture2D _depthMap0;
            Texture2D _depthMap1;
            Texture2D _depthMap2;
            Texture2D _depthMap3;
            Texture2D _depthMap4;
            Texture2D _depthMap5;
            Texture2D _depthMap6;
            Texture2D _depthMap7;
            Texture2D _depthMap8;
            Texture2D _depthMap9;
            Texture2D _depthMap10;
            Texture2D _depthMap11;
            Texture2D _depthMap12;
            Texture2D _depthMap13;
            Texture2D _depthMap14;
            Texture2D _depthMap15;

            SamplerState sampler_point_repeat;

            // Set the texture array as a shader input
            float getCameraLogDepth(float2 uv, int cameraIndex) {
                switch(cameraIndex) {
                    case 0:
                        return _depthMap0.Sample( sampler_point_repeat, uv).r;
                    case 1:
                        return _depthMap1.Sample( sampler_point_repeat, uv).r;
                    case 2:
                        return _depthMap2.Sample( sampler_point_repeat, uv).r;
                    case 3:
                        return _depthMap3.Sample( sampler_point_repeat, uv).r;
                    case 4:
                        return _depthMap4.Sample( sampler_point_repeat, uv).r;
                    case 5:
                        return _depthMap5.Sample( sampler_point_repeat, uv).r;
                    case 6:
                        return _depthMap6.Sample( sampler_point_repeat, uv).r;
                    case 7:
                        return _depthMap7.Sample( sampler_point_repeat, uv).r;
                    case 8:
                        return _depthMap8.Sample( sampler_point_repeat, uv).r;
                    case 9:
                        return _depthMap9.Sample( sampler_point_repeat, uv).r;  
                    case 10:
                        return _depthMap10.Sample( sampler_point_repeat, uv).r;  
                    case 11:
                        return _depthMap11.Sample( sampler_point_repeat, uv).r;  
                    case 12:
                        return _depthMap12.Sample( sampler_point_repeat, uv).r;  
                    case 13:
                        return _depthMap13.Sample( sampler_point_repeat, uv).r;  
                    case 14:
                        return _depthMap14.Sample( sampler_point_repeat, uv).r;  
                    case 15:
                        return _depthMap15.Sample( sampler_point_repeat, uv).r;  
                    default:
                        return _depthMap0.Sample( sampler_point_repeat, uv).r; // use left camera depth map as default
                }
            }



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

            uint shouldDiscardFragment(float2 shiftedTexCoords, float originalDepth, float actualDepth)
            {
                uint discardFragment = 0;
                if(abs(originalDepth - actualDepth) > 0.1) {
                    discardFragment = 1; // discard if the depth of the shifted left camera is too far away from the original left camera depth
                }

                if(shiftedTexCoords.x < 0 || shiftedTexCoords.x > 1.0f) {
                    discardFragment = 1; // discard if the tex coord is out of bounds
                }
                return discardFragment;
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

                // float logDepth = getCameraLogDepth(cellTexCoords, viewIndex);
                // return float4(logDepth, logDepth, logDepth, 1.0f); // return the log depth for debugging purposes

                // first and last image in the grid are the left and right camera
                uint gridCount = grid_size_x * grid_size_y;
                if (viewIndex == 0) {
                    return _leftCamTex.Sample(sampler_point_repeat, cellTexCoords); // sample the left camera texture
                }
                if (viewIndex == gridCount / 2) {
                    return _middleCamTex.Sample(sampler_point_repeat, cellTexCoords); // sample the middle camera texture
                }
                if (viewIndex == gridCount - 1) {
                    return _rightCamTex.Sample(sampler_point_repeat, cellTexCoords); // sample the right camera texture
                }

                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                
                float2 shiftedLeftTexcoords = calculateProjectedFragmentPosition(cellTexCoords, viewIndex, leftViewProjMatrix); // convert to pixel space
                float originalLeftDepth = LinearEyeDepthViewBased(shiftedLeftTexcoords, 0); // sample the depth of the original left camera
                
                float2 shiftedMiddleTexcoords = calculateProjectedFragmentPosition(cellTexCoords, viewIndex, middleViewProjMatrix); // convert to pixel space
                float originalMiddleDepth = LinearEyeDepthViewBased(shiftedMiddleTexcoords, gridCount / 2); // sample the depth of the original middle camera

                float2 shiftedRightTexcoords = calculateProjectedFragmentPosition(cellTexCoords, viewIndex, rightViewProjMatrix); // convert to pixel space
                float originalRightDepth = LinearEyeDepthViewBased(shiftedRightTexcoords, gridCount - 1); // sample the depth of the original right camera
                
                float actualDepth = LinearEyeDepthViewBased(cellTexCoords, viewIndex); // sample the depth of the shifted left camera

                uint discardFragmentLeft = shouldDiscardFragment(shiftedLeftTexcoords, originalLeftDepth, actualDepth);
                uint discardFragmentMiddle = shouldDiscardFragment(shiftedMiddleTexcoords, originalMiddleDepth, actualDepth);
                uint discardFragmentRight = shouldDiscardFragment(shiftedRightTexcoords, originalRightDepth, actualDepth);

                float4 finalColor = float4(0.0f, 0.0f, 0.0f, 0.0f);
                float factor = 0.0f;
                if (!discardFragmentLeft && factor < 1.0f) { // second condition to ensure we only use one fragment here
                    finalColor += _leftCamTex.Sample(sampler_point_repeat, shiftedLeftTexcoords); // sample the left camera texture
                    factor += 1.0f; // increase the factor for the left camera
                }
                if( !discardFragmentMiddle && factor < 1.0f) { // if the final color is not set yet, sample the middle camera texture
                    finalColor += _middleCamTex.Sample(sampler_point_repeat, shiftedMiddleTexcoords); // sample the middle camera texture
                    factor += 1.0f; // increase the factor for the middle camera
                }
                if (!discardFragmentRight && factor < 1.0f) {
                    finalColor += _rightCamTex.Sample(sampler_point_repeat, shiftedRightTexcoords); // sample the right camera texture
                    factor += 1.0f; // increase the factor for the right camera
                }

                return finalColor / factor;
            }
            ENDHLSL
        }
    }
}