Shader "G3D/AutostereoMultiviewMosaic"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "G3D_ShaderBasics.hlsl"

    // mosaic video parameters
    int mosaic_rows = 1; // number of rows in the mosaic
    int mosaic_columns = 1; // number of columns in the mosaic
    

    Texture2D _colorMosaic;
    SamplerState sampler_colorMosaic;

    int map(int x, int in_min, int in_max, int out_min, int out_max)
    {
        return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }

    float2 calculateUVForMosaic(int viewIndex, float2 startingUV) {
        if(viewIndex < 0 )
        {
            viewIndex = 0;
        }
        if(mosaic_rows * mosaic_columns < nativeViewCount)
        {
            viewIndex = map(viewIndex, 0, nativeViewCount - 1, 0, mosaic_rows * mosaic_columns - 1);
        }
        int xAxis = viewIndex % mosaic_columns;
        int yAxis = viewIndex / mosaic_columns;
        // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
        // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
        yAxis = mosaic_rows - 1 - yAxis;
        int2 moasicIndex = int2(xAxis, yAxis);
        float2 scaledUV = float2(startingUV.x / mosaic_columns, startingUV.y / mosaic_rows);
        float2 cellSize = float2(1.0 / mosaic_columns, 1.0 / mosaic_rows);
        return scaledUV + cellSize * moasicIndex;
    }

    float4 frag (v2f i) : SV_Target
    {
        float yPos = s_height - i.screenPos.y; // invert y coordinate to account for different coordinates between glsl and hlsl (original shader written in glsl)
        
        float2 computedScreenPos = float2(i.screenPos.x, i.screenPos.y) + float2(v_pos_x, v_pos_y);
        int3 viewIndices = getSubPixelViewIndices(computedScreenPos);
        
        float2 uvCoords = i.uv;
        // mirror the image if necessary
        if (mirror != 0) {
            uvCoords.x = 1.0 - uvCoords.x;
        }
        
         //use indices to sample correct subpixels
        float4 color = float4(0.0, 0.0, 0.0, 1.0);
        int viewIndex = 0;
        for (int channel = 0; channel < 3; channel++) {
            viewIndex = viewIndices[channel];

            if (test != 0) {
                if (viewIndex == 0) {
                    color[channel] = 1.0;
                }
                continue;
            }
            
            float2 mappedUVCoords = calculateUVForMosaic(viewIndex, uvCoords);
            float4 tmpColor = _colorMosaic.Sample(sampler_colorMosaic, mappedUVCoords);

            if(channel == 0) {
                color.x = tmpColor.x;
            } else if(channel == 1) {
                color.y = tmpColor.y;
            } else if(channel == 2) {
                color.z = tmpColor.z;
            }
        }
        return color;
    }
    ENDHLSL

    // URP Shader
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.universal": "unity=2021.3"
        }

        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                #pragma vertex vert
                #pragma fragment frag

                struct VertAttributes
                {
                    uint vertexID : SV_VertexID;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
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
            ENDHLSL
        }
    }

    // HDRP Shader
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }

        Tags { "RenderType"="Opaque" "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "G3DFullScreen3D"
            
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
                #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

                #pragma vertex vert
                #pragma fragment frag

                struct VertAttributes
                {
                    uint vertexID : SV_VertexID;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
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
            ENDHLSL
        }
    }

    // Built-in Render Pipeline
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment fragSRP

            #include "UnityCG.cginc"

            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.uv = uv;
                o.screenPos = UnityObjectToClipPos(vertex);
                return o;
            }


            float4 fragSRP(v2f i) : SV_Target
            {
                // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
                // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
                i.screenPos.y = viewportHeight - i.screenPos.y;
                return frag(i);
            }

            ENDHLSL
        }
    }
}
