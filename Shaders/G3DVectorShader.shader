Shader "G3D/VectorShader"
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
            float3 userPosition;
            float4 viewPositions;

            Texture2D _PositionMap;
            Texture2D _VectorMap;
            Texture2D _VectorIndexMap0;
            Texture2D _VectorIndexMap1;
            Texture2D _VectorIndexMap2;
            Texture2D _VectorIndexMap3;

            float vectorMapLength;

            UNITY_DECLARE_TEX2D(_View_0);
            UNITY_DECLARE_TEX2D_NOSAMPLER(_View_1);


            float4 sampleFromView(int viewIndex, float2 uv) {
                if (viewIndex == 1024 || viewIndex == 255) return float4(0, 0, 0, 0);

				switch (viewIndex) {
				case 0:
					return UNITY_SAMPLE_TEX2D(_View_0, uv);
				case 1:
					return UNITY_SAMPLE_TEX2D_SAMPLER(_View_1, _View_0, uv);
				}

				return float4(0, 0, 0, 0);
            }

            float3 get_light_vector_indices_from_repitition(int rep, float4 screenPos) {
                //reconstruct value
                float3 L = float3(0, 0, 0);
                float3 H = float3(0, 0, 0);
                switch (rep) {
                case 0:
                    L = _VectorIndexMap0.Load(screenPos).xyz * 255;
                    H = _VectorIndexMap1.Load(screenPos).xyz * 255 * 256; // << 8
                    break;
                case 1:
                    L = _VectorIndexMap2.Load(screenPos).xyz * 255;
                    H = _VectorIndexMap3.Load(screenPos).xyz * 255 * 256; // << 8
                    break;
                }

                return L + H;
            }

            float4 frag(v2f i, UNITY_VPOS_TYPE _screenPos : VPOS) : COLOR
            {
                float4 screenPos = float4(_screenPos.x + windowPosition.x, _screenPos.y + windowPosition.y, 0, 0);

                //real position on screen in mm
                float pos_x_real = _PositionMap.Load(float4(screenPos.x, 0, 0, 0)).r;
                
                int3 viewIndices = int3(1024, 1024, 1024);
                for (int currentRepitition = 0; currentRepitition < 2; currentRepitition++) {
                    //light vector from vectorMap with vectorMapIndices
                    float3 light_vector_indices = get_light_vector_indices_from_repitition(currentRepitition, screenPos);
                    float3 light_vectors = float3(// vvv- indices converted to uvs -vvv
                        _VectorMap.Load(float4(light_vector_indices.x, 0, 0, 0)).r,
                        _VectorMap.Load(float4(light_vector_indices.y, 0, 0, 0)).r,
                        _VectorMap.Load(float4(light_vector_indices.z, 0, 0, 0)).r
                    );

                    //get view indices, keep overlapping ones dark
                    for (int viewPosition = 0; viewPosition < 2; viewPosition++) {
                        float2 vp = float2(
                            userPosition.x + viewPositions[viewPosition * 2], 
                            userPosition.x + viewPositions[viewPosition * 2 + 1]
                        );

                        for (int channel = 0; channel < 3; channel++) {
                            float CrossX = pos_x_real + (userPosition.z * light_vectors[channel]);
                            if ((vp.x <= CrossX) && (CrossX <= vp.y)) {
                                if (viewIndices[channel] == 1024 || viewIndices[channel] == viewPosition) {
                                    viewIndices[channel] = viewPosition;
                                } else {
                                    viewIndices[channel] = 255;
                                }
                            }
                        }
                    }
                }

                //colors via indices
                float4 oColor = float4(0.0, 0.0, 0.0, 1.0);
				for (int channel = 0; channel < 3; channel++) {
                    int viewIndex = viewIndices[channel];
                    oColor[channel] = sampleFromView(viewIndex, i.uv)[channel];
				}

                return oColor;
            }

            ENDCG
        }
    }
}
