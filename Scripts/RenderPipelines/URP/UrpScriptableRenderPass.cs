#if G3D_URP
using Unity.Burst;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

internal class G3DUrpScriptableRenderPass : ScriptableRenderPass
{
    Material m_Material;

    private class PassData
    {
        internal TextureHandle src;
        internal Camera camera;
        internal Material blitMaterial;
    }

    public G3DUrpScriptableRenderPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    [System.Obsolete]
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
    }

    public void updateMaterial(Material material)
    {
        m_Material = material;
    }

    [System.Obsolete]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        var camera = renderingData.cameraData.camera;

        if (performBlit(camera, m_Material))
        {
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

    private void InitPassData(
        RenderGraph renderGraph,
        ContextContainer frameData,
        ref PassData passData
    )
    {
        // Fill up the passData with the data needed by the passes

        // UniversalResourceData contains all the texture handles used by the renderer, including the active color and depth textures
        // The active color and depth textures are the main color and depth buffers that the camera renders into
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

        passData.src = resourceData.activeColorTexture;
        passData.camera = cameraData.camera;
        passData.blitMaterial = m_Material;
    }

    /// <summary>
    /// Checks whether the camera is a G3D camera or a Mosaic Multiview camera and if the blit material has been set.
    /// If so returns true, otherwise returns false.
    /// </summary>
    /// <param name="camera"></param>
    /// <returns></returns>
    static bool performBlit(Camera camera, Material blitMaterial)
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

        if (blitMaterial == null)
            return false;

        // skip all cameras created by the G3D Camera script
        if (camera.name.StartsWith(G3DCamera.CAMERA_NAME_PREFIX))
        {
            return false;
        }

        return true;
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        var camera = data.camera;
        if (performBlit(camera, data.blitMaterial))
        {
            // CommandBuffer cmd = CommandBufferPool.Get();
            // Blitter.BlitCameraTexture(cmd, data.src, data.src, data.blitMaterial, 0);
            // Blitter.
            // context.ExecuteCommandBuffer(cmd);
            // cmd.Clear();
            // CommandBufferPool.Release(cmd);

            Blitter.BlitTexture(
                context.cmd,
                data.src,
                new Vector4(1, 1, 0, 0),
                data.blitMaterial,
                0
            );
        }
    }

    // This is where the renderGraph handle can be accessed.
    // Each ScriptableRenderPass can use the RenderGraph handle to add multiple render passes to the render graph
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        string passName = "Blit With Material";

        // This simple pass copies the active color texture to a new texture using a custom material. This sample is for API demonstrative purposes,
        // so the new texture is not used anywhere else in the frame, you can use the frame debugger to verify its contents.

        // add a raster render pass to the render graph, specifying the name and the data type that will be passed to the ExecutePass function
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
        {
            // Initialize the pass data
            InitPassData(renderGraph, frameData, ref passData);

            // // We disable culling for this pass for the demonstrative purpose of this sampe, as normally this pass would be culled,
            // // since the destination texture is not used anywhere else
            builder.AllowPassCulling(false);

            // Assign the ExecutePass function to the render pass delegate, which will be called by the render graph when executing the pass
            builder.SetRenderFunc(
                (PassData data, RasterGraphContext context) => ExecutePass(data, context)
            );
        }
    }
}
#endif
