#if HDRP
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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

        // for (int i = 0; i < cameraCount; i++)
        // {
        //     Camera camera = cameras[i];
        //     CoreUtils.SetRenderTarget(ctx.cmd, camera.targetTexture, ClearFlag.All);
        //     using (new CustomPassUtils.DisableSinglePassRendering(ctx))
        //     {
        //         using (new CustomPassUtils.OverrideCameraRendering(ctx, camera))
        //         {
        //             CustomPassUtils.DrawRenderers(ctx, ~0, RenderQueueType.AllOpaque);
        //             // CustomPassUtils.RenderFromCamera(
        //             //     ctx: ctx,
        //             //     view: cameras[i],
        //             //     targetRenderTexture: camera.targetTexture,
        //             //     ClearFlag.All,
        //             //     mainCamera.cullingMask
        //             // );
        //         }
        //     }
        // }

        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
        CustomPassUtils.DrawRenderers(ctx, ~0, RenderQueueType.All);
        // CoreUtils.DrawFullScreen(
        //     ctx.cmd,
        //     fullscreenPassMaterial,
        //     ctx.propertyBlock,
        //     shaderPassId: 0
        // );
    }

    protected override void Cleanup() { }
}
#endif
