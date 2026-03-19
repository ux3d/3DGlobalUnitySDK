#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class ViewGenerationMosaicPass : FullScreenCustomPass
    {
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            if (Helpers.isMainG3DCamera(camera))
            {
                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                ctx.propertyBlock.SetFloat(Shader.PropertyToID("mosaic_rows"), 4);
                ctx.propertyBlock.SetFloat(Shader.PropertyToID("mosaic_columns"), 4);
                CoreUtils.DrawFullScreen(
                    ctx.cmd,
                    fullscreenPassMaterial,
                    ctx.propertyBlock,
                    shaderPassId: 0
                );
            }
        }

        protected override void Cleanup() { }
    }
}

#endif
