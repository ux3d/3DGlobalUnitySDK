Shader "G3D/HeadTrackingURP"
{
        SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

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
                int  xScreenCoords = int(i.screenPos.x) + v_pos_x;     // transform x position from viewport to screen coordinates
                // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
                // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
                int  yScreenCoords = int(i.screenPos.y) + v_pos_y;     // transform y position from viewport to screen coordinates
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
                float4 colRight = sampleFromView(0, i.uv);             // Pixeldaten rechtes Bild
                float4 colLeft = sampleFromView(1, i.uv);              // Pixeldaten linkes Bild
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
                if (stest == 1) {
                    color = float4(0.0, 0.0, 0.0, 1.0);
                    if (xwert.r <= hviews2) {
                        color = float4(1.0, 0.0, 0.0, 1.0);     // rechtes Auge sieht einen roten Streifen
                    }
                    if (xwert.g > hviews2) {
                        color = float4(0.0, 1.0, 0.0, 1.0);
                    }
                }  // linkes Auge sieht einen gruenen Streifen
                
                // Bildausgabe
                return color;
            }
            ENDHLSL
        }
    }
}