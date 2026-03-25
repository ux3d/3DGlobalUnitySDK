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
        private Camera mainCamera;

        private bool windowResizedLastFrame = false;
        private bool mainCamInactiveLastFrame = false;
        private Material material;

        private Vector2Int cachedWindowSize;

        private bool initialized = false;

        // Start is called before the first frame update
        public void init(ref Material material)
        {
            this.material = material;

            mainCamera = GetComponent<Camera>();

            initCustomPass();

            initialized = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!initialized)
            {
                return;
            }
            if (mainCamera.enabled == false)
            {
                mainCamInactiveLastFrame = true;
                return;
            }

            if (mainCamInactiveLastFrame)
            {
                mainCamInactiveLastFrame = false;
            }

            windowResizedLastFrame = windowResized();

            if (windowResizedLastFrame)
            {
                Helpers.GetRTHandleSystem().ResetReferenceSize(Screen.width, Screen.height);
                // TODO this is only needed to reset the size of the cameras internal render targets
                // Find a way to avoid this line...
                RTHandles.ResetReferenceSize(Screen.width, Screen.height);
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

            G3D.RenderPipeline.HDRP.CustomPass customPass =
                customPassVolume.AddPassOfType(typeof(G3D.RenderPipeline.HDRP.CustomPass))
                as G3D.RenderPipeline.HDRP.CustomPass;
            customPass.fullscreenPassMaterial = material;
            customPass.materialPassName = "G3DFullScreen3D";
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
    }
}
#endif
