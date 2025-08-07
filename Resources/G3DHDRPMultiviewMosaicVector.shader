Shader "G3D/MultiviewMosaicVector"
{
    HLSLINCLUDE
    #include "G3DHLSLCommonFunctions.hlsl"

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 screenPos : SV_POSITION;
    };

    // 0 = left camera, 1 = right camera
    Texture2D _colorMosaic;
    SamplerState sampler_colorMosaic;

    Texture2D _viewMap;
    SamplerState sampler_viewMap;

    uint  screen_height;       // screen height
    uint  screen_width;        // screen width
    uint  viewport_pos_x;        // horizontal viewport position
    uint  viewport_pos_y;        // vertical viewport position
    int viewportHeight;
    int viewportWidth;

    // contains a list mapping view index to actual view (view 0 actually uses texture 0, view 1 uses texture 1, etc. (or shifted))
    // i.e. [0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15]
    uint indexMap[] = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15};
    uint nativeViewCount = 16; // length of indexMap
    int mirror; // 1: mirror from left to right, 0: no mirror


    // viewIndices are the view indices for the three color channels
    // i.e. viewIndex.x maps to view 3, viewIndex.y maps to view 5, and viewIndex.z maps to view 8
    float3 GetColors(int3 viewIndices, v2f i)
    {
        float3 OutColors = float3(0.0, 0.0, 0.0);

        float2 uvCoords = i.uv;
        // mirror the image if necessary
        if (mirror != 0) {
            uvCoords.x = 1.0 - uvCoords.x;
        }


        for (int ColorIndex = 0; ColorIndex < 3; ColorIndex ++)
        {
            int viewIndex = viewIndices[ColorIndex];
            uint TextureIndex = 255;
            if (viewIndex < nativeViewCount)
            {
                TextureIndex = indexMap[viewIndex];
            }

            float2 mappedUVCoords = calculateUVForMosaic(TextureIndex, uvCoords);
            float4 tmpColor = _colorMosaic.Sample(sampler_colorMosaic, mappedUVCoords);

            float Texture1 = 0.0;
            switch (TextureIndex)
            {
            case 254:
                if (ColorIndex == 0)
                Texture1 = 1.0;
                break;
            case 255:
                break;
            default:
                Texture1 = tmpColor[ColorIndex];
                break;
            }
            OutColors[ColorIndex] = Texture1;
        }

        return OutColors;
    }


    float4 frag (v2f i) : SV_Target
    {

        float xScreenCoords = i.uv.x * viewportWidth + viewport_pos_x;
        viewport_pos_y = viewport_pos_y - viewportHeight;
        float yScreenCoords = (viewportHeight - i.uv.y * viewportHeight) + viewport_pos_y;
        // TODO check if this is necessary
        
        // position of window in screen coordinates
        uint2 computedScreenPos = uint2(xScreenCoords, yScreenCoords);
        float2 screenPosUV = float2(computedScreenPos) / float2(screen_width, screen_height);

        // get view indices from view map
        // every color value contain index of view in view array to use for this color
        // this works only, if texture is flipped in y since texelFetch use 0,0 for bottom/left
        float4 ViewIndexFloat = _viewMap.Sample(sampler_viewMap, screenPosUV);
        
        // since value is normalized (0..1), we bring it back to byte value
        int3 ViewIndex = int3(round(ViewIndexFloat.x * 255),
                                  round(ViewIndexFloat.y * 255),
                                  round(ViewIndexFloat.z * 255));

        return float4(GetColors(ViewIndex, i), 1.0f);
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