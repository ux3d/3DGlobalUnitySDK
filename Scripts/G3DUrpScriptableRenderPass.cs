#if URP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class G3DUrpScriptableRenderPass : ScriptableRenderPass
{
    Material m_Material;

    public G3DUrpScriptableRenderPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;

        // skip all cameras created by the G3D Camera script
        if (cameraData.camera.name.StartsWith("g3dcam_"))
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get();
        Blitter.BlitCameraTexture(
            cmd,
            renderingData.cameraData.renderer.cameraColorTargetHandle,
            renderingData.cameraData.renderer.cameraColorTargetHandle,
            m_Material,
            0
        );
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
    }
}
#endif
