#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace g3d
{
    internal enum AntialiasingMode
    {
        None,
        FXAA,
        SMAA,
        TAA
    }

    internal class HDRPViewGenerationMosaicPass : FullScreenCustomPass
    {
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            if (isMainG3DCamera(camera))
            {
                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                ctx.propertyBlock.SetFloat(Shader.PropertyToID("mosaic_rows"), 4);
                ctx.propertyBlock.SetFloat(Shader.PropertyToID("mosaic_columns"), 4);
                CoreUtils.DrawFullScreen(
                    ctx.cmd,
                    fullscreenPassMaterial,
                    ctx.propertyBlock,
                    shaderPassId: 0
                );
            }
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

            bool isMosaicMultiviewCamera =
                camera.gameObject.TryGetComponent<G3DCameraMosaicMultiview>(out var mosaicCamera);
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
}

#endif
