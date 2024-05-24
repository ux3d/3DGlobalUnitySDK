Shader "G3D/HeadTrackingHDRP"
{
    HLSLINCLUDE
    #pragma vertex Vert

    #pragma target 4.5
    #pragma only_renderers d3d11 playstation xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"


    int  viewcount;      // Anzahl nativer Views
    int  zwinkel;        // Winkelzähler
    int  nwinkel;        // Winkelnenner
    int  isleft;         // links(1) oder rechts(0) geneigtes Lentikular
    int  test;           // Rot/Schwarz (1)ein, (0)aus
    int  stest;          // Streifen Rot/Schwarz (1)ein, (0)aus
    int  testgap;        // Breite der Lücke im Testbild
    int  track;          // Trackingshift
    int  mstart;         // Viewshift permanent Offset
    int  blur;           // je größer der Wert umso mehr wird verwischt 0-1000 sinnvoll
    int  hqview;         // hqViewCount
    int  hviews1;
    int  hviews2;
    int  bls;
    int  ble;
    int  brs;
    int  bre;
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

    Texture2D texture0;
    SamplerState sampler_texture0;
    Texture2D texture1;
    Texture2D texture2;
    Texture2D texture3;
    Texture2D texture4;
    Texture2D texture5;
    Texture2D texture6;
    Texture2D texture7;
    Texture2D texture8;
    Texture2D texture9;
    Texture2D texture10;
    Texture2D texture11;
    Texture2D texture12;
    Texture2D texture13;
    Texture2D texture14;
    Texture2D texture15;

    float4 sampleFromView(int viewIndex, float2 uv) {
        switch (viewIndex) {
        case 0:
            return texture0.Sample(sampler_texture0, uv);
        case 1:
            return texture1.Sample(sampler_texture0, uv);
        case 2:
            return texture2.Sample(sampler_texture0, uv);
        }

        return float4(0, 0, 0, 0);
    }

    float4 FullScreenPass(Varyings varyings) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(varyings);
        float depth = LoadCameraDepth(varyings.positionCS.xy);
        PositionInputs posInput = GetPositionInput(varyings.positionCS.xy, _ScreenSize.zw, depth, UNITY_MATRIX_I_VP, UNITY_MATRIX_V);

        // Start der Berechnung von dynamische Daten
        int  xScreenCoords = int(posInput.positionSS.x) + v_pos_x;     // transform x position from viewport to screen coordinates
        // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
        // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
        int  yScreenCoords = int(viewportHeight - posInput.positionSS.y) + v_pos_y;     // transform y position from viewport to screen coordinates
        if (isleft == 0) {
            yScreenCoords = s_height - yScreenCoords ;        // invertieren für rechts geneigte Linse
        }
        int  yw = int(yScreenCoords * zwinkel) / nwinkel;        // Winkelberechnung für die Renderberechnung

        //Start native Renderberechnung
        int  sr = (xScreenCoords * 3) + yw;
        int3 xwert = int3(sr + 0, sr + 1, sr + 2) % viewcount;                              // #### viewcount->lt03
        // int3 xwert = modiv3g3d( int3(sr + 0, sr + 1, sr + 2), viewcount);                              // #### viewcount->lt03

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

        // hier wird der Farbwert des Views aus der Textur geholt und die Ausblendung realisisert
        float4 colRight = sampleFromView(0, posInput.positionNDC);             // Pixeldaten rechtes Bild
        float4 colLeft = sampleFromView(1, posInput.positionNDC);              // Pixeldaten linkes Bild
        float cor=0.0, cog=0.0, cob=0.0;

        
        if ( (xwert.r >= bls) && (xwert.r <= ble) )  cor = colRight.r ;
        if ( (xwert.g >= bls) && (xwert.g <= ble) )  cog = colRight.g ;
        if ( (xwert.b >= bls) && (xwert.b <= ble) )  cob = colRight.b ;
        if ( (xwert.r >= brs) && (xwert.r <= bre) )  cor = colLeft.r ;
        if ( (xwert.g >= brs) && (xwert.g <= bre) )  cog = colLeft.g ;
        if ( (xwert.b >= brs) && (xwert.b <= bre) )  cob = colLeft.b ;
        
        float4 color = float4(cor, cog, cob, 1.0); // gerenderte Pixeldaten
        if (0 == tvx) {
            color = colRight;
        }
        
        if (tr2d == -1) {
            color = colRight;
        }
        if (tr2d == -2) {
            color = colLeft;
        }

        // Testbilderzeugung Rot Schwarz
        if (test==1)  {  // Testbild ein nativer View-Rot(rechtes Auge) zu View-Grün(linkes Auge) die Views befinden sich direkt am Kanalübergang!
            color = float4(0.0, 0.0, 0.0, 1.0);
            if ((xwert.r > (hviews2 - testgap)) && (xwert.r <= hviews2)) color.r = 1.0 ;
            if ((xwert.g > hviews2)             && (xwert.g < (hviews2 + testgap)) ) color.g = 1.0 ;
        }

        // Teststreifen Rot Schwarz volle Kanäle ohne Blackmatrix !
        if (yScreenCoords > (s_height - 200) && stest == 1) {
            color = float4(0.0, 0.0, 0.0, 1.0);
            if (xwert.r<=hviews2) color = float4(1.0, 0.0, 0.0, 1.0);     // rechtes Auge sieht einen roten Streifen
            if (xwert.g>hviews2) color = float4(0.0, 1.0, 0.0, 1.0);
        }  // linkes Auge sieht einen gruenen Streifen

        return color;
        // return float4(posInput.positionNDC.x, posInput.positionNDC.y, 0.1, 1.0);
    }
    ENDHLSL

    SubShader
    {
        Tags{ "RenderPipeline" = "HDRenderPipeline" }
        Pass
        {
            Name "G3D Pass 0"

            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off

            HLSLPROGRAM
                #pragma fragment FullScreenPass
            ENDHLSL
        }
    }
    Fallback Off
}
