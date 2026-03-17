#if G3D_URP
using Unity.Burst;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace G3D.RenderPipeline.URP
{
    internal class ScriptableRP : ScriptableRenderPass
    {
        Material m_Material;

        private class PassData
        {
            internal TextureHandle src;
            internal Camera camera;
            internal Material blitMaterial;
        }

        public ScriptableRP(Material material)
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
        public override void Execute(
            ScriptableRenderContext context,
            ref RenderingData renderingData
        )
        {
            var camera = renderingData.cameraData.camera;

            if (Helpers.isMainG3DCamera(camera, m_Material))
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

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            var camera = data.camera;
            if (Helpers.isMainG3DCamera(camera, data.blitMaterial))
            {
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

            using (
                var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData)
            )
            {
                // Initialize the pass data
                InitPassData(renderGraph, frameData, ref passData);

                // TODO check if these steps are needed

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
}
#endif
