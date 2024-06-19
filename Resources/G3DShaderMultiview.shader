Shader "G3D/Autostereo"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    int  hqViewCount;      // Anzahl nativer Views
    int  zwinkel;        // Winkelzähler
    int  nwinkel;        // Winkelnenner
    int  isleft;         // links(1) oder rechts(0) geneigtes Lentikular
    int  test;           // Rot/Schwarz (1)ein, (0)aus
    int  stest;          // Streifen Rot/Schwarz (1)ein, (0)aus
    int  testgap;        // Breite der Lücke im Testbild
    int  track;          // Trackingshift
    int  mstart;         // Viewshift permanent Offset
    int  blur;           // je größer der Wert umso mehr wird verwischt 0-1000 sinnvoll
    int  hqview;         // hqhqViewCount
    int  hviews1;          // hqview - 1
    int  hviews2;       // hqview / 2

    int  bls;            // black left start (start and end points of left and right "eye" window)
    int  ble;         // black left end 
    int  brs;          // black right start
    int  bre;      // black right end 
        
    int  bborder;        // blackBorder schwarz verblendung zwischen den views?
    int  bspace;         // blackSpace
    int  s_width;        // screen width
    int  s_height;       // screen height
    int  v_pos_x;        // horizontal viewport position
    int  v_pos_y;        // vertical viewport position
    int  tvx;            // zCorrectionValue
    int  zkom;           // zCompensationValue, kompensiert den Shift der durch die Z-Korrektur entsteht

    // This shader was originally implemented for OpenGL, so we need to invert the y axis to make it work in Unity.
    // to do this we need the actual viewport height
    int viewportHeight; 

    // unused parameter -> only here for so that this shader overlaps with the multiview shader
    int isBGR; // 0 = RGB, 1 = BGR

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 screenPos : SV_POSITION;
    };


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
        }

        return float4(0, 0, 0, 0);
    }

    int3 mod_i(int3 v, int m) {
        return v - (v / m) * m;
    }

    float4 frag (v2f i) : SV_Target
    {
        // Start der Berechnung von dynamische Daten
        int  xScreenCoords = int(i.screenPos.x) + v_pos_x;     // transform x position from viewport to screen coordinates
        // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
        // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
        int  yScreenCoords = int(i.screenPos.y) + v_pos_y;     // transform y position from viewport to screen coordinates

        if (isleft == 0) {
            yScreenCoords = s_height - yScreenCoords ;        // invertieren für rechts geneigte Linse
        }

        int hqViewCount = hqViewCount * nwinkel;
        /*
        1) startIndex
            each horizontal line starts with a different view index:
                (y * uAngleCounter) % max_views
                e.g.: uAngleCounter = 4 and max_views = 35: 34,3,7,11,15,19,23,27,31,0,4,...
            depending on the screen, this view index will increase or decrease in its modulo loop, therefore:
                uDirection (either -1 or 1)
            handling negative modulo mod(a,b) means we need to add b IF the result is negative, but we dont want that IF, therefore:
                + y * hqViewCount
            each step on the horizontal line in x increases the startIndex by wn (on a subpixel level), therefore:
                startIndex + xScreenCoords * 3 * unwinkel
        */
        int startIndex = yScreenCoords * angleCounter * (direction * -1) + yScreenCoords * hqViewCount + xScreenCoords * 3 * nwinkel;

        /*
        2) viewIndex
            in a shader we operate on a pixel level tho, so we need to add wn:
                + float3(0, unwinkel, 2*unwinkel)
            offsets from the UI or headtracking can factor into this as well:
                view_offset + view_offset_headtracking
            of course, this value will be out of range for our views, so we need a modulo operation that can only behave as expected
                glsl_mod(float3, float) = f - floor(f / m) * m;
        */
        ivec3 viewIndices = ivec3(startIndex, startIndex, startIndex);
        viewIndices += ivec3(0, nwinkel, nwinkel + nwinkel);
        viewIndices += viewOffset;
        viewIndices += viewOffsetHeadtracking;
        viewIndices = mod_i(viewIndices, hqViewCount);

        //use indices to sample correct subpixels
        pixel = vec4(0.0, 0.0, 0.0, 1.0);
        int viewIndex = 0;
        for (int channel = 0; channel < 3; channel++) {
            if(bgr != 0) {
                viewIndex = viewIndices[2 - channel];
            } else {
                viewIndex = viewIndices[channel];
            }

            if (testmode != 0) {
                if (viewIndex == 0) {
                    pixel[channel] = 1.0;
                }
                continue;
            }
            
            int textureIndex = int(textureLod(indexmap, vec2(viewIndex, 0), 0).r); //texture index according to the configured index map
            if (textureIndex == 250) {
                continue; //black, which is what we started with
            }

            pixel[channel] = texture(tex, vec3(uv, textureIndex))[channel];
        }

        
        float4 color = float4(1.0, 0.0, 0.0, 1.0); // gerenderte Pixeldaten
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
