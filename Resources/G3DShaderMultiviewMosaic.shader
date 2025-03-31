Shader "G3D/AutostereoMultiviewMosaic"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    int  nativeViewCount;      // Anzahl nativer Views
    int  zwinkel;        // Winkelzähler
    int  nwinkel;        // Winkelnenner
    int  isleft;         // links(1) oder rechts(0) geneigtes Lentikular
    int  test;           // Rot/Schwarz (1)ein, (0)aus
    int  hqview;         // hqhqViewCount
    
    
    int  s_height;       // screen height
    int  v_pos_x;        // horizontal viewport position
    int  v_pos_y;        // vertical viewport position
    
    // This shader was originally implemented for OpenGL, so we need to invert the y axis to make it work in Unity.
    // to do this we need the actual viewport height
    int viewportHeight;
    
    // amount of render targets
    int cameraCount;
    
    int mirror; // 1: mirror from left to right, 0: no mirror
    
    // unused parameter -> only here for so that this shader overlaps with the multiview shader
    int isBGR; // 0 = RGB, 1 = BGR

    // mosaic video parameters
    int mosaic_rows = 1; // number of rows in the mosaic
    int mosaic_columns = 1; // number of columns in the mosaic
    
    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 screenPos : SV_POSITION;
    };


    Texture2D mosaictexture;
    SamplerState samplermosaictexture;

    // original code (adapted to remove redundant calculations):
    // map(viewIndex, 0, calculatedViewCount, 0, cameraCount);
    // float map(float s, float from1, float from2, float to1, float to2)
    // {
    //     return to1 + (s-from1)*(to2-to1)/(from2-from1);
    // }
    float map(float s, float from2, float to2)
    {
        return s*to2/from2;
    }

    float2 calculateUVForMosaic(int index, float2 startingUV) {
        int2 moasicIndex = int2(index % mosaic_columns, index / mosaic_rows);
        float2 scaledUV = float2(startingUV.x / mosaic_columns, startingUV.y / mosaic_rows);
        float2 cellSize = float2(1.0 / mosaic_columns, 1.0 / mosaic_rows);
        return scaledUV + cellSize * moasicIndex;
    }


    int3 getSubPixelViewIndices(float2 screenPos)
    {
        uint view = uint(screenPos.x * 3.f + ((screenPos.y * (float(zwinkel) / float(nwinkel))) % float(nativeViewCount)) + float(nativeViewCount));
        int3 viewIndices = int3(view, view, view);

        viewIndices += uint3(0 + (isBGR * 2), 1, 2 - (isBGR * 2));

        viewIndices.x = viewIndices.x % nativeViewCount;
        viewIndices.y = viewIndices.y % nativeViewCount;
        viewIndices.z = viewIndices.z % nativeViewCount;

        return viewIndices;
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
            if(isBGR != 0) {
                viewIndex = viewIndices[2 - channel];
            } else {
                viewIndex = viewIndices[channel];
            }

            if (test != 0) {
                if (viewIndex == 0) {
                    color[channel] = 1.0;
                }
                continue;
            }

            float2 mappedUVCoords = calculateUVForMosaic(viewIndex, uvCoords);
            float4 tmpColor = mosaictexture.Sample(samplermosaictexture, mappedUVCoords);

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
