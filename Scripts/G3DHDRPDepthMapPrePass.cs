#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPDepthMapPrePass : FullScreenCustomPass
{
    public List<Camera> cameras;
    public int internalCameraCount = 16;

    RenderTexture[] indivDepthTextures;

    public RTHandle depthMosaicHandle;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        indivDepthTextures = new RenderTexture[internalCameraCount];
        for (int i = 0; i < internalCameraCount; i++)
        {
            RenderTexture depthTexture = new RenderTexture(
                cameras[0].pixelWidth,
                cameras[0].pixelHeight,
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

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (isMainG3DCamera(camera) == false)
        {
            return;
        }

        // render depth maps
        for (int i = 0; i < internalCameraCount; i++)
        {
            Camera bakingCamera = cameras[i];

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

        // combine depthmaps into mosaic depth map
        CoreUtils.SetRenderTarget(ctx.cmd, depthMosaicHandle, ClearFlag.None);
        CoreUtils.DrawFullScreen(
            ctx.cmd,
            fullscreenPassMaterial,
            ctx.propertyBlock,
            shaderPassId: 0
        );
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
