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

            float2 calculateUVForMosaic(int viewIndex, float2 fullScreenUV, int mosaic_rows = 4, int mosaic_columns = 4) {
                if(viewIndex < 0 )
                {
                    viewIndex = 0;
                }
                int xAxis = viewIndex % mosaic_columns;
                int yAxis = viewIndex / mosaic_columns;
                // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
                // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
                yAxis = mosaic_rows - 1 - yAxis;
                int2 moasicIndex = int2(xAxis, yAxis);
                float2 scaledUV = float2(fullScreenUV.x / mosaic_columns, fullScreenUV.y / mosaic_rows);
                float2 cellSize = float2(1.0 / mosaic_columns, 1.0 / mosaic_rows);
                return scaledUV + cellSize * moasicIndex;
            }

            // Set the texture array as a shader input
            float getCameraLogDepth(float2 fullScreenUV, int viewIndex) {
                float2 fragementUV = calculateUVForMosaic(viewIndex, fullScreenUV, grid_size_y, grid_size_x);
                return _depthMosaic.Sample(sampler_depthMosaic, fragementUV).r;
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

            /// <summary>
            /// creates a grid of views with a given grid size.
            /// each view is offset by a given disparity value.
            /// the upper left corner is the left most camera.
            /// the lower right corner is the right most camera.
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

                // first and last image in the grid are the left and right camera
                uint gridCount = grid_size_x * grid_size_y;
                if (viewIndex == 0) {
                    return texture0.Sample(samplertexture0, actualTexCoords); // sample the left camera texture
                }
                if (viewIndex == gridCount - 1) {
                    return texture1.Sample(samplertexture1, actualTexCoords); // sample the right camera texture
                }

                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                
                float2 shiftedLeftTexcoords = calculateProjectedFragmentPosition(actualTexCoords, viewIndex, leftViewProjMatrix); // convert to pixel space
                float originalLeftDepth = getCameraLogDepth(shiftedLeftTexcoords, 0); // sample the depth of the original left camera
                originalLeftDepth = LinearEyeDepth(originalLeftDepth, _ZBufferParams); // convert from log depth to linear depth
                float leftDepth = getCameraLogDepth(actualTexCoords, viewIndex); // sample the depth of the shifted left camera
                leftDepth = LinearEyeDepth(leftDepth, _ZBufferParams); // convert from log depth to linear depth

                float2 shiftedRightTexcoords = calculateProjectedFragmentPosition(actualTexCoords, viewIndex, rightViewProjMatrix); // convert to pixel space
                float originalRightDepth = getCameraLogDepth(shiftedRightTexcoords, gridCount - 1); // sample the depth of the original right camera
                originalRightDepth = LinearEyeDepth(originalRightDepth, _ZBufferParams); // convert from log depth to linear depth
                float rightDepth = getCameraLogDepth(actualTexCoords, viewIndex); // sample the depth of the shifted right camera
                rightDepth = LinearEyeDepth(rightDepth, _ZBufferParams); // convert from log depth to linear depth

                uint discardFragmentLeft = 0;
                if(abs(originalLeftDepth - leftDepth) > 0.1) {
                    discardFragmentLeft = 1; // discard if the depth of the shifted left camera is too far away from the original left camera depth
                }

                if(shiftedLeftTexcoords.x < 0 || shiftedLeftTexcoords.x > 1.0f) {
                    discardFragmentLeft = 1; // discard if the tex coord is out of bounds
                }


                uint discardFragmentRight = 0;
                if(abs(originalRightDepth - rightDepth) > 0.1) {
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
                if (leftDepth < rightDepth) {
                    return texture0.Sample(samplertexture0, shiftedLeftTexcoords); // sample the left camera texture
                }


                return texture1.Sample(samplertexture1, shiftedRightTexcoords);
                

                // -----------------
                // handle left and right cameras
                // -----------------
                // float rightOffset = (gridCount - 1 - viewIndex) * disparityStep; // distance between the original left camera and the current view
                // float DRight = calculateOffset(rightOffset, focusDistance, distLayerFocusPlane, rightProjMatrix); // calculate offset of layer in pixel
                // float2 texCoordShiftedRight = actualTexCoords;
                // texCoordShiftedRight.x -= DRight;
                // float shiftedRightDepth = sampleRightDepth(texCoordShiftedRight);
                
                // int leftFills = 1;
                // if(texCoordShiftedLeft.x < 0 || texCoordShiftedLeft.x > 1.0f) {
                //     leftFills = 0; // discard if the tex coord is out of bounds
                // }
                // if (abs((layer - shiftedLeftDepth)) > layerDistance) {
                //     leftFills = 0; // discard if the layer is too far away from the shifted left depth
                // }


                // int rightFills = 0;
                // if(texCoordShiftedRight.x < 0 || texCoordShiftedRight.x > 1.0f) {
                //     rightFills = 0; // discard if the tex coord is out of bounds
                // }
                // if (abs((layer - shiftedRightDepth)) > layerDistance) {
                //     rightFills = 0; // discard if the layer is too far away from the shifted right depth
                // }

                // if (leftFills == 0 && rightFills == 0) {
                //     discard; // discard if both left and right camera do not fill the layer
                // }
                // if (leftFills == 1 && rightFills == 0) {
                //     return texture1.Sample(samplertexture1, texCoordShiftedLeft); // sample the left camera texture
                // }
                // if (leftFills == 0 && rightFills == 1) {
                //     return texture0.Sample(samplertexture0, texCoordShiftedRight); // sample the right camera texture
                // }

                // if(shiftedLeftDepth < shiftedRightDepth) {
                //     return texture1.Sample(samplertexture1, texCoordShiftedLeft); // sample the left camera texture
                // }

                // return texture0.Sample(samplertexture0, texCoordShiftedRight); // sample the right camera texture
            }
            ENDHLSL
        }
    }
}