Shader "G3D/ZTrackingShader"
{
    Properties
    {
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0, out float4 screenPos : SV_POSITION)
            {
                v2f o;
                o.uv = uv;
                screenPos = UnityObjectToClipPos(vertex);
                return o;
            }

			float4 windowPosition;

			float indexMap[64];
			int view_count_monitor_hq;
			int view_offset;
			int view_offset_headtracking;
			int angle_counter;
			int angle_denominator;
            int direction;
            int blur_factor;
            float3 userPosition; //x,y,z in mm.

			UNITY_DECLARE_TEX2D(_View_0);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_1);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_2);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_3);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_4);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_5);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_6);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_7);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_8);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_9);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_10);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_11);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_12);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_13);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_14);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_15);
			UNITY_DECLARE_TEX2D_NOSAMPLER(_View_16);

            float4 sampleFromView(int viewIndex, float2 uv) {
				switch (viewIndex) {
				case 0:
					return UNITY_SAMPLE_TEX2D(_View_0, uv);
				case 1:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_1, _View_0, uv);
				case 2:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_2, _View_0, uv);
				case 3:										 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_3, _View_0, uv);
				case 4:										 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_4, _View_0, uv);
				case 5:									 
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_5, _View_0, uv);
				case 6:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_6, _View_0, uv);
				case 7:									
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_7, _View_0, uv);
				case 8:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_8, _View_0, uv);
				case 9:										
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_9, _View_0, uv);
				case 10:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_10, _View_0, uv);
				case 11:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_11, _View_0, uv);
				case 12:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_12, _View_0, uv);
				case 13:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_13, _View_0, uv);
				case 14:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_14, _View_0, uv);
				case 15:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_15, _View_0, uv);
				case 16:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_16, _View_0, uv);
				}

				return float4(0, 0, 0, 0);
            }

            float3 glsl_modulo(float3 f, float m) {
                return f - floor(f / m) * m;
            }

            float3 glsl_mod(float3 f, float m) {
                return glsl_modulo(glsl_modulo(f, m) + m, m); //2 mod for negative values, could be avoided if no negative values are passed to this function
            }

            float random(float2 uv) {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float4 frag(v2f i, UNITY_VPOS_TYPE _screenPos : VPOS) : COLOR
            {
                int2 screenPos = int2(_screenPos.x + windowPosition.x, _screenPos.y + windowPosition.y);
                float angle = (float)angle_counter / angle_denominator;


                float4 oColor = float4(0.0, 0.0, 0.0, 1.0);

                /*
                1) startIndex
                    each horizontal line starts with a different view index:
                        (y * angle_counter) % max_views
                        e.g.: angle_counter = 4 and max_views = 35: 34,3,7,11,15,19,23,27,31,0,4,...
                    depending on the screen, this view index will increase or decrease in its modulo loop, therefore:
                        direction (either -1 or 1)
                */
                float startIndex = screenPos.y * angle_counter * direction;

                /*
                2) mid point correction offset
                    the views need to remain stable with different z positions
                    this factors into the calculations with another offset
                */

                float midPoint = ((_ScreenParams.x / 2.0) * 3.0) + (_ScreenParams.y / 2.0) * -angle;
                float view_offset_midpoint = midPoint / userPosition.z;

                /*
                3) viewIndex
                    each step along the horizontal line in x increases the startIndex by wn (on a subpixel level), therefore:
                        startIndex + screenPos.x * 3 * angle_denominator
                        e.g.: angle_denominator = 5 and max_views = 35 with startIndex = 0: 0,5,10,15,20,25,30,0,5,10,...
                    in a shader we operate on a pixel level tho, so we need to add wn:
                        + float3(0, angle_denominator, angle_denominator + angle_denominator)
                    offsets from the UI, headtracking or midpoint correction can factor into this as well:
                        view_offset + view_offset_headtracking + view_offset_midpoint
                    of course, this value will be out of range for our views, so we need a modulo operation
                */


                float3 viewIndex = glsl_mod(
                    (startIndex + screenPos.x * 3 * angle_denominator)
                    + float3(0, angle_denominator, angle_denominator + angle_denominator)
                    + view_offset + view_offset_headtracking + view_offset_midpoint,
                    view_count_monitor_hq
                );

                /*
                example 10x10 with 7 views, 4/5 angle and direction = -1:
                     0  5 10 15 20 25 30  0  5 10
                    31  1  6 11 16 21 26 31  1  6
                    27 32  2  7 12 17 22 27 32  2
                    23 28 33  3  8 13 18 23 28 33
                    19 24 29 34  4  9 14 19 24 29
                    15 20 25 30  0  5 10 15 20 25
                    11 16 21 26 31  1  6 11 16 21
                     7 12 17 22 27 32  2  7 12 17
                     3  8 13 18 23 28 33  3  8 13
                    34  4  9 14 19 24 29 34  4  9

                    in y the next row always starts with angle_counter less, each element in x is increased by angle_denominator
                    also see: ViewMapCalculator (Delphi)
                    note that there are multiple ways to create an algorithm that produces valid results
                    its just one of the versions I fully understand
                */

                /*
                4) z-tracking
                    The user distance pushes the indexMap around (imagine a queue).
                    Here, instead of pushing the indexMap around, the viewIndex is modified.
                    The offset is the total distance we currently have in x (x subpixel position + y offset) devided by the user position.

                    The idea is to use views that would, at a certain distance, show a wrong view and correct the index.
                    For more details refer to Ulli.
                */
                float rand = (random(i.uv) - 0.5) * blur_factor;
                float offset_z = glsl_mod(
                    floor(
                        ((screenPos.x + rand) * 3 + screenPos.y * -angle)// x subpixel position + angle offset (used in another form of this algo to determine current viewIndex)
                        / userPosition.z                                                             // user distance to the screen in mm
                    ),
                    view_count_monitor_hq
                );


                viewIndex = glsl_mod(viewIndex - offset_z, view_count_monitor_hq);

                /*
                5) colors
                    fetching colors according to the viewIndex should be trivial
                    unfortunately, at the time of writing, unity handles texture2D arrays very poorly and I was not able to get rid of that horrible switch case statement
                */
                for (int channel = 0; channel < 3; channel++) {
                    oColor[channel] = sampleFromView(indexMap[viewIndex[channel]], i.uv)[channel];
                }

                return oColor;
            }

            ENDCG
        }
    }
}
