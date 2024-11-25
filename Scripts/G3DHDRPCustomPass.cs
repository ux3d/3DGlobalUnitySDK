#if HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPCustomPass : FullScreenCustomPass
{
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        // only render for game cameras and cameras with the G3D camera component
        if (
            ctx.hdCamera.camera.cameraType != CameraType.Game
            || (
                ctx.hdCamera.camera.gameObject.TryGetComponent<G3DCamera>(out var g3dCamera)
                    == false
                || g3dCamera.enabled == false
            )
        )
        {
            return;
        }

        // skip all cameras created by the G3D Camera script
        if (ctx.hdCamera.camera.name.StartsWith(G3DCamera.CAMERA_NAME_PREFIX))
        {
            return;
        }

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
#endif
