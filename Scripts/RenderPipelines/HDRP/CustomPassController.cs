#if G3D_HDRP
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline.HDRP
{
    [HideInInspector]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(G3DCamera))]
    [RequireComponent(typeof(Camera))]
    public class CustomPassController : MonoBehaviour
    {
        private G3DCamera g3dCamera;
        private Camera mainCamera;

        private HDAdditionalCameraData.AntialiasingMode antialiasingMode = HDAdditionalCameraData
            .AntialiasingMode
            .None;

        private ViewGeneration viewGenerationPass;
        private DepthMaps depthMosaicPass;

        /// <summary>
        /// Used for view generation mosaic rendering.
        /// </summary>
        private RTHandle rtHandleMosaic;

        public bool cameraCountChanged = false;
        public bool resolutionScaleChanged = false;
        public bool debugRendering = false;
        public bool generateViews = true;
        private bool windowResizedLastFrame = false;
        private bool mainCamInactiveLastFrame = false;
        private int mainCamCullingMask = -1;
        private int internalCameraCount = 16;
        private int renderResolutionScale = 100;
        private List<Camera> cameras;
        private Material viewGenerationMaterial;
        private Material material;

        private Vector2Int cachedWindowSize;

        // Start is called before the first frame update
        public Material init(
            int internalCameraCount,
            List<Camera> cameras,
            int mainCamCullingMask,
            ref Material material,
            int renderResolutionScale = 100
        )
        {
            this.internalCameraCount = internalCameraCount;
            this.mainCamCullingMask = mainCamCullingMask;
            this.cameras = cameras;
            this.material = material;
            this.renderResolutionScale = renderResolutionScale;

            g3dCamera = GetComponent<G3DCamera>();
            mainCamera = GetComponent<Camera>();

            initCustomPass();
            antialiasingMode = mainCamera.GetComponent<HDAdditionalCameraData>().antialiasing;

            return viewGenerationMaterial;
        }

        // Update is called once per frame
        void Update()
        {
            if (mainCamera.enabled == false)
            {
                mainCamInactiveLastFrame = true;
                return;
            }

            bool recreatedRenderTextures = false;

            if (mainCamInactiveLastFrame)
            {
                mainCamInactiveLastFrame = false;
                // recreate shader render textures when main camera was inactive last frame
                recreatedRenderTextures = true;
            }

            windowResizedLastFrame = windowResized();

            if (windowResizedLastFrame)
            {
                recreatedRenderTextures = true;
                Helpers.GetRTHandleSystem().ResetReferenceSize(Screen.width, Screen.height);
                // TODO this is only needed to reset the size of the cameras internal render targets
                // Find a way to avoid this line...
                RTHandles.ResetReferenceSize(Screen.width, Screen.height);
            }

            if (cameraCountChanged || resolutionScaleChanged)
            {
                recreatedRenderTextures = true;
            }

            if (recreatedRenderTextures)
            {
                recreateRenderTextures();
            }
        }

        private void initCustomPass()
        {
            // init fullscreen postprocessing for hd render pipeline
            CustomPassVolume customPassVolume = gameObject.AddComponent<CustomPassVolume>();
            customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
            customPassVolume.isGlobal = true;
            // Make the volume invisible in the inspector
            customPassVolume.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;

            if (generateViews)
            {
                initViewGeneration(customPassVolume);
            }
            else
            {
                G3D.RenderPipeline.HDRP.CustomPass customPass =
                    customPassVolume.AddPassOfType(typeof(G3D.RenderPipeline.HDRP.CustomPass))
                    as G3D.RenderPipeline.HDRP.CustomPass;
                customPass.fullscreenPassMaterial = material;
                customPass.materialPassName = "G3DFullScreen3D";
            }
        }

        private void initViewGeneration(CustomPassVolume customPassVolume)
        {
            // add depth mosaic generation pass
            depthMosaicPass =
                customPassVolume.AddPassOfType(typeof(G3D.RenderPipeline.HDRP.DepthMaps))
                as G3D.RenderPipeline.HDRP.DepthMaps;
            depthMosaicPass.cullingMask = mainCamCullingMask;
            depthMosaicPass.cameras = cameras;
            depthMosaicPass.internalCameraCount = internalCameraCount;

            // add multiview generation pass
            viewGenerationPass =
                customPassVolume.AddPassOfType(typeof(G3D.RenderPipeline.HDRP.ViewGeneration))
                as G3D.RenderPipeline.HDRP.ViewGeneration;
            viewGenerationMaterial = new Material(Shader.Find("G3D/ViewGeneration"));

            viewGenerationPass.fullscreenPassMaterial = viewGenerationMaterial;
            viewGenerationPass.materialPassName = "G3DViewGeneration";
            viewGenerationPass.cameras = cameras;
            viewGenerationPass.internalCameraCount = internalCameraCount;

            recreateDepthTextures();
            viewGenerationPass.indivDepthMaps = depthMosaicPass.indivDepthTextures;
            viewGenerationPass.debugRendering = debugRendering;

            G3D.RenderPipeline.AntialiasingMode aaMode = getCameraAAMode();
            if (aaMode == G3D.RenderPipeline.AntialiasingMode.SMAA)
            {
                // add SMAA pass
            }
            else if (aaMode == G3D.RenderPipeline.AntialiasingMode.FXAA)
            {
                // add FXAA pass
            }
            else if (aaMode == G3D.RenderPipeline.AntialiasingMode.TAA)
            {
                viewGenerationMaterial.EnableKeyword("TAA");
            }

            // add autostereo mosaic generation pass
            recreateMosaicTexture();

            if (debugRendering == false)
            {
                G3D.RenderPipeline.HDRP.ViewGenerationMosaicPass finalAutostereoGeneration =
                    customPassVolume.AddPassOfType(
                        typeof(G3D.RenderPipeline.HDRP.ViewGenerationMosaicPass)
                    ) as G3D.RenderPipeline.HDRP.ViewGenerationMosaicPass;
                finalAutostereoGeneration.fullscreenPassMaterial = material;
                finalAutostereoGeneration.materialPassName = "G3DFullScreen3D";
            }
        }

        private void recreateDepthTextures()
        {
            depthMosaicPass.recreateDepthTextures(renderResolutionScale);

            for (int i = 0; i < internalCameraCount; i++)
            {
                viewGenerationPass.fullscreenPassMaterial.SetTexture(
                    "_depthMap" + i,
                    depthMosaicPass.indivDepthTextures[i],
                    RenderTextureSubElement.Depth
                );
            }
        }

        private void recreateMosaicTexture()
        {
            Helpers.GetRTHandleSystem().Release(rtHandleMosaic);
            rtHandleMosaic = Helpers
                .GetRTHandleSystem()
                .Alloc(
                    mainCamera.pixelWidth,
                    mainCamera.pixelHeight,
                    colorFormat: UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
                    enableRandomWrite: true
                );

            material.SetTexture("_colorMosaic", rtHandleMosaic);
            viewGenerationPass.mosaicImageHandle = rtHandleMosaic;
        }

        private bool windowResized()
        {
            var window_dim = new Vector2Int(Screen.width, Screen.height);
            if (cachedWindowSize != window_dim)
            {
                cachedWindowSize = window_dim;
                return true;
            }
            return false;
        }

        private void recreateRenderTextures()
        {
            recreateDepthTextures();
            recreateMosaicTexture();
        }

        private G3D.RenderPipeline.AntialiasingMode getCameraAAMode()
        {
            G3D.RenderPipeline.AntialiasingMode aaMode = G3D.RenderPipeline.AntialiasingMode.None;
            if (
                antialiasingMode
                == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing
            )
            {
                aaMode = G3D.RenderPipeline.AntialiasingMode.SMAA;
            }
            else if (
                antialiasingMode
                == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing
            )
            {
                aaMode = G3D.RenderPipeline.AntialiasingMode.FXAA;
            }
            else if (
                antialiasingMode == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing
            )
            {
                aaMode = G3D.RenderPipeline.AntialiasingMode.TAA;
            }
            return aaMode;
        }
    }
}
#endif
