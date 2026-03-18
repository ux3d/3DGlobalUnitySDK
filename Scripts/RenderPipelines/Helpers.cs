#if G3D_HDRP
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace G3D.RenderPipeline
{
    internal enum AntialiasingMode
    {
        None,
        FXAA,
        SMAA,
        TAA
    }

    internal class Helpers
    {
        /// <summary>
        /// Checks whether the camera is a G3D camera or a Mosaic Multiview camera and if the blit material has been set.
        /// If so returns true, otherwise returns false.
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        internal static bool isMainG3DCamera(Camera camera)
        {
            if (camera.cameraType != CameraType.Game)
                return false;
            bool isG3DCamera = camera.gameObject.TryGetComponent<G3DCamera>(out var g3dCamera);
            bool isG3DCameraEnabled = isG3DCamera && g3dCamera.enabled; // only do something if our component is enabled

            bool isMosaicMultiviewCamera =
                camera.gameObject.TryGetComponent<G3DCameraMosaicMultiview>(out var mosaicCamera);
            bool isMosaicMultiviewCameraEnabled = isMosaicMultiviewCamera && mosaicCamera.enabled; // same check if it is a mosaic camera

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
}
#endif
