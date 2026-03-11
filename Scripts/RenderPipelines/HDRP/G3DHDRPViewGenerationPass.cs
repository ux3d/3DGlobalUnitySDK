#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal enum AntialiasingMode
{
    None,
    FXAA,
    SMAA
}

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

    private RenderTexture[] transparentObjects;
    //private RenderTexture[] transparentObjectsDepth;
    private RTHandle[] transparentObjectsHandles;
    //private RTHandle[] transparentObjectsDepthHandles;

    public int holeFillingRadius;

    public bool fillHoles;

    public bool debugRendering;

    private Material blitMaterial;
    private Material transparentCompositeMaterial;

    public RTHandle[] indivDepthMaps;

    private ComputeShader fxaaCompShader;
    private int fxaaKernel;

    private Material smaaMaterial;
    private AntialiasingMode antialiasingMode = AntialiasingMode.None;

    public Vector2Int renderResolution = new Vector2Int(1920, 1080);

    public int cullingMask = -1;

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

        transparentCompositeMaterial = new Material(Shader.Find("G3D/G3DTransparentComposite"));
    }

    public void init(Vector2Int resolution, AntialiasingMode mode)
    {
        renderResolution = resolution;
        CreateComputeShaderResultTexture();
        setAntiAliasingMode(mode);
        createTransparentObjectsTextures(internalCameraCount);
    }

    public void updateRenderResolution(Vector2Int resolution)
    {
        if (renderResolution.x == resolution.x && renderResolution.y == resolution.y)
        {
            return;
        }

        renderResolution = resolution;
        CreateComputeShaderResultTexture();

        if (antialiasingMode == AntialiasingMode.SMAA)
        {
            CreateSMAATextures(renderResolution.x, renderResolution.y);
        }
    }

    private void CreateComputeShaderResultTexture()
    {
        // release old texture if it exists
        computeShaderResultTexture?.Release();
        computeShaderResultTextureHandle?.Release();

        computeShaderResultTexture = new RenderTexture(
            renderResolution.x,
            renderResolution.y,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        )
        {
            enableRandomWrite = true
        };
        computeShaderResultTexture.Create();
        computeShaderResultTextureHandle = G3DHDRPCustomPass
            .GetRTHandleSystem()
            .Alloc(computeShaderResultTexture);

        if (blitMaterial != null)
        {
            blitMaterial.SetTexture(Shader.PropertyToID("_mainTex"), computeShaderResultTexture);
        }
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
        )
        {
            name = "SMAAEdgesTex",
            enableRandomWrite = true
        };
        smaaEdgesTex.Create();
        smaaEdgesTexHandle = G3DHDRPCustomPass.GetRTHandleSystem().Alloc(smaaEdgesTex);

        smaaBlendTex = new RenderTexture(
            width,
            height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Linear
        )
        {
            name = "SMAABlendTex",
            enableRandomWrite = true
        };
        smaaBlendTex.Create();
        smaaBlendTexHandle = G3DHDRPCustomPass.GetRTHandleSystem().Alloc(smaaBlendTex);
    }

    public void createTransparentObjectsTextures(int count)
    {
        transparentObjects = new RenderTexture[count];
        transparentObjectsHandles = new RTHandle[count];
        //transparentObjectsDepth = new RenderTexture[count];
        //transparentObjectsDepthHandles = new RTHandle[count];

        for (int i = 0; i < count; i++)
        {
            transparentObjects[i] = new RenderTexture(
                renderResolution.x,
                renderResolution.y,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            )
            {
                name = "TransparentObject" + i,
                enableRandomWrite = true
            };
            transparentObjects[i].Create();
            transparentObjectsHandles[i] = G3DHDRPCustomPass
                .GetRTHandleSystem()
                .Alloc(transparentObjects[i]);

            /*transparentObjectsDepth[i] = new RenderTexture(
                renderResolution.x,
                renderResolution.y,
                24,
                RenderTextureFormat.Depth,
                RenderTextureReadWrite.Linear
            )
            {
                name = "TransparentObjectDepth" + i,
                enableRandomWrite = false
            };
            transparentObjectsDepth[i].Create();
            transparentObjectsDepthHandles[i] = G3DHDRPCustomPass
                .GetRTHandleSystem()
                .Alloc(transparentObjectsDepth[i]);*/
        }
    }

    public void setAntiAliasingMode(AntialiasingMode mode)
    {
        AntialiasingMode oldMode = antialiasingMode;
        if (oldMode == mode)
        {
            return;
        }

        antialiasingMode = mode;
        if (mode == AntialiasingMode.None || mode == AntialiasingMode.FXAA)
        {
            releaseSMAATextures();
        }
        else if (mode == AntialiasingMode.SMAA)
        {
            CreateSMAATextures(renderResolution.x, renderResolution.y);
        }
    }

    private void releaseSMAATextures()
    {
        smaaEdgesTex?.Release();
        smaaBlendTex?.Release();
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
        HDAdditionalCameraData hdCamera = camera.GetComponent<HDAdditionalCameraData>();
        if (isMainG3DCamera(camera))
        {
            runReprojection(ctx);
            // color image now in mosaicImageHandle
            runTransparentPass(ctx);

            runHoleFilling(ctx);
            runFXAA(ctx);
            runSMAA(ctx, hdCamera);

            if (debugRendering)
            {
                if (fillHoles == false && antialiasingMode == AntialiasingMode.None)
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
            else
            {
                if (fillHoles == false && antialiasingMode == AntialiasingMode.None)
                {
                    return;
                }

                blitMaterial.SetTexture(
                    Shader.PropertyToID("_mainTex"),
                    computeShaderResultTexture
                );

                CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
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

    private void runTransparentPass(CustomPassContext ctx)
    {
        if (transparentObjectsHandles == null || indivDepthMaps == null)
            return;

        int tileWidth = mosaicImageHandle.rt.width / 4;
        int tileHeight = mosaicImageHandle.rt.height / 4;

        for (int i = 0; i < internalCameraCount; i++)
        {
            // Copy opaque depth into the transparent depth buffer so transparent objects
            // are depth-tested against the already-reprojected opaque geometry.
            //ctx.cmd.CopyTexture(indivDepthMaps[i], transparentObjectsDepthHandles[i]);

            // Render transparent objects from this camera's perspective.
            // ClearFlag.Color clears the color target while preserving the copied depth.

            Camera bakingCamera = cameras[i];

            // We need to be careful about the aspect ratio of render textures when doing the culling, otherwise it could result in objects poping:
            bakingCamera.aspect = Mathf.Max(
                bakingCamera.aspect,
                transparentObjectsHandles[i].referenceSize.x / (float)transparentObjectsHandles[i].referenceSize.y
            );
            bakingCamera.TryGetCullingParameters(out var cullingParams);
            cullingParams.cullingOptions = CullingOptions.None;
            //camera.cullingMask &= ~(1 << bakingCamera.gameObject.layer);

            // Assign the custom culling result to the context
            // so it'll be used for the following operations
            ctx.cullingResults = ctx.renderContext.Cull(ref cullingParams);

            LayerMask mask = (LayerMask)cullingMask;
            
            CustomPassUtils.RenderFromCamera(
                ctx,
                cameras[i],
                transparentObjects[i],
                ClearFlag.Color,
                mask,
                RenderQueueType.AllTransparent
            );

            // Composite the transparent render into the corresponding tile of the mosaic
            // (column-major layout: camera i occupies column i%4, row i/4).
            int col = i % 4;
            int row = i / 4;

            CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
            ctx.cmd.SetViewport(new Rect(col * tileWidth, row * tileHeight, tileWidth, tileHeight));
            ctx.propertyBlock.SetTexture(
                Shader.PropertyToID("_mainTex"),
                transparentObjects[i]
            );
            CoreUtils.DrawFullScreen(
                ctx.cmd,
                transparentCompositeMaterial,
                ctx.propertyBlock,
                shaderPassId: 0
            );
        }
        CoreUtils.SetRenderTarget(ctx.cmd, mosaicImageHandle, ClearFlag.None);
        // Restore full-mosaic viewport for subsequent passes.
        ctx.cmd.SetViewport(
            new Rect(0, 0, mosaicImageHandle.rt.width, mosaicImageHandle.rt.height)
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
        if (antialiasingMode != AntialiasingMode.FXAA)
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

    private void runSMAA(CustomPassContext ctx, HDAdditionalCameraData hdCamera)
    {
        if (antialiasingMode != AntialiasingMode.SMAA)
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

        switch (hdCamera.SMAAQuality)
        {
            case HDAdditionalCameraData.SMAAQualityLevel.Low:
                smaaMaterial.EnableKeyword("SMAA_PRESET_LOW");
                break;
            case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                smaaMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                break;
            case HDAdditionalCameraData.SMAAQualityLevel.High:
                smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                break;
            default:
                smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                break;
        }

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

    protected override void Cleanup()
    {
        releaseSMAATextures();
        computeShaderResultTextureHandle?.Release();
        computeShaderResultTexture?.Release();
        CoreUtils.Destroy(transparentCompositeMaterial);
    }
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
