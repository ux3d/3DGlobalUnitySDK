#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    internal class ViewGeneration : FullScreenCustomPass
    {
        public RTHandle leftColorMapHandle;
        public RTHandle rightColorMapHandle;

        public List<Camera> cameras;
        public int internalCameraCount = 16;

        public RTHandle mosaicImageHandle;
        public bool debugRendering;

        private Material blitMaterial;

        public RTHandle[] indivDepthMaps;

        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
        }

        private void setMatrix(CustomPassContext ctx, Matrix4x4 matrix, string name)
        {
            ctx.propertyBlock.SetMatrix(Shader.PropertyToID(name), matrix);
        }

        private void addViewProjectionMatrix(CustomPassContext ctx, Camera camera, string name)
        {
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
            Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;
            setMatrix(ctx, viewProjectionMatrix, name);
        }

        protected override void Execute(CustomPassContext ctx)
        {
            var camera = ctx.hdCamera.camera;
            HDAdditionalCameraData hdCamera = camera.GetComponent<HDAdditionalCameraData>();
            if (Helpers.isMainG3DCamera(camera))
            {
                runReprojection(ctx);
                // color image now in mosaicImageHandle

                if (debugRendering)
                {
                    blitMaterial.SetTexture(Shader.PropertyToID("_mainTex"), mosaicImageHandle);

                    CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                    CoreUtils.DrawFullScreen(
                        ctx.cmd,
                        blitMaterial,
                        ctx.propertyBlock,
                        shaderPassId: 0
                    );
                }
            }
        }

        private void runReprojection(CustomPassContext ctx)
        {
            // upload all inv view projection matrices
            Matrix4x4[] viewMatrices = new Matrix4x4[16];
            Matrix4x4[] invProjMatrices = new Matrix4x4[16];

            for (int i = 0; i < internalCameraCount; i++)
            {
                Matrix4x4 projectionMatrixInner = GL.GetGPUProjectionMatrix(
                    cameras[i].projectionMatrix,
                    false
                );
                Matrix4x4 viewMatrixInner = cameras[i].worldToCameraMatrix;

                Matrix4x4 viewProjectionMatrixInner = projectionMatrixInner * viewMatrixInner;
                Matrix4x4 invGPUProjMatrix = viewProjectionMatrixInner.inverse;

                setMatrix(ctx, invGPUProjMatrix, "inverseProjMatrix" + i);
                setMatrix(ctx, cameras[i].worldToCameraMatrix, "viewMatrix" + i);

                viewMatrices[i] = cameras[i].worldToCameraMatrix;
                invProjMatrices[i] = invGPUProjMatrix;
            }

            addViewProjectionMatrix(ctx, cameras[0], "leftViewProjMatrix");
            addViewProjectionMatrix(ctx, cameras[internalCameraCount / 2], "middleViewProjMatrix");
            addViewProjectionMatrix(ctx, cameras[internalCameraCount - 1], "rightViewProjMatrix");

            // always render to mosaic image handle
            CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);

            // reprojection pass
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                fullscreenPassMaterial,
                ctx.propertyBlock,
                shaderPassId: 0
            );
        }
    }
}

#endif
