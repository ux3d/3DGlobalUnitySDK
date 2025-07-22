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

            #pragma require 2darray

            Texture2DArray _DepthMaps;
            SamplerState sampler_DepthMaps;

            Texture2D _depthMapRight;
            SamplerState sampler_depthMapRight;


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


            Texture2D _depthMapLeft;
            SamplerState sampler_depthMapLeft;

            float4x4 inverseRightProjMatrix;
            float4x4 rightProjMatrix;

            float4x4 inverseLeftProjMatrix;
            float4x4 leftViewMatrix;
            float4x4 leftProjMatrix;

            float4x4 inverseProjMatrix1;
            float4x4 inverseViewMatrix1;
            float4x4 inverseProjMatrix2;
            float4x4 inverseViewMatrix2;
            float4x4 inverseProjMatrix3;
            float4x4 inverseViewMatrix3;
            float4x4 inverseProjMatrix4;
            float4x4 inverseViewMatrix4;
            float4x4 inverseProjMatrix5;
            float4x4 inverseViewMatrix5;
            float4x4 inverseProjMatrix6;
            float4x4 inverseViewMatrix6;
            float4x4 inverseProjMatrix7;
            float4x4 inverseViewMatrix7;
            float4x4 inverseProjMatrix8;
            float4x4 inverseViewMatrix8;
            float4x4 inverseProjMatrix9;
            float4x4 inverseViewMatrix9;
            float4x4 inverseProjMatrix10;
            float4x4 inverseViewMatrix10;
            float4x4 inverseProjMatrix11;
            float4x4 inverseViewMatrix11;
            float4x4 inverseProjMatrix12;
            float4x4 inverseViewMatrix12;
            float4x4 inverseProjMatrix13;
            float4x4 inverseViewMatrix13;
            float4x4 inverseProjMatrix14;
            float4x4 inverseViewMatrix14;
            float4x4 inverseProjMatrix15;
            float4x4 inverseViewMatrix15;


            // 0 = right camera, 1 = left camera
            Texture2D texture0;
            SamplerState samplertexture0;
            Texture2D texture1;
            SamplerState samplertexture1;
            
            float maxDisparity = 1.5f; // range 0..2 disparity between left and right camera
            float nearPlane = 0; // scene min depth value -> set to nearPlane
            float farPlane = 1; // scene max depth value -> set to farPlane

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


            float map(float x, float in_min, float in_max, float out_min, float out_max)
            {
                // Convert the current value to a percentage
                // 0% - min1, 100% - max1
                float perc = (x - in_min) / (in_max - in_min);

                // Do the same operation backwards with min2 and max2
                float value = perc * (out_max - out_min) + out_min;
                return value;
            }
            
            float sampleRightDepth(float2 uv) {
                float4 depthSample = _depthMapRight.Sample(sampler_depthMapRight, uv);;
                float depth = Linear01Depth(depthSample.r, _ZBufferParams); // convert depth from logarithmic scale to linear scale
                float x = uv.x * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                float y = uv.y * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                float4 NDC = float4(x, y, depth, 1.0f);
                NDC = mul(inverseRightProjMatrix, NDC); // apply inverse projection matrix to get clip space coordinates
                return -NDC.z / NDC.w; // devide by w to get depth in view space
            }

            float sampleLeftDepth(float2 uv) {
                float4 depthSample = _depthMapLeft.Sample(sampler_depthMapLeft, uv);
                float test = -1.0f + farPlane / nearPlane;
                float4 myZBufferParams = float4(test, 1.0f, test / farPlane, 1.0f / farPlane);
                float depth = LinearEyeDepth(depthSample.r, myZBufferParams); // convert depth from logarithmic scale to linear scale
                // float centerDepth = depth * 2.0f - 1.0f; 
                // float x = uv.x * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                // float y = uv.y * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                // float4 NDC = float4(x, y, centerDepth, 1.0f);
                // NDC = mul(inverseLeftProjMatrix, NDC); // apply inverse projection matrix to get clip space coordinates

                // float finalDepth = abs(NDC.z / NDC.w); // devide by w to get depth in view space
                return depth; // devide by w to get depth in view space
            }

            float4x4 getInverseProjectionMatrix(int cameraIndex) {
                switch (cameraIndex) {
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
            float4x4 getInverseViewMatrix(int cameraIndex) {
                switch (cameraIndex) {
                    case 1: return inverseViewMatrix1;
                    case 2: return inverseViewMatrix2;
                    case 3: return inverseViewMatrix3;
                    case 4: return inverseViewMatrix4;
                    case 5: return inverseViewMatrix5;
                    case 6: return inverseViewMatrix6;
                    case 7: return inverseViewMatrix7;
                    case 8: return inverseViewMatrix8;
                    case 9: return inverseViewMatrix9;
                    case 10: return inverseViewMatrix10;
                    case 11: return inverseViewMatrix11;
                    case 12: return inverseViewMatrix12;
                    case 13: return inverseViewMatrix13;
                    case 14: return inverseViewMatrix14;
                    case 15: return inverseViewMatrix15;
                    default: return float4x4(1.0f, 1.0f, 1.0f, 1.0f,
                                            1.0f, 1.0f, 1.0f, 1.0f,
                                            1.0f, 1.0f, 1.0f, 1.0f,
                                            1.0f, 1.0f, 1.0f, 1.0f);
                    }
            }

            // Set the texture array as a shader input
            float getCameraDepth(float2 uv, int cameraIndex) {
                float zIndex = 1.0f/ 16.0f * cameraIndex; // calculate the z index for the texture array
                
                float4 depthSample;
                switch(cameraIndex) {
                    case 0:
                        depthSample =  _depthMap0.Sample( sampler_depthMap0, uv);
                        break;
                    case 1:
                        depthSample =  _depthMap1.Sample( sampler_depthMap1, uv);
                        break;
                    case 2:
                        depthSample =  _depthMap2.Sample( sampler_depthMap2, uv);
                        break;
                    case 3:
                        depthSample =  _depthMap3.Sample( sampler_depthMap3, uv);
                        break;
                    case 4: 
                        depthSample =  _depthMap4.Sample( sampler_depthMap4, uv);
                        break;
                    case 5:
                        depthSample =  _depthMap5.Sample( sampler_depthMap5, uv);
                        break;
                    case 6:
                        depthSample =  _depthMap6.Sample( sampler_depthMap6, uv);
                        break;
                    case 7:
                        depthSample =  _depthMap7.Sample( sampler_depthMap7, uv);
                        break;
                    case 8:
                        depthSample =  _depthMap8.Sample( sampler_depthMap8, uv);
                        break;
                    case 9:
                        depthSample =  _depthMap9.Sample( sampler_depthMap9, uv);  
                        break;
                    default:
                        depthSample = _depthMapLeft.Sample(sampler_depthMapLeft, uv); // use left camera depth map as default
                        break;
                }

                float test = -1.0f + farPlane / nearPlane;
                float4 myZBufferParams = float4(test, 1.0f, test / farPlane, 1.0f / farPlane);
                return LinearEyeDepth(depthSample.r, myZBufferParams); // convert depth from logarithmic scale to linear scale
            }

            float calculateOffset(float2 uv, int cameraIndex) {
                float depth = getCameraDepth(uv, cameraIndex);

                float4 p = float4(0.0f, 0.0f, depth, 1.0f); // point in view space
                p = mul(getInverseProjectionMatrix(cameraIndex), p); // convert from clip space to view space
                p *= depth; // multiply by layer depth to get correct depth value in view space (layer is the correct w component)
                p = mul(getInverseViewMatrix(cameraIndex), p); // convert from view space to world space

                // p now in world space

                p = mul(leftViewMatrix, p); // apply left view matrix to get shifted point in view space
                p = mul(leftProjMatrix, p); // apply main camera projection matrix to get clip space coordinates

                float clipSpaceX = -p.x / p.w; // convert to clip space by dividing by w
                clipSpaceX = clipSpaceX / 2.0f; // devide by 2 to get from clip space [-1, 1] to texture coordinates [0, 1]
                return clipSpaceX; // return the difference between the shifted and original x coordinate
            }

            /// <summary>
            /// creates a grid of views with a given grid size.
            /// each view is offset by a given disparity value.
            /// the upper left corner is the left most camera.
            /// the lower right corner is the right most camera.
            /// </summary>
            float4 fragHDRP (v2f i) : SV_Target
            {
                // float depth = sampleLeftDepth(i.uv);
                // float depth = getCameraDepth(i.uv, 0);
                // depth = depth/ 10.0f; // scale depth to a smaller range for visualization
                // // depth = map(depth, 0, farPlane, 0, 1);
                // return float4(depth, depth, depth, 1.0f); // return depth value in world space

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
                    return texture1.Sample(samplertexture1, actualTexCoords); // sample the left camera texture
                }
                if (viewIndex == gridCount - 1) {
                    return texture0.Sample(samplertexture0, actualTexCoords); // sample the right camera texture
                }

                // float depth = getCameraDepth(actualTexCoords, viewIndex);
                // depth = depth/ 10.0f; // scale depth to a smaller range for visualization
                // // depth = map(depth, 0, farPlane, 0, 1);
                // return float4(depth, depth, depth, 1.0f); // return depth value in world space


                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                
                float disparityStep = maxDisparity / gridCount;

                float DLeft = calculateOffset(actualTexCoords, viewIndex); // convert to pixel space

                float2 texCoordShiftedLeft = actualTexCoords;
                texCoordShiftedLeft.x -= DLeft;
                
                // if(texCoordShiftedLeft.x < 0 || texCoordShiftedLeft.x > 1.0f) {
                //     discard; // discard if the tex coord is out of bounds
                // }

                float shiftedLeftDepth = sampleLeftDepth(texCoordShiftedLeft);
                
                float actualDepth = sampleLeftDepth(actualTexCoords); // get the actual depth of the current texel

                // if(abs((actualDepth - shiftedLeftDepth)) > 0.1f) {
                //     discard; // discard if the layer is too far away from the shifted left depth
                // }
                if( DLeft < 0.0f) {
                    return float4(0.0f, -DLeft, 0.0f, 1.0f); // return a debug value if the offset is negative
                }
                else  {
                    return float4(DLeft, 0.0f, 0.0f, 1.0f); // return a debug value if the offset is too large
                }

                return texture1.Sample(samplertexture1, texCoordShiftedLeft); // sample the left camera texture

                

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