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

            int useLeftCamera = 1; // 0 = right, 1 = left

            Texture2D texture0;
            SamplerState samplertexture0;
            Texture2D texture1;
            SamplerState samplertexture1;
            
            float layer;  // range 0..1

            int depth_layer_discretization; // max layer amount -> 1024

            float layerDistance = 1.0f; // distance between layers in world space

            float maxDisparity = 1.5f; // range 0..2 disparity between left and right camera
            float crop;      // range 0..1 percentage of image to crop from each side
            float normalization_min = 0; // scene min depth value -> set to nearPlane
            float normalization_max = 1; // scene max depth value -> set to farPlane

            int debug_mode;
            int grid_size_x;
            int grid_size_y;

            float focus_plane = 0.5f; // range near to far plane

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

            float linearNormalize(float value, float minV, float maxV) {
                return clamp((value - minV) / (maxV - minV), 0.0f, 1.0f);
            }
            
            // convert range back from 0..1 to minV..maxV
            // normalized device coordinates -> pixel coords + depth
            // normalized device coordinates times inverse projecion matrix
            float4 sampleDepth(float2 texCoords) {
                if(useLeftCamera == 1) {
                    return _depthMapLeft.Sample(sampler_depthMapLeft, texCoords);
                } else {
                    return _depthMapRight.Sample(sampler_depthMapRight, texCoords);
                }
            }

            float4 sampleTexture(float2 texCoords) {
                if(useLeftCamera == 1) {
                    return texture0.Sample(samplertexture0, texCoords);
                } else {
                    return texture1.Sample(samplertexture1, texCoords);
                }
            }
            
            /// <summary>
            /// creates a grid of views with a given grid size.
            /// each view is offset by a given disparity value.
            /// the upper left corner is the left most camera.
            /// the lower right corner is the right most camera.
            /// </summary>
            float4 fragHDRP (v2f i) : SV_Target
            {
                float2 texCoord = i.uv;

                float4 outputColor = float4(0.0f, 0.0f, 0.0f, 1.0f);


                int gridCount = grid_size_x * grid_size_y;
                float disparityStep = maxDisparity / gridCount * 1.0f; // flip (1.0) to invert view order
                

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
                float2 cellCoordinates = float2(texCoord.x, texCoord.y);
                cellCoordinates = float2(cellCoordinates.x * grid_size_x, cellCoordinates.y * grid_size_y);
              
                int viewIndex = int(cellCoordinates.x) + grid_size_x * int(cellCoordinates.y);
                
                // texcoords in this texcels coordinate system (from 0 - 1)
                float2 actualTexCoords = float2(cellCoordinates.x - float(int(cellCoordinates.x)), cellCoordinates.y - float(int(cellCoordinates.y)));
              
                // Crop image to avoid image artefacts at border
                // actualTexCoords *= (1.0f - crop);
                // actualTexCoords += crop * 0.5f;
                

                // first and last image in the grid are the left and right camera
                if (gridCount % 2 == 1 && viewIndex == gridCount / 2) { // CENTER VIEW
              
                  outputColor = sampleTexture(actualTexCoords);
              
                  outputColor.a = 0.0f;
                  return outputColor;
                }

                // disparity has to be calculated based on the formula in https://medium.com/analytics-vidhya/distance-estimation-cf2f2fd709d8
                // Z is the depth we get from the depthmap
                // D is the disparity we want to calculate
                // D = ((f/d) * T) / Z
              
                // Disparity calculation A:
                // linear shift
                //
                float dynamicViewOffset = viewIndex * disparityStep;
                
                // distance to the focus plane
                float focusPlaneDistance = (focus_plane - (normalization_max - layer));
              
                float2 texCoordShifted = actualTexCoords;
                texCoordShifted.x += focusPlaneDistance * dynamicViewOffset * 0.1f;
              
                float texDepth = sampleDepth(texCoordShifted).r;
                // layer 0 = back
                // layer 1 = front
              
                // texDepth 0 = back
                
                // discard fragments with a depth smaller than the depth of the layer we are currently rendering
                if ((layer - texDepth) > layerDistance) {
                  // from back to front
                  // discard fragments that are in front of current render
                  // layer
                  discard;
                }
              
                float hole_marker = 0.0f;
                // FragColor.b >0.5 -> hole
                if ((layer - texDepth) < -layerDistance) {
                  // from back to front
                  // discard fragments that are in front of current render
                  // layer
                    hole_marker = 1.0f;
                   discard;
                }
              
                // float dx = linearNormalize(sobel_x(texCoordShifted), normalization_min,
                //                            normalization_max);
                // float dxy = linearNormalize(sobel(texCoordShifted), normalization_min,
                //                             normalization_max);
              
                outputColor = sampleTexture(texCoordShifted);
              
                if (debug_mode == 4) {              // DEBUG OUTPUT
                  outputColor.r = (1.0f - texDepth); // 0.0=front | 1.0=back
                  outputColor.g = 0.0f;
                  outputColor.b = 0.0f;
                  outputColor.a = 1.0f;
                  return outputColor;
                }
              
                
                outputColor = clamp(outputColor, 0.0f, 1.0f);
                // outputColor.a = (dxy + dx) * (0.5); // TODO: set this bias as parameter
                return outputColor;
            }
            ENDHLSL
        }
    }
}