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
            
            float sampleLeftDepth(float2 uv) {
                float4 depthSample = _depthMapLeft.Sample(sampler_depthMapLeft, uv);
                float depth = Linear01Depth(depthSample.r, _ZBufferParams); // convert depth from logarithmic scale to linear scale
                depth = map(depth, 0.0f, 1.0f, 0, farPlane);
                return depth;
            }

            float sampleRightDepth(float2 uv) {
                float4 depthSample = _depthMapRight.Sample(sampler_depthMapRight, uv);
                float depth = Linear01Depth(depthSample.r, _ZBufferParams); // convert depth from logarithmic scale to linear scale
                depth = map(depth, 0.0f, 1.0f, 0, farPlane);
                return depth;
            }

            float computeClipSpaceOffset(float cameraDistance, float focusDist, float distLayerFocusPlane, float4x4 projectionMatrix) {
                float y = (cameraDistance / focusDist) * distLayerFocusPlane;
                float4 offsetClipSpace = float4(y, 0.0f, 0.0f, 1.0f); // offset in clip space
                offsetClipSpace = mul(offsetClipSpace, projectionMatrix); // apply projection matrix to get clip space coordinates
                return offsetClipSpace.x;
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
                // depth = map(depth, 0, farPlane, 0, 1);
                // return float4(depth, depth, depth, 1.0f); // return depth value in world space

                
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
                float2 cellCoordinates = float2(i.uv.x, 1.0 - i.uv.y); // flip y coordiate to have cell index 0 in upper left corner
                cellCoordinates = float2(cellCoordinates.x * grid_size_x, cellCoordinates.y * grid_size_y);
                uint viewIndex = uint(cellCoordinates.x) + grid_size_x * uint(cellCoordinates.y);
                // texcoords in this texcels coordinate system (from 0 - 1)
                float2 actualTexCoords = float2(cellCoordinates.x - float(int(cellCoordinates.x)), cellCoordinates.y - float(int(cellCoordinates.y)));
                actualTexCoords.y = 1.0 - actualTexCoords.y; // flip y coordinate to match original tex coords

                uint gridCount = grid_size_x * grid_size_y;
                // first and last image in the grid are the left and right camera
                if (viewIndex == 0) {
                    return texture1.Sample(samplertexture1, actualTexCoords); // sample the left camera texture
                }
                if (viewIndex == gridCount - 1) {
                    return texture0.Sample(samplertexture0, actualTexCoords); // sample the right camera texture
                }
                
                float disparityStep = maxDisparity / gridCount;
                
                float distLayerFocusPlane = -(layer - focusDistance); // distance between the layer and the focus plane
                float leftOffset = viewIndex * disparityStep; // distance between the original left camera and the current view
                float DLeft = computeClipSpaceOffset(leftOffset, focusDistance, distLayerFocusPlane, leftProjMatrix); // calculate offset of layer in pixel
                
                float rightOffset = (gridCount - 1 - viewIndex) * disparityStep; // distance between the original left camera and the current view
                float DRight = computeClipSpaceOffset(rightOffset, focusDistance, distLayerFocusPlane, rightProjMatrix); // calculate offset of layer in pixel
                
                
                float2 texCoordShiftedLeft = actualTexCoords;
                texCoordShiftedLeft.x += DLeft;
                float shiftedLeftDepth = sampleLeftDepth(texCoordShiftedLeft);
                
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