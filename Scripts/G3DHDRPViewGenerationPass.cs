#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPViewGenerationPass : FullScreenCustomPass
{
    public RTHandle leftDepthMapHandle;
    public RTHandle rightDepthMapHandle;

    public RTHandle leftColorMapHandle;
    public RTHandle rightColorMapHandle;

    public float focusDistance = 1.0f;

    public List<Camera> cameras;
    public int internalCameraCount = 16;

    RenderTexture depthTexturesArray;

    RenderTexture[] indivDepthTextures;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Initialize the depth textures
        // depthTexturesArray = new RenderTexture(1024, 1024, 24, RenderTextureFormat.Depth);
        // depthTexturesArray.dimension = TextureDimension.Tex2DArray;
        // depthTexturesArray.volumeDepth = internalCameraCount;
        // depthTexturesArray.Create();

        // fullscreenPassMaterial.SetTexture(
        //     "_DepthMaps",
        //     depthTexturesArray,
        //     RenderTextureSubElement.Depth
        // );

        indivDepthTextures = new RenderTexture[10];
        for (int i = 0; i < 10; i++)
        {
            RenderTexture depthTexture = new RenderTexture(
                1024,
                1024,
                24,
                RenderTextureFormat.Depth
            );
            depthTexture.dimension = TextureDimension.Tex2D;
            depthTexture.Create();
            indivDepthTextures[i] = depthTexture;
            fullscreenPassMaterial.SetTexture(
                "_depthMap" + i,
                depthTexture,
                RenderTextureSubElement.Depth
            );
        }
    }

    float map(float x, float in_min, float in_max, float out_min, float out_max)
    {
        // Convert the current value to a percentage
        // 0% - min1, 100% - max1
        float perc = (x - in_min) / (in_max - in_min);

        // Do the same operation backwards with min2 and max2
        float value = perc * (out_max - out_min) + out_min;
        return value;
    }

    private float layerOffset(float layer, float farPlane)
    {
        float tmp = map(layer, 0.0f, farPlane, 0, 1.0f); // convert layer distance from [nearPlane, farPlane] to [0,1]
        Vector4 p = new Vector4(0.0f, 0.0f, tmp, layer); // point in view space
        p = cameras[internalCameraCount - 2].projectionMatrix.inverse * p; // convert from clip space to view space
        // p.x = p.x * layer; // convert from clip space to view space
        // p.y = p.y * layer; // convert from clip space to view space
        // p.z = p.z * layer; // convert from clip space to view space
        p = cameras[internalCameraCount - 2].worldToCameraMatrix.inverse * p; // convert from view space to world space

        // p now in world space

        p = cameras[internalCameraCount - 1].worldToCameraMatrix * p; // apply left view matrix to get shifted point in view space
        p = cameras[internalCameraCount - 1].projectionMatrix * p; // apply main camera projection matrix to get clip space coordinates

        p.x = p.x / p.w; // convert from clip space to view space
        p.y = p.y / p.w; // convert from clip space to view space
        p.z = p.z / p.w; // convert from clip space to view space
        float clipSpaceX = -p.x / p.w / 2.0f; // convert to clip space by dividing by w
        // clipSpaceX = clipSpaceX * 0.5f + 0.5f; // convert from [-1,1] to [0,1] to get texture coordinates
        // clipSpaceX = map(clipSpaceX, -1.0f, 1.0f, 0.0f, 1.0f); // convert from [-1,1] to [0,1] to get texture coordinates

        return clipSpaceX;
    }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (isMainG3DCamera(camera))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);

            float viewRange = camera.farClipPlane - camera.nearClipPlane;
            float stepSize = viewRange / 1024.0f;

            // set the inverse projection matrix
            ctx.propertyBlock.SetMatrix(
                Shader.PropertyToID("inverseProjMatrix1"),
                cameras[internalCameraCount - 2].projectionMatrix.inverse
            );
            ctx.propertyBlock.SetMatrix(
                Shader.PropertyToID("inverseViewMatrix1"),
                cameras[internalCameraCount - 2].cameraToWorldMatrix
            );

            for (int i = 0; i < 10; i++)
            {
                Camera bakingCamera = cameras[15 - i];

                // We need to be careful about the aspect ratio of render textures when doing the culling, otherwise it could result in objects poping:
                bakingCamera.aspect = Mathf.Max(
                    bakingCamera.aspect,
                    indivDepthTextures[i].width / (float)indivDepthTextures[i].height
                );
                bakingCamera.TryGetCullingParameters(out var cullingParams);
                cullingParams.cullingOptions = CullingOptions.None;

                // Assign the custom culling result to the context
                // so it'll be used for the following operations
                ctx.cullingResults = ctx.renderContext.Cull(ref cullingParams);
                var overrideDepthTest = new RenderStateBlock(RenderStateMask.Depth)
                {
                    depthState = new DepthState(true, CompareFunction.LessEqual)
                };
                CustomPassUtils.RenderDepthFromCamera(
                    ctx,
                    bakingCamera,
                    indivDepthTextures[i],
                    ClearFlag.Depth,
                    bakingCamera.cullingMask,
                    overrideRenderState: overrideDepthTest
                );
            }

            for (int i = internalCameraCount - 2; i > 0; i--)
            {
                int idx = internalCameraCount - i - 1;
                ctx.propertyBlock.SetMatrix(
                    Shader.PropertyToID("inverseProjMatrix" + idx),
                    cameras[i].projectionMatrix.inverse
                );
                ctx.propertyBlock.SetMatrix(
                    Shader.PropertyToID("inverseViewMatrix" + idx),
                    cameras[i].cameraToWorldMatrix
                );
            }

            CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                fullscreenPassMaterial,
                ctx.propertyBlock,
                shaderPassId: 0
            );
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
        return camera.name == "g3dcam_" + (internalCameraCount - 1).ToString();
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

internal class G3DHDRPViewGenerationMosaicPass : FullScreenCustomPass
{
    public RTHandle mosaicImageHandle;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (shouldPerformBlit(camera))
        {
            CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
            CustomPassUtils.Copy(ctx, ctx.cameraColorBuffer, mosaicImageHandle);

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
    static bool shouldPerformBlit(Camera camera)
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
