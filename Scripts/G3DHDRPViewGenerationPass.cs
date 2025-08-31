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

    public RenderTexture smaaEdgesTex;
    public RenderTexture smaaBlendTex;

    public int holeFillingRadius;

    public bool fillHoles;

    public bool debugRendering;

    private Material blitMaterial;

    public RenderTexture[] indivDepthMaps;

    private ComputeShader fxaaCompShader;
    private int fxaaKernel;
    public bool fxaaEnabled;
    
    private Material smaaMaterial;
    public bool smaaEnabled;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (fillHoles)
        {
            holeFillingCompShader = Resources.Load<ComputeShader>("G3DViewGenHoleFilling");
            holeFillingKernel = holeFillingCompShader.FindKernel("main");
        }

        if (fxaaEnabled)
        {
            fxaaCompShader = Resources.Load<ComputeShader>("G3DFXAA");
            fxaaKernel = fxaaCompShader.FindKernel("FXAA");
        }

        smaaMaterial = new Material(Shader.Find("G3D/SMAA"));
        smaaMaterial.SetTexture("areaTex", Resources.Load<Texture2D>("SMAA/AreaTex"));
        // Import search tex as PNG because I can't get Unity to work with an R8 DDS file properly.
        smaaMaterial.SetTexture("searchTex", Resources.Load<Texture2D>("SMAA/SearchTexPNG"));

        blitMaterial = new Material(Shader.Find("G3D/G3DBlit"));
        blitMaterial.SetTexture(Shader.PropertyToID("_mainTex"), computeShaderResultTexture);
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

            // runFXAA(ctx);

            runSMAA(ctx);

            // ctx.cmd.Blit(smaaEdgesTex, computeShaderResultTexture);
            // ctx.cmd.Blit(smaaBlendTex, computeShaderResultTexture);

            if (debugRendering)
            {
                if (fxaaEnabled == false && fillHoles == false)
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
            ctx.cmd.Blit(mosaicImageHandle, computeShaderResultTexture);
            return;
        }

        smaaMaterial.SetVector(Shader.PropertyToID("SMAA_RT_METRICS"), new Vector4(
            1.0f / mosaicImageHandle.rt.width,
            1.0f / mosaicImageHandle.rt.height,
            mosaicImageHandle.rt.width,
            mosaicImageHandle.rt.height
        ));

        smaaMaterial.SetTexture(Shader.PropertyToID("ColorTex"), mosaicImageHandle);
        CoreUtils.SetRenderTarget(ctx.cmd, smaaEdgesTex, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, smaaMaterial, ctx.propertyBlock, shaderPassId: smaaMaterial.FindPass("EdgeDetection"));

        smaaMaterial.SetTexture(Shader.PropertyToID("edgesTex"), smaaEdgesTex);
        CoreUtils.SetRenderTarget(ctx.cmd, smaaBlendTex, ClearFlag.Color);
        CoreUtils.DrawFullScreen(ctx.cmd, smaaMaterial, ctx.propertyBlock, shaderPassId: smaaMaterial.FindPass("BlendingWeightCalculation"));

        smaaMaterial.SetTexture(Shader.PropertyToID("blendTex"), smaaBlendTex);
        CoreUtils.SetRenderTarget(ctx.cmd, computeShaderResultTexture, ClearFlag.None);
        CoreUtils.DrawFullScreen(ctx.cmd, smaaMaterial, ctx.propertyBlock, shaderPassId: smaaMaterial.FindPass("NeighborhoodBlending"));
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
