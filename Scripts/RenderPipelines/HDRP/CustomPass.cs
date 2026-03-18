#if G3D_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class CustomPass : FullScreenCustomPass
    {
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            if (Helpers.isMainG3DCamera(camera))
            {
                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
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
