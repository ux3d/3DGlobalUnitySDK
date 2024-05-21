Shader "G3D/HeadTracking"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

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

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : SV_POSITION;
            };

            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
            {
                v2f o;
                o.uv = uv;
                o.screenPos = UnityObjectToClipPos(vertex);
                return o;
            }

            UNITY_DECLARE_TEX2D(_view_0);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_1);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_2);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_3);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_4);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_5);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_6);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_7);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_8);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_9);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_10);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_11);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_12);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_13);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_14);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_15);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_view_16);

            float4 sampleFromView(int  viewIndex, float2 uv) {
				switch (viewIndex) {
				case 0:
					return UNITY_SAMPLE_TEX2D(_view_0, uv);
				case 1:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_1, _view_0, uv);
				case 2:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_2, _view_0, uv);
				case 3:										 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_3, _view_0, uv);
				case 4:										 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_4, _view_0, uv);
				case 5:									 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_5, _view_0, uv);
				case 6:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_6, _view_0, uv);
				case 7:									
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_7, _view_0, uv);
				case 8:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_8, _view_0, uv);
				case 9:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_9, _view_0, uv);
				case 10:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_10, _view_0, uv);
				case 11:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_11, _view_0, uv);
				case 12:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_12, _view_0, uv);
				case 13:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_13, _view_0, uv);
				case 14:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_14, _view_0, uv);
				case 15:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_15, _view_0, uv);
				case 16:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_view_16, _view_0, uv);
				}

				return float4(0, 0, 0, 0);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Start der Berechnung von dynamische Daten
                int  xScreenCoords = int(i.screenPos.x) + v_pos_x;     // transform x position from viewport to screen coordinates
                // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
                // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
                int  yScreenCoords = int(viewportHeight - i.screenPos.y) + v_pos_y;     // transform y position from viewport to screen coordinates
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
                int  zwert = 0;
                if (tvx != 0) {
                    zwert = (sr / tvx) - zkom;
                }

                int  tr2d = 0;
                if (track < 0) {
                    tr2d = track;
                }

                int3 mtmp = ((hviews1 - xwert) * nwinkel) + hqwert + track + mstart + zwert;
                xwert = hviews1 - (mtmp % hqview);
                // xwert = hviews1 - modiv3g3d(mtmp, hqview);

                // hier wird der Farbwert des Views aus der Textur geholt und die Ausblendung realisisert
                float4 colRight = sampleFromView(0, i.uv);             // Pixeldaten rechtes Bild
                float4 colLeft = sampleFromView(1, i.uv);              // Pixeldaten linkes Bild
                float cor=0.0, cog=0.0 , cob=0.0;

                
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
                    color = float4(0.0,0.0,0.0,1.0);
                    if (xwert.r<=hviews2) color = float4(1.0, 0.0, 0.0, 1.0);     // rechtes Auge sieht einen roten Streifen
                    if (xwert.g>hviews2) color = float4(0.0, 1.0, 0.0, 1.0);
                }  // linkes Auge sieht einen gruenen Streifen



                // Bildausgabe
                return color;
            }
            ENDCG
        }
    }
}
