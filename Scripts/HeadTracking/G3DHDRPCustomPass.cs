using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class G3DHDRPCustomPass : FullScreenCustomPass
{
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        // Render the outline buffer fullscreen
        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
        CoreUtils.DrawFullScreen(
            ctx.cmd,
            fullscreenPassMaterial,
            ctx.propertyBlock,
            shaderPassId: 0
        );
    }

    protected override void Cleanup() { }
}
