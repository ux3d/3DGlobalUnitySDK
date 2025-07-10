Shader "G3D/ViewGeneration"
{
    Properties
    {
        DepthMap ("Texture", 2D) = "green" {}
    }
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.high-definition": "unity=2021.3"
        }
        Tags { "RenderType"="Opaque" "RenderPipeline" = "HDRenderPipeline"}

        Pass
        {
            Name "G3DViewGeneration"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

            #pragma vertex vert
            #pragma fragment fragHDRP

            Texture2D DepthMap;
            SamplerState samplerDepthMap;

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

            float4 fragHDRP (v2f i) : SV_Target
            {
                // float yPos = s_height - i.screenPos.y; // invert y coordinate to account for different coordinates between glsl and hlsl (original shader written in glsl)
                // float2 computedScreenPos = float2(i.screenPos.x, i.screenPos.y) + float2(v_pos_x, v_pos_y);
                // int3 viewIndices = getSubPixelViewIndices(computedScreenPos);
                
                float2 uvCoords = i.uv;

                // // mirror the image if necessary
                // if (mirror != 0) {
                //     uvCoords.x = 1.0 - uvCoords.x;
                // }
                
                float4 depthMap = DepthMap.Sample(samplerDepthMap, uvCoords);
                return float4(depthMap.x, depthMap.x, depthMap.x, 1.f);
            }
            ENDHLSL
        }
    }
}


// Shader "G3D/ViewGeneration"
// {
//     HLSLINCLUDE
//     #pragma target 4.5
//     #pragma only_renderers d3d11 playstation xboxone vulkan metal switch


//     // #if defined (SHADER_API_GAMECORE)
//     // #include "Packages/com.unity.render-pipelines.gamecore/ShaderLibrary/API/GameCore.hlsl"
//     // #elif defined(SHADER_API_XBOXONE)
//     // #include "Packages/com.unity.render-pipelines.xboxone/ShaderLibrary/API/XBoxOne.hlsl"
//     // #elif defined(SHADER_API_PS4)
//     // #include "Packages/com.unity.render-pipelines.ps4/ShaderLibrary/API/PSSL.hlsl"
//     // #elif defined(SHADER_API_PS5)
//     // #include "Packages/com.unity.render-pipelines.ps5/ShaderLibrary/API/PSSL.hlsl"
//     // #elif defined(SHADER_API_D3D11)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
//     // #elif defined(SHADER_API_METAL)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Metal.hlsl"
//     // #elif defined(SHADER_API_VULKAN)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Vulkan.hlsl"
//     // #elif defined(SHADER_API_SWITCH)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Switch.hlsl"
//     // #elif defined(SHADER_API_GLCORE)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLCore.hlsl"
//     // #elif defined(SHADER_API_GLES3)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES3.hlsl"
//     // #elif defined(SHADER_API_GLES)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES2.hlsl"
//     // #else
//     // #error unsupported shader api
//     // #endif

//     // #if defined(SHADER_API_D3D11)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/D3D11.hlsl"
//     // #elif defined(SHADER_API_METAL)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Metal.hlsl"
//     // #elif defined(SHADER_API_VULKAN)
//     // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Vulkan.hlsl"
//     // #else
//     // #error unsupported shader api
//     // #endif

//     // #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/TextureXR.hlsl"

    
//     struct v2f
//     {
//         float2 uv : TEXCOORD0;
//         float4 screenPos : SV_POSITION;
//     };


//     // Texture2D DepthMap;
//     // SamplerState samplerDepthMap;

//     // TEXTURE2D_X(_CameraDepthTexture);
//     // SAMPLER(sampler_CameraDepthTexture);



//     float4 frag (v2f i) : SV_Target
//     {
//         // float yPos = s_height - i.screenPos.y; // invert y coordinate to account for different coordinates between glsl and hlsl (original shader written in glsl)
//         // float2 computedScreenPos = float2(i.screenPos.x, i.screenPos.y) + float2(v_pos_x, v_pos_y);
//         // int3 viewIndices = getSubPixelViewIndices(computedScreenPos);
        
//         float2 uvCoords = i.uv;

//         // // mirror the image if necessary
//         // if (mirror != 0) {
//         //     uvCoords.x = 1.0 - uvCoords.x;
//         // }
        
//         // float4 depthMap = _CameraDepthTexture.Sample(sampler_CameraDepthTexture, uvCoords);
//         // float depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uvCoords, 0).r;
//         return float4(1, 0, 0, 1.f);
//     }
//     ENDHLSL

//     // URP Shader
//     SubShader
//     {
//         PackageRequirements
//         {
//             "com.unity.render-pipelines.universal": "unity=2021.3"
//         }

//         Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
//         LOD 100
//         Cull Off

//         Pass
//         {
//             ZTest Always
//             Blend Off
//             Cull Off

//             HLSLPROGRAM
//                 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

//                 #pragma vertex vert
//                 #pragma fragment frag

//                 struct VertAttributes
//                 {
//                     uint vertexID : SV_VertexID;
//                     UNITY_VERTEX_INPUT_INSTANCE_ID
//                 };

//                 v2f vert(VertAttributes input)
//                 {
//                     v2f output;
//                     UNITY_SETUP_INSTANCE_ID(input);
//                     UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//                     output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
//                     output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

//                     return output;

//                 }
//             ENDHLSL
//         }
//     }

//     // HDRP Shader
//     SubShader
//     {
//         PackageRequirements
//         {
//             "com.unity.render-pipelines.high-definition": "unity=2021.3"
//         }

//         Tags { "RenderType"="Opaque" "RenderPipeline" = "HDRenderPipeline"}

//         Pass
//         {
//             Name "G3DFullScreen3D"

//             ZTest Always
//             Blend Off
//             Cull Off

//             HLSLPROGRAM
//                 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassCommon.hlsl"
//                 #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"

//                 #pragma vertex vert
//                 #pragma fragment fragHDRP

//                 Texture2D DepthMap;
//                 SamplerState samplerDepthMap;

//                 struct VertAttributes
//                 {
//                     uint vertexID : SV_VertexID;
//                     UNITY_VERTEX_INPUT_INSTANCE_ID
//                 };

//                 v2f vert(VertAttributes input)
//                 {
//                     v2f output;
//                     UNITY_SETUP_INSTANCE_ID(input);
//                     UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
//                     output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
//                     output.screenPos = GetFullScreenTriangleVertexPosition(input.vertexID);

//                     return output;

//                 }

//                 float4 fragHDRP (v2f i) : SV_Target
//                 {
//                     // float yPos = s_height - i.screenPos.y; // invert y coordinate to account for different coordinates between glsl and hlsl (original shader written in glsl)
//                     // float2 computedScreenPos = float2(i.screenPos.x, i.screenPos.y) + float2(v_pos_x, v_pos_y);
//                     // int3 viewIndices = getSubPixelViewIndices(computedScreenPos);
                    
//                     float2 uvCoords = i.uv;

//                     // // mirror the image if necessary
//                     // if (mirror != 0) {
//                     //     uvCoords.x = 1.0 - uvCoords.x;
//                     // }
                    
//                     float4 depthMap = DepthMap.Sample(samplerDepthMap, uvCoords);
//                     float depth = LoadCameraDepth(uvCoords);
//                     return float4(depth, 0, 0, 1.f);
//                 }
//             ENDHLSL
//         }
//     }

//     // Built-in Render Pipeline
//     SubShader
//     {
//         // No culling or depth
//         Cull Off ZTest Always

//         Pass
//         {
//             HLSLPROGRAM
//             #pragma vertex vert
//             #pragma fragment fragSRP

//             #include "UnityCG.cginc"

//             v2f vert(float4 vertex : POSITION, float2 uv : TEXCOORD0)
//             {
//                 v2f o;
//                 o.uv = uv;
//                 o.screenPos = UnityObjectToClipPos(vertex);
//                 return o;
//             }


//             float4 fragSRP(v2f i) : SV_Target
//             {
//                 // invert y axis to account for different coordinate systems between Unity and OpenGL (OpenGL has origin at bottom left)
//                 // The shader was written for OpenGL, so we need to invert the y axis to make it work in Unity.
//                 // i.screenPos.y = viewportHeight - i.screenPos.y;
//                 return frag(i);
//             }

//             ENDHLSL
//         }
//     }
// }
