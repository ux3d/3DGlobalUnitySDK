#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPViewGenerationPass : FullScreenCustomPass
{
    public RTHandle leftColorMapHandle;
    public RTHandle rightColorMapHandle;

    public List<Camera> cameras;
    public int internalCameraCount = 16;

    public RTHandle mosaicImageHandle;
    public RTHandle depthMosaicHandle;

    private ComputeShader holeFillingCompShader;

    public RenderTexture computeShaderResultTexture;
    public RTHandle computeShaderResultTextureHandle;

    public int holeFillingRadius = 8;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        holeFillingCompShader = Resources.Load<ComputeShader>("G3DViewGenHoleFilling");
    }

    private void setMatrix(CustomPassContext ctx, Matrix4x4 matrix, string name)
    {
        ctx.propertyBlock.SetMatrix(Shader.PropertyToID(name), matrix);
        ctx.cmd.SetComputeMatrixParam(holeFillingCompShader, name, matrix);
    }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (isMainG3DCamera(camera))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);

            // upload all inv view projection matrices
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
            }

            // upload left view projection matrix
            Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(
                cameras[0].projectionMatrix,
                false
            );
            Matrix4x4 viewMatrix = cameras[0].worldToCameraMatrix;
            Matrix4x4 viewProjectionMatrix = projectionMatrix * viewMatrix;
            setMatrix(ctx, viewProjectionMatrix, "leftViewProjMatrix");

            // upload right view projection matrix
            Matrix4x4 rightProjectionMatrix = GL.GetGPUProjectionMatrix(
                cameras[internalCameraCount - 1].projectionMatrix,
                false
            );
            Matrix4x4 rightViewMatrix = cameras[internalCameraCount - 1].worldToCameraMatrix;
            Matrix4x4 rightViewProjectionMatrix = rightProjectionMatrix * rightViewMatrix;
            setMatrix(ctx, rightViewProjectionMatrix, "rightViewProjMatrix");

            // TODO remove this line (used to render the mosaic image to screen)
            // CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                fullscreenPassMaterial,
                ctx.propertyBlock,
                shaderPassId: 0
            );

            // fill holes in the mosaic image
            int kernel = holeFillingCompShader.FindKernel("kernelFunction");
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                kernel,
                "Result",
                computeShaderResultTexture
            );
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                kernel,
                "_depthMosaic",
                depthMosaicHandle
            );
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                kernel,
                "_colorMosaic",
                mosaicImageHandle
            );
            ctx.cmd.SetComputeIntParam(holeFillingCompShader, "grid_size_x", 4);
            ctx.cmd.SetComputeIntParam(holeFillingCompShader, "grid_size_y", 4);
            ctx.cmd.SetComputeIntParam(holeFillingCompShader, "radius", holeFillingRadius);
            ctx.cmd.SetComputeFloatParam(holeFillingCompShader, "sigma", holeFillingRadius / 2.0f);
            ctx.cmd.SetComputeIntParam(
                holeFillingCompShader,
                "imageWidth",
                mosaicImageHandle.rt.width
            );
            ctx.cmd.SetComputeIntParam(
                holeFillingCompShader,
                "imageHeight",
                mosaicImageHandle.rt.height
            );

            ctx.cmd.DispatchCompute(holeFillingCompShader, kernel, 32, 32, 1);

            HDUtils.BlitCameraTexture(
                ctx.cmd,
                computeShaderResultTextureHandle,
                ctx.cameraColorBuffer
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

internal class G3DHDRPViewGenerationMosaicPass : FullScreenCustomPass
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
