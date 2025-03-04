using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
#endif


public class PackageManagerInteractor
{
#if UNITY_EDITOR
    // You must use '[InitializeOnLoadMethod]' or '[InitializeOnLoad]' to subscribe to this event.
    [InitializeOnLoadMethod]
    static void SubscribeToEvent()
    {
        // This causes the method to be invoked after the Editor registers the new list of packages.
        Events.registeredPackages += RegisteredPackagesEventHandler;
    }

    static void RegisteredPackagesEventHandler(
        PackageRegistrationEventArgs packageRegistrationEventArgs
    )
    {
        foreach (var addedPackage in packageRegistrationEventArgs.added)
        {
            if (addedPackage.name == "com.3dglobal.core")
            {
                addScriptingDefineSymbols();
            }
        }
        foreach (var addedPackage in packageRegistrationEventArgs.removed)
        {
            if (addedPackage.name == "com.3dglobal.core")
            {
                removeScriptingDefineSymbols();
            }
        }
    }

    private static void addScriptingDefineSymbols()
    {
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        UnityEditor.Build.NamedBuildTarget namedBuildTarget =
            UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

        List<string> definesList = defines.Split(';').ToList();

        if (GraphicsSettings.defaultRenderPipeline == null)
        {
            return;
        }
        if (GraphicsSettings.defaultRenderPipeline.GetType().Name == "HDRenderPipelineAsset")
        {
            // HDRP rendering pipeline is being used.
            if (!definesList.Contains("G3D_HDRP"))
            {
                definesList.Add("G3D_HDRP");
            }
        }
        else if (
            GraphicsSettings.defaultRenderPipeline.GetType().Name == "UniversalRenderPipelineAsset"
        )
        {
            // URP rendering pipeline is being used.
            if (!definesList.Contains("G3D_URP"))
            {
                definesList.Add("G3D_URP");
            }
        }
        defines = string.Join(";", definesList.ToArray());
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);

        // request a script compilation to apply the changescaused by the new scripting define symbols.
        CompilationPipeline.RequestScriptCompilation();
    }

    private static void removeScriptingDefineSymbols()
    {
        BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
        BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
        UnityEditor.Build.NamedBuildTarget namedBuildTarget =
            UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        string defines = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

        List<string> definesList = defines.Split(';').ToList();
        if (definesList.Contains("G3D_HDRP"))
        {
            definesList.Remove("G3D_HDRP");
        }
        if (definesList.Contains("G3D_URP"))
        {
            definesList.Remove("G3D_URP");
        }
        defines = string.Join(";", definesList.ToArray());
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defines);

        // request a script compilation to apply the changescaused by the new scripting define symbols.
        CompilationPipeline.RequestScriptCompilation();
    }
#endif
}
