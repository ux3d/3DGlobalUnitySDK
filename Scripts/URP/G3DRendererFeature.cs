#if URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class G3DRendererFeature : ScriptableRendererFeature
{
    private G3DRendererPass pass;

    // Gets called every time serialization happens.
    // Gets called when you enable/disable the renderer feature.
    // Gets called when you change a property in the inspector of the renderer feature.
    public override void Create()
    {
        pass = new G3DRendererPass();
    }

    // Injects one or multiple render passes in the renderer.
    // Gets called when setting up the renderer, once per-camera.
    // Gets called every frame, once per-camera.
    // Will not be called if the renderer feature is disabled in the renderer inspector.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.camera.name.StartsWith(G3DCamera.CAMERA_NAME_PREFIX)) return; //block virtual cameras

        renderer.EnqueuePass(pass);
    }
}

public class G3DRendererPass : ScriptableRenderPass
{
    const string ProfilerTag = "G3D Pass"; //frame debugger

    public G3DRendererPass()
    {
        renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var material = G3DCamera.GetMaterial();

        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, new ProfilingSampler(ProfilerTag)))
        {
            Blit(cmd,
                renderingData.cameraData.renderer.cameraColorTarget,
                renderingData.cameraData.renderer.cameraColorTarget,
                material
            );
        }

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }
}

#endif
