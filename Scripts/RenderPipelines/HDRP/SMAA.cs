#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class SMAA : FullScreenCustomPass
    {
        public RTHandle mosaicImageHandle;

        public RTHandle computeShaderResultTextureHandle;

        private RTHandle smaaEdgesTexHandle;
        private RTHandle smaaBlendTexHandle;

        private Material blitMaterial;

        private Material smaaMaterial;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            smaaMaterial = new Material(Shader.Find("G3D/SMAA"));
            smaaMaterial.SetTexture("_AreaTex", Resources.Load<Texture2D>("SMAA/AreaTex"));
            // Import search tex as PNG because I can't get Unity to work with an R8 DDS file properly.
            smaaMaterial.SetTexture("_SearchTex", Resources.Load<Texture2D>("SMAA/SearchTex"));

            blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
            blitMaterial.SetTexture(
                Shader.PropertyToID("_mainTex"),
                computeShaderResultTextureHandle
            );
        }

        public void CreateSMAATextures(int width, int height)
        {
            releaseSMAATextures();

            smaaEdgesTexHandle = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    width,
                    height,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    name: "SMAAEdgesTex",
                    enableRandomWrite: true
                );

            smaaBlendTexHandle = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    width,
                    height,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    name: "SMAABlendTex",
                    enableRandomWrite: true
                );

            computeShaderResultTextureHandle = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    width,
                    height,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    enableRandomWrite: true
                );
        }

        private void releaseSMAATextures()
        {
            smaaBlendTexHandle?.Release();
            smaaEdgesTexHandle?.Release();
            computeShaderResultTextureHandle?.Release();
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            HDAdditionalCameraData hdCamera = camera.GetComponent<HDAdditionalCameraData>();
            if (Helpers.isMainG3DCamera(camera))
            {
                runSMAA(ctx, hdCamera);

                // Blit the result of the compute shader to the camera's color buffer
                CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, blitMaterial, shaderPassId: 0);
            }
        }

        private void runSMAA(CustomPassContext ctx, HDAdditionalCameraData hdCamera)
        {
            smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
            smaaMaterial.SetVector(
                Shader.PropertyToID("_SMAARTMetrics"),
                new Vector4(
                    1.0f / mosaicImageHandle.rt.width,
                    1.0f / mosaicImageHandle.rt.height,
                    mosaicImageHandle.rt.width,
                    mosaicImageHandle.rt.height
                )
            );
            smaaMaterial.SetInt(Shader.PropertyToID("_StencilRef"), (int)(1 << 2));
            smaaMaterial.SetInt(Shader.PropertyToID("_StencilCmp"), (int)(1 << 2));

            switch (hdCamera.SMAAQuality)
            {
                case HDAdditionalCameraData.SMAAQualityLevel.Low:
                    smaaMaterial.EnableKeyword("SMAA_PRESET_LOW");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                    smaaMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                    break;
                case HDAdditionalCameraData.SMAAQualityLevel.High:
                    smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
                default:
                    smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                    break;
            }

            // -----------------------------------------------------------------------------
            // EdgeDetection stage
            ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), mosaicImageHandle);
            CoreUtils.SetRenderTarget(ctx.cmd, smaaEdgesTexHandle, ClearFlag.Color);
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                smaaMaterial,
                ctx.propertyBlock,
                shaderPassId: smaaMaterial.FindPass("EdgeDetection")
            );

            // -----------------------------------------------------------------------------
            // BlendWeights stage
            ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), smaaEdgesTexHandle);
            CoreUtils.SetRenderTarget(ctx.cmd, smaaBlendTexHandle, ClearFlag.Color);
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                smaaMaterial,
                ctx.propertyBlock,
                shaderPassId: smaaMaterial.FindPass("BlendingWeightCalculation")
            );

            // -----------------------------------------------------------------------------
            // NeighborhoodBlending stage
            ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), mosaicImageHandle);
            ctx.propertyBlock.SetTexture(Shader.PropertyToID("_BlendTex"), smaaBlendTexHandle);
            CoreUtils.SetRenderTarget(ctx.cmd, computeShaderResultTextureHandle, ClearFlag.None);
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                smaaMaterial,
                ctx.propertyBlock,
                shaderPassId: smaaMaterial.FindPass("NeighborhoodBlending")
            );
        }

        protected override void Cleanup()
        {
            releaseSMAATextures();
        }
    }
}

#endif
