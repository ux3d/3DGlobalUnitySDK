#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class FXAA : FullScreenCustomPass
    {
        public RTHandle mosaicImageHandle;

        public RTHandle computeShaderResultTextureHandle;

        private Material blitMaterial;

        private ComputeShader fxaaCompShader;
        private int fxaaKernel;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            fxaaCompShader = Resources.Load<ComputeShader>("G3DFXAA");
            fxaaKernel = fxaaCompShader.FindKernel("FXAA");

            blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
            blitMaterial.SetTexture(
                Shader.PropertyToID("_mainTex"),
                computeShaderResultTextureHandle
            );
        }

        public void CreateFXAATextures(int width, int height)
        {
            // release old texture if it exists
            computeShaderResultTextureHandle?.Release();

            computeShaderResultTextureHandle = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    width,
                    height,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm,
                    enableRandomWrite: true
                );
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            if (Helpers.isMainG3DCamera(camera))
            {
                runFXAA(ctx);
            }
        }

        private void runFXAA(CustomPassContext ctx)
        {
            Tonemapping tonemapping = ctx.hdCamera.volumeStack.GetComponent<Tonemapping>();
            float paperWhite = tonemapping.paperWhite.value;
            Vector4 hdroutParameters = new Vector4(0, 1000, paperWhite, 1f / paperWhite);

            ctx.cmd.SetComputeTextureParam(
                fxaaCompShader,
                fxaaKernel,
                "_colorMosaic",
                mosaicImageHandle
            );
            ctx.cmd.SetComputeTextureParam(
                fxaaCompShader,
                fxaaKernel,
                "_OutputTexture",
                computeShaderResultTextureHandle
            );
            ctx.cmd.SetComputeVectorParam(fxaaCompShader, "_HDROutputParams", hdroutParameters);
            ctx.cmd.DispatchCompute(
                fxaaCompShader,
                fxaaKernel,
                mosaicImageHandle.rt.width,
                mosaicImageHandle.rt.height,
                1
            );
        }

        protected override void Cleanup()
        {
            computeShaderResultTextureHandle?.Release();
        }
    }
}

#endif
