#if HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPCustomPass : FullScreenCustomPass
{
    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = renderingData.cameraData.camera;
        if (camera.cameraType != CameraType.Game)
            return;
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

        bool isG3DCamera = camera.gameObject.TryGetComponent<G3DCamera>(out var g3dCamera);
        bool isG3DCameraEnabled = isG3DCamera && g3dCamera.enabled;

        bool isMosaicMultiviewCamera = camera.gameObject.TryGetComponent<G3DCameraMosaicMultiview>(
            out var mosaicCamera
        );
        bool isMosaicMultiviewCameraEnabled = isMosaicMultiviewCamera && mosaicCamera.enabled;

        if (!isG3DCameraEnabled && !isMosaicMultiviewCameraEnabled)
            return;

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
