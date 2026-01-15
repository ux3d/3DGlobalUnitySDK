using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEngine.Video;

#endif

#if G3D_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if G3D_URP
using UnityEngine.Rendering.Universal;
#endif

// TODO handle index maps

/// <summary>
/// Replaces the image the camera this script is attached to sees with the rendertexture.
/// The texture should contain a mosaic image with several views.
///
/// IMPORTANT: This script must not be attached to a camera already using a G3D camera script.
/// </summary>
[RequireComponent(typeof(Camera))]
public class G3DCameraMosaicMultiview : MonoBehaviour
{
    #region Calibration
    [Tooltip("Drop the calibration file for the display you want to use here.")]
    public TextAsset calibrationFile;

    [Min(1)]
    public int mosaicRowCount = 3;

    [Min(1)]
    public int mosaicColumnCount = 3;

    [Space(10)]
    [Tooltip(
        "Where the views start to yoyo in the index map. Index map contains the order of views."
    )]
    [Range(0.0f, 1.0f)]
    public float indexMapYoyoStart = 0.0f;

    [Tooltip("Inverts the entire index map. Index map contains the order of views.")]
    public bool invertIndexMap = false;

    [Tooltip("Inverts the indices in the index map. Index map contains the order of views.")]
    public bool invertIndexMapIndices = false;

    [Space(10)]
    #endregion

    /// <summary>
    /// Shifts the individual views to the left or right by the specified number of views.
    /// </summary>
    [Tooltip("Shifts the individual views to the left or right by the specified number of views.")]
    public int viewOffset = 0;
    public RenderTexture mosaicTexture;

    #region 3D Effect settings
    [Header("3D Effect settings")]
    [Tooltip("If set to true, the views will be flipped horizontally.")]
    public bool mirrorViews = false;
    #endregion


    #region Private variables
    private PreviousValues previousValues = new PreviousValues();
    private IndexMap indexMap = IndexMap.Instance;
    private Camera mainCamera;
    private Material material;
#if G3D_HDRP
    private G3DHDRPCustomPass customPass;
#endif
#if G3D_URP
    private G3DUrpScriptableRenderPass customPass;
#endif

    private ShaderHandles shaderHandles;
    private G3DShaderParameters shaderParameters;

    private Vector2Int cachedWindowPosition;
    private Vector2Int cachedWindowSize;

    #endregion

    #region Initialization
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.cullingMask = 0; //disable rendering of the main camera
        mainCamera.clearFlags = CameraClearFlags.Color;

        //initialize cameras

        shaderHandles = new ShaderHandles()
        {
            leftViewportPosition = Shader.PropertyToID("v_pos_x"),
            bottomViewportPosition = Shader.PropertyToID("v_pos_y"),
            screenHeight = Shader.PropertyToID("s_height"),
            nativeViewCount = Shader.PropertyToID("nativeViewCount"),
            angleRatioNumerator = Shader.PropertyToID("zwinkel"),
            angleRatioDenominator = Shader.PropertyToID("nwinkel"),
            leftLensOrientation = Shader.PropertyToID("isleft"),
            BGRPixelLayout = Shader.PropertyToID("isBGR"),
            hqViewCount = Shader.PropertyToID("hqview"),
            mstart = Shader.PropertyToID("mstart"),
        };

        // This has to be done after the cameras are updated
        cachedWindowPosition = new Vector2Int(
            Screen.mainWindowPosition.x,
            Screen.mainWindowPosition.y
        );
        cachedWindowSize = new Vector2Int(Screen.width, Screen.height);

#if G3D_HDRP
        // init fullscreen postprocessing for hd render pipeline
        var customPassVolume = gameObject.AddComponent<CustomPassVolume>();
        customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
        customPassVolume.isGlobal = true;
        // Make the volume invisible in the inspector
        customPassVolume.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;
        customPass = customPassVolume.AddPassOfType(typeof(G3DHDRPCustomPass)) as G3DHDRPCustomPass;
        customPass.fullscreenPassMaterial = material;
        customPass.materialPassName = "G3DFullScreen3D";
#endif

#if G3D_URP
        customPass = new G3DUrpScriptableRenderPass(material);
#endif

        // Do his last to ensure custom passes are already set up
        CalibrationProvider defaultCalibrationProvider = CalibrationProvider.getFromString(
            calibrationFile.text
        );
        shaderParameters = defaultCalibrationProvider.getShaderParameters();
        reinitializeShader();

        previousValues.init();

        indexMap.UpdateIndexMap(
            shaderParameters.nativeViewCount,
            mosaicColumnCount * mosaicRowCount,
            indexMapYoyoStart,
            invertIndexMap,
            invertIndexMapIndices
        );
    }

    public void reinitializeShader()
    {
        material = new Material(Shader.Find("G3D/AutostereoMultiviewMosaic"));
        material.SetTexture("mosaictexture", mosaicTexture, RenderTextureSubElement.Color);

        updateScreenViewportProperties();
        updateShaderParameters();

#if G3D_HDRP
        customPass.fullscreenPassMaterial = material;
#endif
#if G3D_URP
        customPass.updateMaterial(material);
#endif
    }

#if G3D_URP
    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCamera;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
    }

    private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
    {
        // Use the EnqueuePass method to inject a custom render pass
        cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(customPass);
    }
#endif

    #endregion

    #region Updates

    /// <summary>
    /// OnValidate gets called every time the script is changed in the editor.
    /// This is used to react to changes made to the parameters.
    /// </summary>
    void OnValidate()
    {
        if (isActiveAndEnabled == false)
        {
            // do not run this code if the script is not enabled
            return;
        }

        if (calibrationFile != previousValues.calibrationFile)
        {
            previousValues.calibrationFile = calibrationFile;
            updateShaderFromCalibrationFile();
        }

        if (
            previousValues.indexMapYoyoStart != indexMapYoyoStart
            || previousValues.invertIndexMap != invertIndexMap
            || previousValues.invertIndexMapIndices != invertIndexMapIndices
        )
        {
            previousValues.indexMapYoyoStart = indexMapYoyoStart;
            previousValues.invertIndexMap = invertIndexMap;
            previousValues.invertIndexMapIndices = invertIndexMapIndices;

            indexMap.UpdateIndexMap(
                shaderParameters.nativeViewCount,
                mosaicColumnCount * mosaicRowCount,
                indexMapYoyoStart,
                invertIndexMap,
                invertIndexMapIndices
            );
        }
    }

    public void updateShaderFromCalibrationFile(TextAsset calibrationFile)
    {
        if (calibrationFile == null || calibrationFile.text == "")
        {
            return;
        }
        this.calibrationFile = calibrationFile;
        updateShaderFromCalibrationFile();
    }

    public void updateShaderFromCalibrationFile()
    {
        if (calibrationFile == null || calibrationFile.text == "")
        {
            return;
        }

        CalibrationProvider calibrationProvider = CalibrationProvider.getFromString(
            calibrationFile.text
        );
        shaderParameters = calibrationProvider.getShaderParameters();
    }

    void Update()
    {
        updateShaderParameters();

        if (windowResized() || windowMoved())
        {
            updateScreenViewportProperties();
        }
    }

    private void updateScreenViewportProperties()
    {
        try
        {
            shaderParameters.screenHeight = Screen.height;
            shaderParameters.screenWidth = Screen.width;
            shaderParameters.leftViewportPosition = Screen.mainWindowPosition.x;
            shaderParameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height;
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update screen viewport properties: " + e.Message);
        }

        // this parameter is used in the shader to invert the y axis
        material?.SetInt(Shader.PropertyToID("viewportHeight"), Screen.height);
    }

    private void updateShaderParameters()
    {
        material?.SetInt(shaderHandles.leftViewportPosition, shaderParameters.leftViewportPosition);
        material?.SetInt(
            shaderHandles.bottomViewportPosition,
            shaderParameters.bottomViewportPosition
        );
        material?.SetInt(shaderHandles.screenHeight, shaderParameters.screenHeight);
        material?.SetInt(shaderHandles.nativeViewCount, shaderParameters.nativeViewCount);
        material?.SetInt(shaderHandles.angleRatioNumerator, shaderParameters.angleRatioNumerator);
        material?.SetInt(
            shaderHandles.angleRatioDenominator,
            shaderParameters.angleRatioDenominator
        );
        material?.SetInt(shaderHandles.leftLensOrientation, shaderParameters.leftLensOrientation);
        material?.SetInt(shaderHandles.showTestFrame, 0);
        material?.SetInt(shaderHandles.hqViewCount, shaderParameters.hqViewCount);
        material?.SetInt(shaderHandles.BGRPixelLayout, shaderParameters.BGRPixelLayout);
        material?.SetInt(shaderHandles.mstart, shaderParameters.mstart);

        int cameraCount = mosaicColumnCount * mosaicRowCount;
        int shaderMaxCount = shaderParameters.nativeViewCount;
        if (cameraCount > shaderMaxCount)
        {
            cameraCount = shaderMaxCount;
        }

        material?.SetInt(Shader.PropertyToID("cameraCount"), cameraCount);

        material?.SetInt(Shader.PropertyToID("mirror"), mirrorViews ? 1 : 0);

        material?.SetInt(Shader.PropertyToID("mosaic_rows"), mosaicRowCount);
        material?.SetInt(Shader.PropertyToID("mosaic_columns"), mosaicColumnCount);

        material?.SetInt(Shader.PropertyToID("viewOffset"), viewOffset);

        material.SetInt(Shader.PropertyToID("indexMapLength"), indexMap.currentMap.Length);
        material.SetFloatArray(Shader.PropertyToID("index_map"), indexMap.getPaddedIndexMapArray());
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

    private bool windowMoved()
    {
        var window_pos = new Vector2Int(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y);
        if (cachedWindowPosition != window_pos)
        {
            cachedWindowPosition = window_pos;
            return true;
        }
        return false;
    }

    // This function only does something when you use the SRP render pipeline.
    // when using either URP or HRDP image combination is handled in the respective renderpasses.
    // URP -> G3DUrpScriptableRenderPass.cs
    // HDRP -> G3DHDRPCustomPass.cs
    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // This is where the material and shader are applied to the camera image.
        //legacy support (no URP or HDRP)
#if G3D_HDRP || URP
#else
        if (material == null)
            Graphics.Blit(source, destination);
        else
            Graphics.Blit(source, destination, material);
#endif
    }
    #endregion

    /// <summary>
    /// The provided file uri has to be a display calibration ini file.
    /// </summary>
    /// <param name="uri"></param>
    public void UpdateShaderParametersFromURI(string uri)
    {
        if (uri == null || uri == "")
        {
            return;
        }

        try
        {
            CalibrationProvider defaultCalibrationProvider = CalibrationProvider.getFromURI(
                uri,
                (CalibrationProvider provider) =>
                {
                    shaderParameters = provider.getShaderParameters();
                    updateShaderParameters();
                    return 0;
                }
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update shader parameters from uri: " + e.Message);
        }
    }

    /// <summary>
    /// The provided file path has to be a display calibration ini file.
    /// </summary>
    /// <param name="filePath"></param>
    public void UpdateShaderParametersFromFile(string filePath)
    {
        if (filePath == null || filePath == "" || filePath.EndsWith(".ini") == false)
        {
            return;
        }

        try
        {
            CalibrationProvider defaultCalibrationProvider = CalibrationProvider.getFromConfigFile(
                filePath
            );
            shaderParameters = defaultCalibrationProvider.getShaderParameters();
            updateShaderParameters();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update shader parameters from file: " + e.Message);
        }
    }

    /// <summary>
    /// The provided string has to be a display calibration ini file.
    /// </summary>
    /// <param name="json"></param>
    public void UpdateShaderParametersFromINIString(string iniFile)
    {
        if (iniFile == null || iniFile == "")
        {
            return;
        }

        try
        {
            CalibrationProvider defaultCalibrationProvider = CalibrationProvider.getFromString(
                iniFile
            );
            shaderParameters = defaultCalibrationProvider.getShaderParameters();
            updateShaderParameters();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update shader parameters from json: " + e.Message);
        }
    }
}
