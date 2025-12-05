Shader "G3D/Autostereo"
{
    HLSLINCLUDE
    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "G3D_ShaderBasics.hlsl"


    Texture2D texture0;
    SamplerState samplertexture0;
    Texture2D texture1;
    SamplerState samplertexture1;

    float4 sampleFromView(int viewIndex, float2 uv) {
        switch (viewIndex) {
        case 0:
            return texture0.Sample(samplertexture0, uv);
        case 1:
            return texture1.Sample(samplertexture1, uv);
        }

        return float4(0, 0, 0, 0);
    }

    float4 frag (v2f i) : SV_Target
    {
        // Start der Berechnung von dynamische Daten
        int  xScreenCoords = int(i.screenPos.x) + viewport_pos_x;     // transform x position from viewport to screen coordinates
        // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
        // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
        int  yScreenCoords = int(i.screenPos.y) + viewport_pos_y;     // transform y position from viewport to screen coordinates
        if (isleft == 0) {
            yScreenCoords = screen_height - yScreenCoords ;        // invertieren für rechts geneigte Linse
        }
        int  yw = int(yScreenCoords * zwinkel) / nwinkel;        // Winkelberechnung für die Renderberechnung

        //Start native Renderberechnung
        int  sr = (xScreenCoords * 3) + yw;
        int3 xwert = int3(sr + 0, sr + 1, sr + 2) % nativeViewCount;                              // #### nativeViewCount->lt03
        // int3 xwert = modiv3g3d( int3(sr + 0, sr + 1, sr + 2), nativeViewCount);                              // #### nativeViewCount->lt03

        // Start HQ-Renderberechnung inklusive Z-Korrektur

        int  hqwert = ((yScreenCoords % nwinkel) * zwinkel) % nwinkel;
        // int  hqwert = modg3d((modg3d(yScreenCoords, nwinkel) * zwinkel), nwinkel);
        if (isleft == 1)
        {
            hqwert = (nwinkel - 1) - hqwert;
        }
        if (hqwert < 0)
        {
            hqwert = hqwert * -1;
        }
        int zwert = 0;
        if (tvx != 0) {
            zwert = (sr / tvx) - zkom;
        }

        int tr2d = 0;
        if (track < 0) {
            tr2d = track;
        }

        int3 mtmp = ((hviews1 - xwert) * nwinkel) + hqwert + track + mstart + zwert;
        xwert = hviews1 - (mtmp % hqview);
        // xwert = hviews1 - modiv3g3d(mtmp, hqview);

        float2 uvCoords = i.uv;
        // mirror the image if necessary
        if (mirror != 0) {
            uvCoords.x = 1.0 - uvCoords.x;
        }

        // hier wird der Farbwert des Views aus der Textur geholt und die Ausblendung realisisert
        float4 colorLeft = sampleFromView(0, uvCoords);              // Pixeldaten linkes Bild
        float4 colorRight = sampleFromView(1, uvCoords);             // Pixeldaten rechtes Bild
        float cor=0.0, cog=0.0, cob=0.0;

        
        if ( (xwert.r >= bls) && (xwert.r <= ble) )  cor = colorLeft.r ;
        if ( (xwert.g >= bls) && (xwert.g <= ble) )  cog = colorLeft.g ;
        if ( (xwert.b >= bls) && (xwert.b <= ble) )  cob = colorLeft.b ;
        if ( (xwert.r >= brs) && (xwert.r <= bre) )  cor = colorRight.r ;
        if ( (xwert.g >= brs) && (xwert.g <= bre) )  cog = colorRight.g ;
        if ( (xwert.b >= brs) && (xwert.b <= bre) )  cob = colorRight.b ;
        
        float4 color = float4(cor, cog, cob, 1.0); // gerenderte Pixeldaten
        if (0 == tvx) {
            color = colorLeft;
        }
        
        if (tr2d == -1) {
            color = colorLeft;
        }
        if (tr2d == -2) {
            color = colorRight;
        }

        // Testbilderzeugung Rot Schwarz
        if (test==1)  {  // Testbild ein nativer View-Rot(rechtes Auge) zu View-Grün(linkes Auge) die Views befinden sich direkt am Kanalübergang!
            color = float4(0.0, 0.0, 0.0, 1.0);
            if ((xwert.r > (hviews2 - testgap)) && (xwert.r <= hviews2)) color.r = 1.0 ;
            if ((xwert.g > hviews2)             && (xwert.g < (hviews2 + testgap)) ) color.g = 1.0 ;
        }

        // Teststreifen Rot Schwarz volle Kanäle ohne Blackmatrix !
        if (stest == 1) {
            color = float4(0.0, 0.0, 0.0, 1.0);
            if (xwert.r <= hviews2) {
                color = float4(1.0, 0.0, 0.0, 1.0);     // rechtes Auge sieht einen roten Streifen
            }
            if (xwert.g > hviews2) {
                color = float4(0.0, 1.0, 0.0, 1.0);
            }
        }  // linkes Auge sieht einen gruenen Streifen
        
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
