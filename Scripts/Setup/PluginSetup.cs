using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class PluginSetup : MonoBehaviour
{
    public enum RenderingPipelineType
    {
        DEFAULT,
        URP,
        HDRP
    }

    //properties
    [Header("Install / Uninstall the Plugin")]
    public bool                     on = false;

    [Header("Links to modified objects")]
    public GameObject               mainCamera = null;
    public GameObject               ui = null;


    [Header("UI Settings")]
    public bool                     wantUIAsWell = true;
    public bool                     isUiVisibleOnStartup = false;
    public KeyCode                  uiKey = KeyCode.F1;

    [Header("Pipeline (script compilation takes a while - be patient!)")]
    public RenderingPipelineType    renderingPipelineType;

    //cache for old variables
    private bool                     _activate3D = true;
    private GameObject               _mainCamera = null;
    private bool                     _isUiEnabled = true;
    private bool                     _isUiVisibleOnStartup = false;
    private KeyCode                  _uiKey = KeyCode.F1;
    private RenderingPipelineType    _renderingPipelineType;

#if UNITY_EDITOR

    #region Utility

    private bool CheckPreprocessorFlags()
    {
        var preprocessorFlags = PlayerSettings.GetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone).Replace(" ", "");
        bool changed = false;

        //remove all flags that should not be there
        var flags = System.Enum.GetNames(typeof(RenderingPipelineType));
        foreach (string flag in flags)
        {
            if (RenderingPipelineType.DEFAULT.ToString().Equals(flag)) continue;

            if (preprocessorFlags.Contains(flag) && flag != renderingPipelineType.ToString())
            {
                preprocessorFlags = preprocessorFlags.Replace(
                    string.Format("{0}{1}",
                    preprocessorFlags.StartsWith(flag) ? "" : ";", flag),
                    ""
                );
                if (preprocessorFlags.StartsWith(";")) preprocessorFlags = preprocessorFlags.Substring(1);
                changed = true;
            }
        }

        //then add the current one if necessary
        if (renderingPipelineType != RenderingPipelineType.DEFAULT && !preprocessorFlags.Contains(renderingPipelineType.ToString()))
        {
            preprocessorFlags += string.Format("{0}{1}", preprocessorFlags.Length == 0 ? "" : ";", renderingPipelineType);
            changed = true;
        }

        //only write if we did anything with it
        if(changed)
        {
            PlayerSettings.SetScriptingDefineSymbols(UnityEditor.Build.NamedBuildTarget.Standalone, preprocessorFlags);
            return true;
        }

        return false;
    }

    private void AddCameraScript(GameObject cameraHost)
    {
        if (!cameraHost) return;

        cameraHost?.AddComponent<G3DCamera>();
    }
    private void RemoveCameraScript(GameObject cameraHost)
    {
        if (!cameraHost) return;

        var g3dc = cameraHost?.GetComponent<G3DCamera>();
        if (g3dc != null) DestroyImmediate(g3dc);
    }

    private void AddUi()
    {
        if (GameObject.Find("G3DCameraUI") != null) return;
        var go = (GameObject)Instantiate(Resources.Load("G3DCameraUI"));
        go.name = "G3DCameraUI";
        ui = go;
    }
    private void RemoveUi()
    {
        var handle_ui = GameObject.Find("G3DCameraUI");
        if(handle_ui != null) DestroyImmediate(handle_ui);
        ui = null;
    }
    private void SetUiVisibleOnStartup(bool visible)
    {
        var handle_ui = GameObject.Find("G3DCameraUI");
        if(handle_ui != null) handle_ui.transform.Find("Gui").gameObject.SetActive(visible);
    }
    private void SetUiKey(KeyCode keyCode)
    {
        var handle_ui = GameObject.Find("G3DCameraUI");
        if (handle_ui != null) handle_ui.GetComponent<SetActiveOnKeypress>().setActiveKey = keyCode;
    }
    
    
    private void AdjustAlwaysIncludedShaders(List<string> toAdd, List<string> toDelete)
    {
        var graphicsSettingsObj = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var serializedObject = new SerializedObject(graphicsSettingsObj);
        var arrayProp = serializedObject.FindProperty("m_AlwaysIncludedShaders");

        bool changed = false;
        foreach (string shaderName in toDelete)
        {
            if (shaderName == "") continue;
            var shader = Shader.Find(shaderName);
            if (shader == null) continue;

            int shaderIndex;
            for (shaderIndex = 0; shaderIndex < arrayProp.arraySize; shaderIndex++)
            {
                var arrayElem = arrayProp.GetArrayElementAtIndex(shaderIndex);
                if (shader == arrayElem.objectReferenceValue) break;
            }

            if (shaderIndex < arrayProp.arraySize)
            {
                arrayProp.DeleteArrayElementAtIndex(shaderIndex);
                changed = true;
            }
        }

        foreach (string shaderName in toAdd)
        {
            if (shaderName == "") continue;
            var shader = Shader.Find(shaderName);
            if (shader == null) continue;


            bool hasShader = false;
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var arrayElem = arrayProp.GetArrayElementAtIndex(i);
                if (shader == arrayElem.objectReferenceValue)
                {
                    hasShader = true;
                    break;
                }
            }

            if (!hasShader)
            {
                int arrayIndex = arrayProp.arraySize;
                arrayProp.InsertArrayElementAtIndex(arrayIndex);
                var arrayElem = arrayProp.GetArrayElementAtIndex(arrayIndex);
                arrayElem.objectReferenceValue = shader;
                changed = true;
            }
        }

        if(changed)
        {
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }


    private void AddUrpRenderFeature()
    {
#if URP
        var pipeline = ((UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset);
        System.Reflection.FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var p = ((UnityEngine.Rendering.Universal.ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[0];
        if (p == null) return;

        bool hasFeature = false;
        for (int i = 0; i < p.rendererFeatures.Count; i++)
        {
            if (p.rendererFeatures[i] is G3DRendererFeature)
            {
                hasFeature = true;
                break;
            }
        }

        if (!hasFeature)
        {
            var rf = ScriptableObject.CreateInstance<G3DRendererFeature>();
            AssetDatabase.AddObjectToAsset(rf, GetDefaultRenderer());
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rf, out var guid, out long localId);
            rf.name = "G3DRendererFeature";
            p.rendererFeatures.Add(rf);
        }
#endif
    }

#if URP
    private static int GetDefaultRendererIndex(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset)
    {
        return (int)typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);
    }

    private static UnityEngine.Rendering.Universal.ScriptableRendererData GetDefaultRenderer()
    {
        if (UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset)
        {
            UnityEngine.Rendering.Universal.ScriptableRendererData[] rendererDataList = (UnityEngine.Rendering.Universal.ScriptableRendererData[])typeof(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)
                    .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset);
            int defaultRendererIndex = GetDefaultRendererIndex(UnityEngine.Rendering.Universal.UniversalRenderPipeline.asset);

            return rendererDataList[defaultRendererIndex];
        }
        else
        {
            Debug.LogError("No Universal Render Pipeline is currently active.");
            return null;
        }
    }
#endif


    private void RemoveUrpRenderFeature()
    {
#if URP
        var pipeline = ((UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset);
        System.Reflection.FieldInfo propertyInfo = pipeline.GetType().GetField("m_RendererDataList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var p = ((UnityEngine.Rendering.Universal.ScriptableRendererData[])propertyInfo?.GetValue(pipeline))?[0];
        if (p == null) return;

        int featureIndex;
        for (featureIndex = 0; featureIndex < p.rendererFeatures.Count; featureIndex++)
        {
            if (p.rendererFeatures[featureIndex] is G3DRendererFeature)
            {
                break;
            }
        }

        if (featureIndex < p.rendererFeatures.Count) p.rendererFeatures.RemoveAt(featureIndex);
#endif
    }


    private void AddHdrpPostProcessing()
    {
#if HDRP
        //get variables via reflections
        var globalSettings = GraphicsSettings.currentRenderPipeline
            .GetType()
            .GetProperty(
                "globalSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(GraphicsSettings.currentRenderPipeline)
        ;

        var ap = globalSettings
            .GetType()
            .GetField(
                "afterPostProcessCustomPostProcesses",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(globalSettings)
        ;
        List<string> afterPostprocessings = (List<string>)ap;

        var vp = globalSettings
            .GetType()
            .GetProperty(
                "volumeProfile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(globalSettings)
        ;
        VolumeProfile volumeProfile = (VolumeProfile)vp;


        int i;
        //adjust post processing
        string type_s = typeof(G3DPostProcessingHDRP).AssemblyQualifiedName;
        for (i = 0; i < afterPostprocessings.Count; i++)
            if (type_s == afterPostprocessings[i]) break;
        if (i == afterPostprocessings.Count) afterPostprocessings.Add(type_s);

        //adjust volumeProfile
        for (i = 0; i < volumeProfile.components.Count; i++)
            if (volumeProfile.components[i] is G3DPostProcessingHDRP) break;
        if (i == volumeProfile.components.Count) volumeProfile.components.Add(ScriptableObject.CreateInstance<G3DPostProcessingHDRP>());
#endif
    }
    private void RemoveHdrpPostProcessing()
    {
#if HDRP
        //get variables via reflections
        var globalSettings = GraphicsSettings.currentRenderPipeline
            .GetType()
            .GetProperty(
                "globalSettings",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(GraphicsSettings.currentRenderPipeline)
        ;

        var ap = globalSettings
            .GetType()
            .GetField(
                "afterPostProcessCustomPostProcesses",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(globalSettings)
        ;
        List<string> afterPostprocessings = (List<string>)ap;

        var vp = globalSettings
            .GetType()
            .GetProperty(
                "volumeProfile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(globalSettings)
        ;
        VolumeProfile volumeProfile = (VolumeProfile)vp;


        int i;
        //adjust post processing
        string type_s = typeof(G3DPostProcessingHDRP).AssemblyQualifiedName;
        for (i = 0; i < afterPostprocessings.Count; i++)
            if (type_s == afterPostprocessings[i]) break;
        if (i < afterPostprocessings.Count) afterPostprocessings.RemoveAt(i);

        //adjust volumeProfile
        for (i = 0; i < volumeProfile.components.Count; i++)
            if (volumeProfile.components[i] is G3DPostProcessingHDRP) break;
        if (i < volumeProfile.components.Count) volumeProfile.components.RemoveAt(i);
#endif
    }

#endregion

    #region MonoBehaviour

    private void OnEnable()
    {
        
    }

    private bool firstCall = true;

    private void OnValidate()
    {
        //we just want to have this in the editor
        if (Application.isPlaying) return;
        if (!gameObject.activeInHierarchy) return;
        if (!enabled) return;

        if (firstCall)
        {
            //if first use: we dont want the script to do anything without at least one interaction
            //if after leaving editor player: we dont want this script to fire in this case
            //if recompile: we dont end up in this if branch
            _activate3D = on;
            _mainCamera = mainCamera;
            _isUiEnabled = wantUIAsWell;
            _isUiVisibleOnStartup = isUiVisibleOnStartup;
            _uiKey = uiKey;
            _renderingPipelineType = renderingPipelineType;

            firstCall = false;
            return;
        }

        //if render pipeline switches we also switch preprocessor flags, which leads to a recompile (so we need to get rid of these features NOW)
        if (renderingPipelineType != _renderingPipelineType)
        {
            RemoveUrpRenderFeature();
            RemoveHdrpPostProcessing();
            if (CheckPreprocessorFlags()) return;
            _renderingPipelineType = renderingPipelineType;
        }

        //delay any actual action to the next editor update (some functions just wont work in here)
        EditorApplication.update += OnValidateNextUpdate;

    }

    private void OnValidateNextUpdate()
    {
        EditorApplication.update -= OnValidateNextUpdate;

        if (!on)
        {
            RemoveCameraScript(_mainCamera);
            RemoveCameraScript(mainCamera);
            RemoveUi();
            RemoveUrpRenderFeature();
            RemoveHdrpPostProcessing();
            AdjustAlwaysIncludedShaders(new List<string>() { }, new List<string>() { "G3D/AlgoShaderHDRP", "G3D/ViewmapShader", "G3D/ViewmapShaderURP", "G3D/ViewmapShaderHDRP", "G3D/VectorShader", "G3D/VectorShaderURP", "G3D/VectorShaderHDRP", "G3D/ZTrackingShaderHDRP" });
            return;
        }

        //check camera scripts
        if (!mainCamera) mainCamera = GameObject.FindGameObjectWithTag("MainCamera"); 
        if (_mainCamera && mainCamera != _mainCamera && _mainCamera?.GetComponent<G3DCamera>() != null)
            RemoveCameraScript(_mainCamera);

        if (mainCamera?.GetComponent<G3DCamera>() == null)
            AddCameraScript(mainCamera);

        //check ui
        var uiExists = GameObject.Find("G3DCameraUI") != null;
        if (!wantUIAsWell && uiExists) RemoveUi();
        if (wantUIAsWell && !uiExists) AddUi();
        SetUiVisibleOnStartup(isUiVisibleOnStartup);
        SetUiKey(uiKey);

        //shaders & pipeline specific features
        switch (renderingPipelineType)
        {
            case RenderingPipelineType.DEFAULT:
                AdjustAlwaysIncludedShaders(new List<string>() { "G3D/AlgoShader", "G3D/ViewmapShader", "G3D/VectorShader", "G3D/ZTrackingShader" }, new List<string>() { "G3D/AlgoShaderURP", "G3D/ViewmapShaderURP", "G3D/VectorShaderURP", "G3D/ZTrackingShaderURP", "G3D/AlgoShaderHDRP", "G3D/ViewmapShaderHDRP", "G3D/VectorShaderHDRP", "G3D/ZTrackingShaderHDRP" });
                break;
            case RenderingPipelineType.URP:
                AdjustAlwaysIncludedShaders(new List<string>() { "G3D/AlgoShaderURP", "G3D/ViewmapShaderURP", "G3D/VectorShaderURP", "G3D/ZTrackingShaderURP" }, new List<string>() { "G3D/AlgoShader", "G3D/ViewmapShader", "G3D/VectorShader", "G3D/ZTrackingShader", "G3D/AlgoShaderHDRP", "G3D/ViewmapShaderHDRP", "G3D/VectorShaderHDRP", "G3D/ZTrackingShaderHDRP" });
                AddUrpRenderFeature();
                break;
            case RenderingPipelineType.HDRP:
                AdjustAlwaysIncludedShaders(new List<string>() { "G3D/AlgoShaderHDRP", "G3D/ViewmapShaderHDRP", "G3D/VectorShaderHDRP", "G3D/ZTrackingShaderHDRP" }, new List<string>() { "G3D/AlgoShader", "G3D/ViewmapShader", "G3D/VectorShader", "G3D/ZTrackingShader", "G3D/AlgoShaderURP", "G3D/ViewmapShaderURP", "G3D/VectorShaderURP", "G3D/ZTrackingShaderURP" });
                AddHdrpPostProcessing();
                break;
        }

        //add an EventSystem if there is none
        if (EventSystem.current == null) {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    #endregion

#endif

}
