#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class G3DHDRPDepthMapPrePass : FullScreenCustomPass
{
    public List<Camera> cameras;
    public int internalCameraCount = 16;

    public RTHandle[] indivDepthTextures;

    public bool excludeLayer = false;
    public int layerToExclude = 3;

    public int cullingMask = -1;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

    protected override void Cleanup()
    {
        cleanupDepthTextures();
    }

    protected override void Execute(CustomPassContext ctx)
    {
        var camera = ctx.hdCamera.camera;
        if (isMainG3DCamera(camera) == false)
        {
            return;
        }

        LayerMask depthLayerMask = (LayerMask)cullingMask;

        // render depth maps
        for (int i = 0; i < internalCameraCount; i++)
        {
            Camera bakingCamera = cameras[i];

            // We need to be careful about the aspect ratio of render textures when doing the culling, otherwise it could result in objects poping:
            bakingCamera.aspect = Mathf.Max(
                bakingCamera.aspect,
                indivDepthTextures[i].referenceSize.x / (float)indivDepthTextures[i].referenceSize.y
            );
            bakingCamera.TryGetCullingParameters(out var cullingParams);
            cullingParams.cullingOptions = CullingOptions.None;
            camera.cullingMask &= ~(1 << bakingCamera.gameObject.layer);

            if (excludeLayer)
            {
                int layerMask = 1 << layerToExclude;
                cullingParams.cullingMask = ~(uint)layerMask;
            }

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
                depthLayerMask
            // overrideRenderState: overrideDepthTest
            );
        }
    }

    private void cleanupDepthTextures()
    {
        if (indivDepthTextures == null)
        {
            return;
        }
        for (int i = 0; i < indivDepthTextures.Length; i++)
        {
            if (indivDepthTextures[i] == null)
            {
                continue;
            }
            G3DHDRPCustomPass.GetRTHandleSystem().Release(indivDepthTextures[i]);
        }
    }

    public void recreateDepthTextures(float renderResolutionScale = 1.0f)
    {
        cleanupDepthTextures();

        indivDepthTextures = new RTHandle[internalCameraCount];
        for (int i = 0; i < internalCameraCount; i++)
        {
            float width = Screen.width;
            float height = Screen.height;

            width = width * (renderResolutionScale / 100f);
            height = height * (renderResolutionScale / 100f);

            RenderTexture depthTexture = new RenderTexture(
                (int)width,
                (int)height,
                16,
                RenderTextureFormat.Depth
            );
            depthTexture.Create();
            RTHandle handle = G3DHDRPCustomPass.GetRTHandleSystem().Alloc(depthTexture);
            indivDepthTextures[i] = handle;
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
}


#endif
