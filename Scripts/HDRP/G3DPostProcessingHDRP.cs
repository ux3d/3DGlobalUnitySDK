#if HDRP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/G3D/G3DCameraFeatureHDRP")]

public sealed class G3DPostProcessingHDRP : CustomPostProcessVolumeComponent, IPostProcessComponent

{
    Material g3dMaterial;

    public bool IsActive()
    {
        return true;
    }

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup() 
    {

    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        g3dMaterial = G3DCamera.GetMaterial();

        if (g3dMaterial == null || camera.name.StartsWith(G3DCamera.CAMERA_NAME_PREFIX))
            HDUtils.BlitCameraTexture(cmd, source, destination); //we dont apply the effect to these
        else
            HDUtils.DrawFullScreen(cmd, g3dMaterial, destination);

    }

    public override void Cleanup()
    {
        
    }

}

#endif
