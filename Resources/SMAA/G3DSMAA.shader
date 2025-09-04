Shader "G3D/SMAA" {
    Properties
    {
        [HideInInspector] _StencilRef("_StencilRef", Int) = 4
        [HideInInspector] _StencilMask("_StencilMask", Int) = 4
    }

    HLSLINCLUDE

    #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
    #pragma multi_compile_fragment SMAA_PRESET_LOW SMAA_PRESET_MEDIUM SMAA_PRESET_HIGH
    #pragma editor_sync_compilation

    ENDHLSL

    SubShader {
        PackageRequirements {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }
        Tags {
            "RenderPipeline" = "HDRenderPipeline"
        }

        Cull Off ZWrite Off ZTest Always

        Pass {
            Name "EdgeDetection"

            Stencil
            {
                WriteMask [_StencilMask]
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM

                #pragma vertex VertEdge
                #pragma fragment FragEdge
                #include "SMAA/SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }

        Pass {
            Name "BlendingWeightCalculation"

            Stencil
            {
                WriteMask[_StencilMask]
                ReadMask [_StencilMask]
                Ref [_StencilRef]
                Comp Equal
                Pass Replace
            }

            HLSLPROGRAM

                #pragma vertex VertBlend
                #pragma fragment FragBlend
                #include "SMAA/SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }

        Pass {
            Name "NeighborhoodBlending"

            HLSLPROGRAM

                #pragma vertex VertNeighbor
                #pragma fragment FragNeighbor
                #include "SMAA/SubpixelMorphologicalAntialiasingBridge.hlsl"

            ENDHLSL
        }
    }
}
