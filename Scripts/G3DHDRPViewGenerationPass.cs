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

    private ComputeShader holeFillingCompShader;
    private int holeFillingKernel;

    public RenderTexture computeShaderResultTexture;
    public RTHandle computeShaderResultTextureHandle;

    private RenderTexture smaaEdgesTex;
    private RTHandle smaaEdgesTexHandle;
    private RenderTexture smaaBlendTex;
    private RTHandle smaaBlendTexHandle;

    public int holeFillingRadius;

    public bool fillHoles;

    public bool debugRendering;

    private Material blitMaterial;

    public RenderTexture[] indivDepthMaps;

    private ComputeShader fxaaCompShader;
    private int fxaaKernel;
    public bool fxaaEnabled;

    private Material smaaMaterial;
    private bool smaaEnabled;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (fillHoles)
        {
            holeFillingCompShader = Resources.Load<ComputeShader>("G3DViewGenHoleFilling");
            holeFillingKernel = holeFillingCompShader.FindKernel("main");
        }

        fxaaCompShader = Resources.Load<ComputeShader>("G3DFXAA");
        fxaaKernel = fxaaCompShader.FindKernel("FXAA");

        smaaMaterial = new Material(Shader.Find("G3D/SMAA"));
        smaaMaterial.SetTexture("_AreaTex", Resources.Load<Texture2D>("SMAA/AreaTex"));
        // Import search tex as PNG because I can't get Unity to work with an R8 DDS file properly.
        smaaMaterial.SetTexture("_SearchTex", Resources.Load<Texture2D>("SMAA/SearchTex"));

        blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
        blitMaterial.SetTexture(Shader.PropertyToID("_mainTex"), computeShaderResultTexture);
    }

    private void CreateSMAATextures(int width, int height)
    {
        releaseSMAATextures();

        smaaEdgesTex = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );
        smaaEdgesTex.name = "SMAAEdgesTex";
        smaaEdgesTex.enableRandomWrite = true;
        smaaEdgesTex.Create();
        smaaEdgesTexHandle = RTHandles.Alloc(smaaEdgesTex);

        smaaBlendTex = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        );
        smaaBlendTex.name = "SMAABlendTex";
        smaaBlendTex.enableRandomWrite = true;
        smaaBlendTex.Create();
        smaaBlendTexHandle = RTHandles.Alloc(smaaBlendTex);
    }

    public void enableSMAA(int width, int height)
    {
        smaaEnabled = true;
        CreateSMAATextures(width, height);
    }

    public void disableSMAA()
    {
        smaaEnabled = false;
        releaseSMAATextures();
    }

    private void releaseSMAATextures()
    {
        if (smaaEdgesTex)
        {
            smaaEdgesTex?.Release();
        }
        if (smaaBlendTex)
        {
            smaaBlendTex?.Release();
        }
        smaaBlendTexHandle?.Release();
        smaaEdgesTexHandle?.Release();
    }

    private void setMatrix(CustomPassContext ctx, Matrix4x4 matrix, string name)
    {
        ctx.propertyBlock.SetMatrix(Shader.PropertyToID(name), matrix);
        if (fillHoles)
        {
            ctx.cmd.SetComputeMatrixParam(holeFillingCompShader, name, matrix);
        }
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
        if (isMainG3DCamera(camera))
        {
            runReprojection(ctx);
            // color image now in mosaicImageHandle

            runHoleFilling(ctx);
            runFXAA(ctx);
            runSMAA(ctx);

            if (debugRendering)
            {
                if (fxaaEnabled == false && fillHoles == false && smaaEnabled == false)
                {
                    blitMaterial.SetTexture(Shader.PropertyToID("_mainTex"), mosaicImageHandle);
                }
                else
                {
                    blitMaterial.SetTexture(
                        Shader.PropertyToID("_mainTex"),
                        computeShaderResultTexture
                    );
                }

                CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ClearFlag.None);
                CoreUtils.DrawFullScreen(ctx.cmd, blitMaterial, ctx.propertyBlock, shaderPassId: 0);
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

        if (fillHoles)
        {
            ctx.cmd.SetComputeMatrixArrayParam(holeFillingCompShader, "viewMatrices", viewMatrices);
            ctx.cmd.SetComputeMatrixArrayParam(
                holeFillingCompShader,
                "invProjMatrices",
                invProjMatrices
            );
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

    private void runHoleFilling(CustomPassContext ctx)
    {
        if (fillHoles == false)
        {
            return;
        }

        // fill holes in the mosaic image
        ctx.cmd.SetComputeTextureParam(
            holeFillingCompShader,
            holeFillingKernel,
            "Result",
            computeShaderResultTexture
        );
        for (int i = 0; i < internalCameraCount; i++)
        {
            ctx.cmd.SetComputeTextureParam(
                holeFillingCompShader,
                holeFillingKernel,
                "_depthMap" + i,
                indivDepthMaps[i]
            );
        }
        ctx.cmd.SetComputeTextureParam(
            holeFillingCompShader,
            holeFillingKernel,
            "_colorMosaic",
            mosaicImageHandle
        );
        ctx.cmd.SetComputeIntParams(holeFillingCompShader, "gridSize", new int[] { 4, 4 });
        ctx.cmd.SetComputeIntParam(holeFillingCompShader, "radius", holeFillingRadius);
        ctx.cmd.SetComputeFloatParam(holeFillingCompShader, "sigma", holeFillingRadius / 2.0f);
        ctx.cmd.SetComputeIntParams(
            holeFillingCompShader,
            "imageSize",
            new int[] { mosaicImageHandle.rt.width, mosaicImageHandle.rt.height }
        );

        ctx.cmd.DispatchCompute(
            holeFillingCompShader,
            holeFillingKernel,
            mosaicImageHandle.rt.width,
            mosaicImageHandle.rt.height,
            1
        );

        // Blit the result to the mosaic image
        CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
        CoreUtils.DrawFullScreen(ctx.cmd, blitMaterial, ctx.propertyBlock, shaderPassId: 0);
    }

    private void runFXAA(CustomPassContext ctx)
    {
        if (fxaaEnabled == false)
        {
            return;
        }
        Tonemapping tonemapping = ctx.hdCamera.volumeStack.GetComponent<Tonemapping>();
        float paperWhite = tonemapping.paperWhite.value;
        Vector4 hdroutParameters = new Vector4(0, 1000, paperWhite, 1f / paperWhite);

        ctx.cmd.SetComputeTextureParam(
            fxaaCompShader,
            fxaaKernel,
            "_colorMosaic",
            mosaicImageHandle
        );
        ctx.cmd.SetComputeTextureParam(
            fxaaCompShader,
            fxaaKernel,
            "_OutputTexture",
            computeShaderResultTexture
        );
        ctx.cmd.SetComputeVectorParam(fxaaCompShader, "_HDROutputParams", hdroutParameters);
        ctx.cmd.DispatchCompute(
            fxaaCompShader,
            fxaaKernel,
            mosaicImageHandle.rt.width,
            mosaicImageHandle.rt.height,
            1
        );
    }

    private void runSMAA(CustomPassContext ctx)
    {
        if (!smaaEnabled)
        {
            return;
        }

        smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
        smaaMaterial.SetVector(
            Shader.PropertyToID("_SMAARTMetrics"),
            new Vector4(
                1.0f / mosaicImageHandle.rt.width,
                1.0f / mosaicImageHandle.rt.height,
                mosaicImageHandle.rt.width,
                mosaicImageHandle.rt.height
            )
        );
        smaaMaterial.SetInt(Shader.PropertyToID("_StencilRef"), (int)(1 << 2));
        smaaMaterial.SetInt(Shader.PropertyToID("_StencilCmp"), (int)(1 << 2));

        // -----------------------------------------------------------------------------
        // EdgeDetection stage
        ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), mosaicImageHandle);
        CoreUtils.SetRenderTarget(ctx.cmd, smaaEdgesTex, ClearFlag.Color);
        CoreUtils.DrawFullScreen(
            ctx.cmd,
            smaaMaterial,
            ctx.propertyBlock,
            shaderPassId: smaaMaterial.FindPass("EdgeDetection")
        );

        // -----------------------------------------------------------------------------
        // BlendWeights stage
        ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), smaaEdgesTex);
        CoreUtils.SetRenderTarget(ctx.cmd, smaaBlendTex, ClearFlag.Color);
        CoreUtils.DrawFullScreen(
            ctx.cmd,
            smaaMaterial,
            ctx.propertyBlock,
            shaderPassId: smaaMaterial.FindPass("BlendingWeightCalculation")
        );

        // -----------------------------------------------------------------------------
        // NeighborhoodBlending stage
        ctx.propertyBlock.SetTexture(Shader.PropertyToID("_InputTexture"), mosaicImageHandle);
        ctx.propertyBlock.SetTexture(Shader.PropertyToID("_BlendTex"), smaaBlendTex);
        CoreUtils.SetRenderTarget(ctx.cmd, computeShaderResultTextureHandle, ClearFlag.None);
        CoreUtils.DrawFullScreen(
            ctx.cmd,
            smaaMaterial,
            ctx.propertyBlock,
            shaderPassId: smaaMaterial.FindPass("NeighborhoodBlending")
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
