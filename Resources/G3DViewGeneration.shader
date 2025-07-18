Shader "G3D/ViewGeneration"
{
    Properties
    {
        DepthMap ("Texture", 2D) = "green" {}
    }
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

            Texture2D _depthMapRight;
            SamplerState sampler_depthMapRight;


            Texture2D _depthMapLeft;
            SamplerState sampler_depthMapLeft;

            float4x4 rightProjMatrix;
            float4x4 inverseRightProjMatrix;
            float4x4 leftProjMatrix;
            float4x4 inverseLeftProjMatrix;
            float4x4 mainCameraProjectionMatrix;
            float4x4 inverseMainCameraViewMatrix;
            float4x4 leftViewMatrix;

            // 0 = right camera, 1 = left camera
            Texture2D texture0;
            SamplerState samplertexture0;
            Texture2D texture1;
            SamplerState samplertexture1;
            
            float layer;  // range 0..1
            float focusDistance; // distance to focus plane in camera space

            int depth_layer_discretization; // max layer amount -> 1024

            float layerDistance = 1.0f; // distance between layers in world space

            float maxDisparity = 1.5f; // range 0..2 disparity between left and right camera
            float nearPlane = 0; // scene min depth value -> set to nearPlane
            float farPlane = 1; // scene max depth value -> set to farPlane

            int grid_size_x;
            int grid_size_y;

            float focalLengthInPixel = 600;

            int cameraPixelWidth = 1920;



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

            float computeClipSpaceOffset(float cameraDistance, float focusDist, float distLayerFocusPlane, float4x4 projectionMatrix) {
                float y = (cameraDistance / focusDist) * distLayerFocusPlane;
                float4 offsetClipSpace = float4(y, 0.0f, 0.0f, 1.0f); // offset in clip space
                offsetClipSpace = mul(offsetClipSpace, projectionMatrix); // apply projection matrix to get clip space coordinates
                return offsetClipSpace.x;
            }


            float sampleLeftDepthProjection(float2 uv) {
                float4 depthSample = _depthMapLeft.Sample(sampler_depthMapLeft, uv);
                float depth = Linear01Depth(depthSample.r, _ZBufferParams); // convert depth from logarithmic scale to linear scale
                float x = uv.x * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                float y = uv.y * 2.0f - 1.0f; // convert from [0,1] to [-1,1] to get NDC coordinates
                float4 NDC = float4(x, y, depth, 1.0f);
                NDC = mul(inverseLeftProjMatrix, NDC); // apply inverse projection matrix to get clip space coordinates
                return -NDC.z / NDC.w; // devide by w to get depth in view space
            }

            float4x4 getIdentityMatrix() {
                return float4x4(
                    1.0f, 0.0f, 0.0f, 0.0f,
                    0.0f, 1.0f, 0.0f, 0.0f,
                    0.0f, 0.0f, 1.0f, 0.0f,
                    0.0f, 0.0f, 0.0f, 1.0f
                );
            }
            float4x4 inverse(float4x4 m) {
                float n11 = m[0][0], n12 = m[1][0], n13 = m[2][0], n14 = m[3][0];
                float n21 = m[0][1], n22 = m[1][1], n23 = m[2][1], n24 = m[3][1];
                float n31 = m[0][2], n32 = m[1][2], n33 = m[2][2], n34 = m[3][2];
                float n41 = m[0][3], n42 = m[1][3], n43 = m[2][3], n44 = m[3][3];
            
                float t11 = n23 * n34 * n42 - n24 * n33 * n42 + n24 * n32 * n43 - n22 * n34 * n43 - n23 * n32 * n44 + n22 * n33 * n44;
                float t12 = n14 * n33 * n42 - n13 * n34 * n42 - n14 * n32 * n43 + n12 * n34 * n43 + n13 * n32 * n44 - n12 * n33 * n44;
                float t13 = n13 * n24 * n42 - n14 * n23 * n42 + n14 * n22 * n43 - n12 * n24 * n43 - n13 * n22 * n44 + n12 * n23 * n44;
                float t14 = n14 * n23 * n32 - n13 * n24 * n32 - n14 * n22 * n33 + n12 * n24 * n33 + n13 * n22 * n34 - n12 * n23 * n34;
            
                float det = n11 * t11 + n21 * t12 + n31 * t13 + n41 * t14;
                float idet = 1.0f / det;
            
                float4x4 ret;
            
                ret[0][0] = t11 * idet;
                ret[0][1] = (n24 * n33 * n41 - n23 * n34 * n41 - n24 * n31 * n43 + n21 * n34 * n43 + n23 * n31 * n44 - n21 * n33 * n44) * idet;
                ret[0][2] = (n22 * n34 * n41 - n24 * n32 * n41 + n24 * n31 * n42 - n21 * n34 * n42 - n22 * n31 * n44 + n21 * n32 * n44) * idet;
                ret[0][3] = (n23 * n32 * n41 - n22 * n33 * n41 - n23 * n31 * n42 + n21 * n33 * n42 + n22 * n31 * n43 - n21 * n32 * n43) * idet;
            
                ret[1][0] = t12 * idet;
                ret[1][1] = (n13 * n34 * n41 - n14 * n33 * n41 + n14 * n31 * n43 - n11 * n34 * n43 - n13 * n31 * n44 + n11 * n33 * n44) * idet;
                ret[1][2] = (n14 * n32 * n41 - n12 * n34 * n41 - n14 * n31 * n42 + n11 * n34 * n42 + n12 * n31 * n44 - n11 * n32 * n44) * idet;
                ret[1][3] = (n12 * n33 * n41 - n13 * n32 * n41 + n13 * n31 * n42 - n11 * n33 * n42 - n12 * n31 * n43 + n11 * n32 * n43) * idet;
            
                ret[2][0] = t13 * idet;
                ret[2][1] = (n14 * n23 * n41 - n13 * n24 * n41 - n14 * n21 * n43 + n11 * n24 * n43 + n13 * n21 * n44 - n11 * n23 * n44) * idet;
                ret[2][2] = (n12 * n24 * n41 - n14 * n22 * n41 + n14 * n21 * n42 - n11 * n24 * n42 - n12 * n21 * n44 + n11 * n22 * n44) * idet;
                ret[2][3] = (n13 * n22 * n41 - n12 * n23 * n41 - n13 * n21 * n42 + n11 * n23 * n42 + n12 * n21 * n43 - n11 * n22 * n43) * idet;
            
                ret[3][0] = t14 * idet;
                ret[3][1] = (n13 * n24 * n31 - n14 * n23 * n31 + n14 * n21 * n33 - n11 * n24 * n33 - n13 * n21 * n34 + n11 * n23 * n34) * idet;
                ret[3][2] = (n14 * n22 * n31 - n12 * n24 * n31 - n14 * n21 * n32 + n11 * n24 * n32 + n12 * n21 * n34 - n11 * n22 * n34) * idet;
                ret[3][3] = (n12 * n23 * n31 - n13 * n22 * n31 + n13 * n21 * n32 - n11 * n23 * n32 - n12 * n21 * n33 + n11 * n22 * n33) * idet;
            
                return ret;
            }

            float4x4 calculateProjectionMatrix(float localCameraOffset) {
                // horizontal obliqueness
                float horizontalObl = -localCameraOffset / focusDistance;

                // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
                float4x4 shearMatrix = getIdentityMatrix();
                shearMatrix[0, 2] = horizontalObl;
                // apply new projection matrix
                return mul(mainCameraProjectionMatrix, shearMatrix);
            }

            float calculateOffset(float layerDist, float cameraOffset, float2 uv) {
                // erst punkt in clip space berechnen
                // dann mit inverse projection matrix in view space umwandeln
                // dann mit inverse view matrix in world space umwandeln
                // dann mit view matrix der linken kamera in view space umwandeln
                // dann mit projection matrix der linken kamera in clip space umwandeln
                // dann die x koordinaten der beiden punkte vergleichen und den unterschied nehmen

                float tmp = map(layerDist, 0.0f, farPlane, 0.0f, 1.0f); // convert layer distance from [nearPlane, farPlane] to [0,1] 
                float4 p = float4(0.0f, 0.0f, tmp, 1.0f); // point in view space
                float4x4 cameraProjectionMatrix = calculateProjectionMatrix(cameraOffset);
                float4x4 inverseCameraProjectionMatrix = inverse(cameraProjectionMatrix); // inverse projection matrix to convert from clip space to view space
                float4x4 inverseCameraViewMatrix = inverseMainCameraViewMatrix; // inverse view matrix to convert from view space to world space
                // shift view inverse view matrix to the left
                inverseMainCameraViewMatrix[0][3] += cameraOffset; // shift the view matrix to the left by the camera offset
                p = mul(inverseCameraProjectionMatrix, p); // convert from clip space to view space
                p = mul(inverseCameraViewMatrix, p); // convert from view space to world space

                // p now in world space

                p = mul(leftViewMatrix, p); // apply left view matrix to get shifted point in view space
                p = mul(leftProjMatrix, p); // apply main camera projection matrix to get clip space coordinates

                float clipSpaceX = p.x / p.w; // convert to clip space by dividing by w
                clipSpaceX = clipSpaceX * 0.5f + 0.5f; // convert from [-1,1] to [0,1] to get texture coordinates
                // clipSpaceX = map(clipSpaceX, -1.0f, 1.0f, 0.0f, 1.0f); // convert from [-1,1] to [0,1] to get texture coordinates
                
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
                // float depth = sampleLeftDepthProjection(i.uv);
                // depth = map(depth, nearPlane, farPlane, 0, 1);
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

                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                // -----------------------------------------------------------------------------------------------------------
                
                float disparityStep = maxDisparity / gridCount;

                float actualDepth = sampleLeftDepthProjection(actualTexCoords); // sample the depth of the current texel in the left camera
                
                float distLayerFocusPlane = -(layer - focusDistance); // distance between the layer and the focus plane
                float leftOffset = viewIndex * disparityStep; // distance between the original left camera and the current view
                float DLeft = calculateOffset(layer, leftOffset, actualTexCoords); // convert to pixel space

                
                float2 texCoordShiftedLeft = actualTexCoords;
                texCoordShiftedLeft.x += DLeft;
                float shiftedLeftDepth = sampleLeftDepthProjection(texCoordShiftedLeft);


                
                float rightOffset = (gridCount - 1 - viewIndex) * disparityStep; // distance between the original left camera and the current view
                float DRight = computeClipSpaceOffset(rightOffset, focusDistance, distLayerFocusPlane, rightProjMatrix); // calculate offset of layer in pixel
                float2 texCoordShiftedRight = actualTexCoords;
                texCoordShiftedRight.x -= DRight;
                float shiftedRightDepth = sampleRightDepth(texCoordShiftedRight);
                
                int leftFills = 1;
                if(texCoordShiftedLeft.x < 0 || texCoordShiftedLeft.x > 1.0f) {
                    leftFills = 0; // discard if the tex coord is out of bounds
                }
                if (abs((layer - shiftedLeftDepth)) > layerDistance) {
                    leftFills = 0; // discard if the layer is too far away from the shifted left depth
                }


                int rightFills = 0;
                if(texCoordShiftedRight.x < 0 || texCoordShiftedRight.x > 1.0f) {
                    rightFills = 0; // discard if the tex coord is out of bounds
                }
                if (abs((layer - shiftedRightDepth)) > layerDistance) {
                    rightFills = 0; // discard if the layer is too far away from the shifted right depth
                }

                if (leftFills == 0 && rightFills == 0) {
                    discard; // discard if both left and right camera do not fill the layer
                }
                if (leftFills == 1 && rightFills == 0) {
                    return texture1.Sample(samplertexture1, texCoordShiftedLeft); // sample the left camera texture
                }
                if (leftFills == 0 && rightFills == 1) {
                    return texture0.Sample(samplertexture0, texCoordShiftedRight); // sample the right camera texture
                }

                if(shiftedLeftDepth < shiftedRightDepth) {
                    return texture1.Sample(samplertexture1, texCoordShiftedLeft); // sample the left camera texture
                }

                return texture0.Sample(samplertexture0, texCoordShiftedRight); // sample the right camera texture
                
                
                // if (DLeft < 0.0f) {
                //     return float4(0.0f, -DLeft, 0.0f, 1.0f);
                // }
                // return float4(DLeft, 0.0f, 0.0f, 1.0f);
            }
            ENDHLSL
        }
    }
}