Shader "G3D/AutostereoMultiview"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "G3D_ShaderBasics.hlsl"

    Texture2D texture0;
    SamplerState samplertexture0;
    Texture2D texture1;
    SamplerState samplertexture1;
    Texture2D texture2;
    SamplerState samplertexture2;
    Texture2D texture3;
    SamplerState samplertexture3;
    Texture2D texture4;
    SamplerState samplertexture4;
    Texture2D texture5;
    SamplerState samplertexture5;
    Texture2D texture6;
    SamplerState samplertexture6;
    Texture2D texture7;
    SamplerState samplertexture7;
    Texture2D texture8;
    SamplerState samplertexture8;
    Texture2D texture9;
    SamplerState samplertexture9;
    Texture2D texture10;
    SamplerState samplertexture10;
    Texture2D texture11;
    SamplerState samplertexture11;
    Texture2D texture12;
    SamplerState samplertexture12;
    Texture2D texture13;
    SamplerState samplertexture13;
    Texture2D texture14;
    SamplerState samplertexture14;
    Texture2D texture15;
    SamplerState samplertexture15;

    float4 sampleFromView(int viewIndex, float2 uv) {
        switch (viewIndex) {
        case 0:
            return texture0.Sample(samplertexture0, uv);
        case 1:
            return texture1.Sample(samplertexture1, uv);
        case 2:
            return texture2.Sample(samplertexture2, uv);
        case 3:
            return texture3.Sample(samplertexture3, uv);
        case 4:
            return texture4.Sample(samplertexture4, uv);
        case 5:
            return texture5.Sample(samplertexture5, uv);
        case 6:
            return texture6.Sample(samplertexture6, uv);
        case 7:
            return texture7.Sample(samplertexture7, uv);
        case 8:
            return texture8.Sample(samplertexture8, uv);
        case 9: 
            return texture9.Sample(samplertexture9, uv);
        case 10:
            return texture10.Sample(samplertexture10, uv);
        case 11:
            return texture11.Sample(samplertexture11, uv);
        case 12:
            return texture12.Sample(samplertexture12, uv);
        case 13:
            return texture13.Sample(samplertexture13, uv);
        case 14:
            return texture14.Sample(samplertexture14, uv);
        case 15:
            return texture15.Sample(samplertexture15, uv);
        case 16:
            return texture0.Sample(samplertexture0, uv);
        case 17:
            return texture1.Sample(samplertexture1, uv);
        case 18:
            return texture2.Sample(samplertexture2, uv);
        }

        return float4(0, 0, 0, 0);
    }

    float4 frag (v2f i) : SV_Target
    {
        return sampleFromView(nativeViewCount - 0 - 1, i.uv);

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

            float4 tmpColor = sampleFromView(viewIndex, uvCoords);

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
