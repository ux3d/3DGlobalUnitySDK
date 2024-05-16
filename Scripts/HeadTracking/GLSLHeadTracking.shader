Shader "G3D/GLSLHeadTracking"
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
            GLSLPROGRAM
            /*
            * Copyright (C) 3D Global GmbH
            *  www.3d-global.com
            */

            #ifdef GL_ES
            // Set default precision to medium
            precision mediump int;
            precision mediump float;
            #endif

            varying vec2 v_texcoord;
            uniform sampler2D texture0; // Textur mit Bildinhalt
            uniform int viewcount;      // Anzahl nativer Views
            uniform int zwinkel;        // Winkelzähler
            uniform int nwinkel;        // Winkelnenner
            uniform int isleft;         // links(1) oder rechts(0) geneigtes Lentikular
            uniform int test;           // Rot/Schwarz (1)ein, (0)aus
            uniform int stest;          // Streifen Rot/Schwarz (1)ein, (0)aus
            uniform int testgap;        // Breite der Lücke im Testbild
            uniform int track;          // Trackingshift
            uniform int mstart;         // Viewshift permanent Offset
            uniform int blur;           // je größer der Wert umso mehr wird verwischt 0-1000 sinnvoll
            uniform int hqview;
            uniform int hviews1;
            uniform int hviews2;
            uniform int bls;
            uniform int ble;
            uniform int brs;
            uniform int bre;
            uniform int bborder;
            uniform int bspace;
            uniform int s_width;        //screen width
            uniform int s_height;       //screen height
            uniform int v_pos_x;        //horizontal viewport position
            uniform int v_pos_y;        //vertical viewport position
            uniform int tvx;            //zCorrectionValue
            uniform int zkom;           //zCompensationValue, kompensiert den Shift der durch die Z-Korrektur entsteht



            // Funktion zur Zufallssimulation, soll ein Rauschen erzeugen(einzig hier für wird noch float benötigt, sollte ein externer Generator erledigen)
            int random3dg(int blu)
            {
                vec2 uv = gl_FragCoord.xy / vec2(float(s_width), float(s_height));
                return int((fract(sin(dot(uv,vec2(12.9898,78.233))) * 43758.5453123)) * float(blu));
            }

            // Modulo Funktion fuer INT
            int modg3d(int a, int b)
            {
                return (a-((a/b) * b));
            }

            // Modulo Funktion fuer IVEC3
            ivec3 modiv3g3d(ivec3 a, int b)
            {
                return (a-((a/b) * b));
            }



            void main()
            {
                // Start der Berechnung von dynamische Daten
                int x = int(gl_FragCoord.x) + v_pos_x;     // transform x position from viewport to screen coordinates
                int y = int(gl_FragCoord.y) + v_pos_y;     // transform y position from viewport to screen coordinates
                if (isleft == 0) y = s_height - y ;        // invertieren für rechts geneigte Linse
                int yw = ((y * zwinkel) / nwinkel);        // Winkelberechnung für die Renderberechnung

                //Start native Renderberechnung
                int sr = ((x * 3) + yw);
                ivec3 xwert = modiv3g3d( ivec3(sr + 0, sr + 1, sr + 2), viewcount);                              // #### viewcount->lt03

                // Start HQ-Renderberechnung inklusive Z-Korrektur

                int hqwert = modg3d((modg3d(y, nwinkel) * zwinkel), nwinkel);
                if (isleft == 1) { hqwert=(nwinkel-1)-hqwert ; }
                if (hqwert<0) hqwert=hqwert * -1 ;
                // abs Funktion
                //    int zwert = (((sr + random3dg(blur))/tvx) * zio) - zkom ;           // Z-Korrektur mit Zufallsprozess
                //int zwert = ((sr/tvx) * zio) - zkom ;
                int zwert = 0;
                if (tvx != 0) {
                    zwert = (sr / tvx) - zkom;
                }

                int tr2d = 0;
                if (track < 0) {
                    tr2d = track;
                }

                ivec3 mtmp = ((hviews1 - xwert) * nwinkel) + hqwert + track + mstart + zwert;
                xwert = hviews1 - modiv3g3d(mtmp, hqview);

                // hier wird der Farbwert des Views aus der Textur geholt und die Ausblendung realisisert
                vec2 stex = v_texcoord / vec2(2.000000, 1.000000);                  // Positionsdaten fuer SBS-Bild erzeugen
                vec2 ctextl=vec2(stex.x,stex.y) ;                                   // Textur-Position rechtes Bild
                vec2 ctextr=vec2((stex.x + 0.5),stex.y) ;                           // Textur-Position linkes Bild
                vec4 colrechts = vec4( texture2D( texture0,ctextr ) ) ;             // Pixeldaten rechtes Bild
                vec4 collinks = vec4( texture2D( texture0,ctextl ) ) ;              // Pixeldaten linkes Bild
                float cor=0.0, cog=0.0 , cob=0.0 ;

                if ( (xwert.r >= bls) && (xwert.r <= ble) )  cor = colrechts.r ;
                if ( (xwert.g >= bls) && (xwert.g <= ble) )  cog = colrechts.g ;
                if ( (xwert.b >= bls) && (xwert.b <= ble) )  cob = colrechts.b ;
                if ( (xwert.r >= brs) && (xwert.r <= bre) )  cor = collinks.r ;
                if ( (xwert.g >= brs) && (xwert.g <= bre) )  cog = collinks.g ;
                if ( (xwert.b >= brs) && (xwert.b <= bre) )  cob = collinks.b ;

                vec4 color = vec4(cor, cog, cob, 1.0); // gerenderte Pixeldaten
                if (0 == tvx) {
                    color = colrechts;
                }

                if (tr2d == -1) {
                    color = colrechts;
                }
                if (tr2d == -2) {
                    color = collinks;
                }

                // Testbilderzeugung Rot Schwarz
                if (test==1)  {  // Testbild ein nativer View-Rot(rechtes Auge) zu View-Grün(linkes Auge) die Views befinden sich direkt am Kanalübergang!
                    color = vec4(0.0, 0.0, 0.0, 1.0);
                    if ((xwert.r > (hviews2 - testgap)) && (xwert.r <= hviews2)) color.r = 1.0 ;
                    if ((xwert.g > hviews2)             && (xwert.g < (hviews2 + testgap)) ) color.g = 1.0 ;
                }

                // Teststreifen Rot Schwarz volle Kanäle ohne Blackmatrix !
                if (y > (s_height - 200) && stest == 1) {
                    color = vec4(0.0,0.0,0.0,1.0);
                    if (xwert.r<=hviews2) color = vec4(1.0, 0.0, 0.0, 1.0);     // rechtes Auge sieht einen roten Streifen
                    if (xwert.g>hviews2) color = vec4(0.0, 1.0, 0.0, 1.0);
                }  // linkes Auge sieht einen gruenen Streifen


                // Bildausgabe
                gl_FragColor = color;
            }
            ENDGLSL
        }
    }
}
