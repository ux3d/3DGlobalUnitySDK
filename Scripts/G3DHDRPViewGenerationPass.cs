#if G3D_HDRP
using Codice.Client.BaseCommands;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPViewGenerationPass : FullScreenCustomPass
{
    public RTHandle leftDepthMapHandle;
    public RTHandle rightDepthMapHandle;

    public RTHandle leftColorMapHandle;
    public RTHandle rightColorMapHandle;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (isMainG3DCamera(camera))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);

            for (int i = 0; i < 1024; i++)
            {
                float layer = i / 1024.0f;
                ctx.propertyBlock.SetFloat(Shader.PropertyToID("layer"), layer);

                CoreUtils.DrawFullScreen(
                    ctx.cmd,
                    fullscreenPassMaterial,
                    ctx.propertyBlock,
                    shaderPassId: 0
                );
            }

            for (int i = 0; i < 1; i++) { }
        }
        else if (isLeftCamera(camera))
        {
            CustomPassUtils.Copy(ctx, ctx.cameraDepthBuffer, leftDepthMapHandle);
        }
        else if (isRightCamera(camera))
        {
            CustomPassUtils.Copy(ctx, ctx.cameraDepthBuffer, rightDepthMapHandle);
        }
    }

    bool isLeftCamera(Camera camera)
    {
        return camera.name == "g3dcam_1";
    }

    bool isRightCamera(Camera camera)
    {
        return camera.name == "g3dcam_0";
    }

    /// <summary>
    /// Checks whether the camera is a G3D camera or a Mosaic Multiview camera and if the blit material has been set.
    /// If so returns true, otherwise returns false.
    /// </summary>
    /// <param name="camera"></param>
    /// <returns></returns>
    static bool isMainG3DCamera(Camera camera)
    {
        if (camera.cameraType != CameraType.Game)
            return false;
        bool isG3DCamera = camera.gameObject.TryGetComponent<G3DCamera>(out var g3dCamera);
        bool isG3DCameraEnabled = isG3DCamera && g3dCamera.enabled;

        bool isMosaicMultiviewCamera = camera.gameObject.TryGetComponent<G3DCameraMosaicMultiview>(
            out var mosaicCamera
        );
        bool isMosaicMultiviewCameraEnabled = isMosaicMultiviewCamera && mosaicCamera.enabled;

        if (!isG3DCameraEnabled && !isMosaicMultiviewCameraEnabled)
            return false;

        // skip all cameras created by the G3D Camera script
        if (camera.name.StartsWith(G3DCamera.CAMERA_NAME_PREFIX))
        {
            return false;
        }

        return true;
    }

    protected override void Cleanup() { }
}
#endif
