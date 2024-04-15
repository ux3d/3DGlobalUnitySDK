Shader "G3D/ViewmapShader"
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

			Texture2D _ViewMap;
			float4 windowPosition;
			float indexMap[64];
			int view_count;
			int view_count_monitor_hq;
			int view_offset;
			int view_offset_headtracking;

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

            float4 frag(v2f i, UNITY_VPOS_TYPE screenPos : VPOS) : COLOR
            {
				float4 viewIndices = _ViewMap.Load(float4(screenPos.x + windowPosition.x, screenPos.y + windowPosition.y, 0, 0)) * 255;

				float4 oColor = float4(0.0, 0.0, 0.0, 1.0);
				for (int channel = 0; channel < 3; channel++) {
					int viewIndex = int(viewIndices[channel]) + view_offset + view_offset_headtracking;
					viewIndex = viewIndex % view_count_monitor_hq;
					int textureIndex = indexMap[viewIndex];
					if (textureIndex != 255) {
						oColor[channel] = sampleFromView(textureIndex, i.uv)[channel];
					}
				}

                return oColor;
            }

            ENDCG
        }
    }
}
