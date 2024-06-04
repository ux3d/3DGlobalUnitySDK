#if HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

class G3DHDRPCustomPass : FullScreenCustomPass
{
    public int cameraCount;
    public Camera mainCamera;
    public Camera[] cameras;
    public SortingCriteria sortingCriteria = SortingCriteria.SortingLayer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        if (cameras.Length == 0)
        {
            Debug.LogError(
                "G3D head tracking library: Shader render texture handles are not set up correctly."
            );
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
