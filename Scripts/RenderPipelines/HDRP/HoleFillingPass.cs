#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class HoleFillingPass : FullScreenCustomPass
    {
        public RTHandle mosaicImageHandle;

        private ComputeShader holeFillingCompShader;
        private int holeFillingKernel;

        public RTHandle computeShaderResultTextureHandle;

        public int holeFillingRadius;

        private Material blitMaterial;

        public RTHandle[] indivDepthMaps;
        public int internalCameraCount = 16;

        public Vector2Int renderResolution = new Vector2Int(1920, 1080);

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            holeFillingCompShader = Resources.Load<ComputeShader>("G3DViewGenHoleFilling");
            holeFillingKernel = holeFillingCompShader.FindKernel("main");

            blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
            blitMaterial.SetTexture(
                Shader.PropertyToID("_mainTex"),
                computeShaderResultTextureHandle
            );
        }

        public void init(Vector2Int resolution, AntialiasingMode mode)
        {
            renderResolution = resolution;
            CreateComputeShaderResultTexture();
        }

        public void updateRenderResolution(Vector2Int resolution)
        {
            if (renderResolution.x == resolution.x && renderResolution.y == resolution.y)
            {
                return;
            }

            renderResolution = resolution;
            CreateComputeShaderResultTexture();
        }

        private void CreateComputeShaderResultTexture()
        {
            // release old texture if it exists
            computeShaderResultTextureHandle?.Release();

            computeShaderResultTextureHandle = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    renderResolution.x,
                    renderResolution.y,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    enableRandomWrite: true
                );
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            HDAdditionalCameraData hdCamera = camera.GetComponent<HDAdditionalCameraData>();
            if (Helpers.isMainG3DCamera(camera))
            {
                runHoleFilling(ctx);
            }
        }

        private void runHoleFilling(CustomPassContext ctx)
        {
            // fill holes in the mosaic image
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                holeFillingKernel,
                "Result",
                computeShaderResultTextureHandle
            );
            for (int i = 0; i < internalCameraCount; i++)
            {
                ctx.cmd.SetComputeTextureParam(
                    holeFillingCompShader,
                    holeFillingKernel,
                    "_depthMap" + i,
                    indivDepthMaps[i]
                );
            }
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                holeFillingKernel,
                "_colorMosaic",
                mosaicImageHandle
            );
            ctx.cmd.SetComputeIntParams(holeFillingCompShader, "gridSize", new int[] { 4, 4 });
            ctx.cmd.SetComputeIntParam(holeFillingCompShader, "radius", holeFillingRadius);
            ctx.cmd.SetComputeFloatParam(holeFillingCompShader, "sigma", holeFillingRadius / 2.0f);
            ctx.cmd.SetComputeIntParams(
                holeFillingCompShader,
                "imageSize",
                new int[] { mosaicImageHandle.rt.width, mosaicImageHandle.rt.height }
            );

            ctx.cmd.DispatchCompute(
                holeFillingCompShader,
                holeFillingKernel,
                mosaicImageHandle.rt.width,
                mosaicImageHandle.rt.height,
                1
            );

            // Blit the result to the mosaic image
            CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
            CoreUtils.DrawFullScreen(ctx.cmd, blitMaterial, ctx.propertyBlock, shaderPassId: 0);
        }

        protected override void Cleanup()
        {
            computeShaderResultTextureHandle?.Release();
        }
    }
}

#endif
