using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR

#endif

#if G3D_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if G3D_URP
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#endif

public struct HeadPosition
{
    public bool headDetected;
    public bool imagePosIsValid;
    public int imagePosX;
    public int imagePosY;
    public double worldPosX;
    public double worldPosY;
    public double worldPosZ;
}

public enum G3DCameraMode
{
    DIORAMA,
    MULTIVIEW
}

/// <summary>
/// This struct is used to store the shader parameter handles for the individual shader parameters.
/// Its members should always be updated when the G3DShaderParameters struct changes.
/// </summary>
struct ShaderHandles
{
    // Viewport properties
    public int leftViewportPosition; //< The left   position of the viewport in screen coordinates
    public int bottomViewportPosition; //< The bottom position of the viewport in screen coordinates

    // Monitor properties
    public int screenWidth; //< The screen width in pixels
    public int screenHeight; //< The screen height in pixels

    public int nativeViewCount;
    public int angleRatioNumerator;
    public int angleRatioDenominator;
    public int leftLensOrientation;
    public int BGRPixelLayout;

    public int mstart;
    public int showTestFrame;
    public int showTestStripe;
    public int testGapWidth;
    public int track;
    public int hqViewCount;
    public int hviews1;
    public int hviews2;
    public int blur;
    public int blackBorder;
    public int blackSpace;
    public int bls;
    public int ble;
    public int brs;
    public int bre;

    public int zCorrectionValue;
    public int zCompensationValue;
}

/// <summary>
/// IMPORTANT: This script must not be attached to a camera already using a G3D camera script.
/// </summary>
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class G3DCamera
    : MonoBehaviour,
        ITNewHeadPositionCallback,
        ITNewShaderParametersCallback,
        ITNewErrorMessageCallback
{
    [Tooltip("Drop the calibration file for the display you want to use here.")]
    public TextAsset calibrationFile;

    [Tooltip(
        "This path has to be set to the directory where the folder containing the calibration files for your monitor are located. The folder has to have the same name as your camera model."
    )]
    public string calibrationPath = "";

    #region 3D Effect settings
    public G3DCameraMode mode = G3DCameraMode.DIORAMA;
    public static string CAMERA_NAME_PREFIX = "g3dcam_";

    [Tooltip(
        "This value can be used to scale the real world values used to calibrate the extension. For example if your scene is 10 times larger than the real world, you can set this value to 10. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!"
    )]
    [Min(0.000001f)]
    public float sceneScaleFactor = 1.0f;

    [Tooltip("If set to true, the views will be flipped horizontally.")]
    public bool mirrorViews = false;

    [Tooltip(
        "Set a percentage value to render only that percentage of the width and height per view. E.g. a reduction of 50% will reduce the rendered size by a factor of 4. Adapt Render Resolution To Views takes precedence."
    )]
    [Range(1, 100)]
    public int renderResolutionScale = 100;

    [Tooltip(
        "Set the dolly zoom effekt. 1 correponds to no dolly zoom. 0 is all the way zoomed in to the focus plane. 3 is all the way zoomed out."
    )]
    [Range(0.001f, 3)]
    public float dollyZoom = 1;

    [Tooltip(
        "Scale the view offset up or down. 1.0f is no scaling, 0.5f is half the distance, 2.0f is double the distance. This can be used to adjust the view offset down for very large scenes."
    )]
    [Range(0.0f, 5.0f)]
    public float viewOffsetScale = 1.0f; // scale the view offset to the focus distance. 1.0f is no scaling, 0.5f is half the distance, 2.0f is double the distance.

    [Tooltip(
        "Scales the strength of the head tracking effect. 1.0f is no scaling, 0.5f is half the distance, 2.0f is double the distance."
    )]
    [Min(0.0f)]
    public float headTrackingScale = 1.0f; // scale the head tracking effect
    #endregion

    #region Advanced settings
    [Tooltip(
        "If set to true, the render targets for the individual views will be adapted to the resolution actually visible on screen. e.g. for two views each render target will have half the screen width. Overwrites Render Resolution Scale."
    )]
    private bool adaptRenderResolutionToViews = false;

    [Tooltip(
        "Smoothes the head position (Size of the filter kernel). Not filtering is applied, if set to all zeros. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!"
    )]
    private Vector3Int headPositionFilter = new Vector3Int(5, 5, 5);
    private LatencyCorrectionMode latencyCorrectionMode = LatencyCorrectionMode.LCM_SIMPLE;

    [Tooltip("If set to true, the library will print debug messages to the console.")]
    public bool debugMessages = false;
    public bool showTestFrame = false;

    [Tooltip(
        "If set to true, the gizmos for the focus distance (green) and eye separation (blue) will be shown."
    )]
    public bool showGizmos = true;

    [Tooltip("Scales the gizmos. Affectd by scene scale factor.")]
    [Range(0.005f, 1.0f)]
    public float gizmoSize = 0.2f;

    #endregion

    #region Private variables

    private LibInterface libInterface;

    // distance between the two cameras for diorama mode (in meters). DO NOT USE FOR MULTIVIEW MODE!
    private float viewSeparation = 0.065f;

    /// <summary>
    /// The distance between the camera and the focus plane in meters. Default is 70 cm.
    /// Is read from calibration file at startup
    /// </summary>
    private float focusDistance = 0.7f;

    private const int MAX_CAMERAS = 19; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ...
    private int internalCameraCount = 2;
    private int oldRenderResolutionScale = 100;

    /// <summary>
    /// This struct is used to store the current head position.
    /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
    /// NEVER use headPosition directly.
    /// </summary>
    private HeadPosition headPosition;
    private HeadPosition filteredHeadPosition;

    private static object headPosLock = new object();
    private static object shaderLock = new object();

    private Camera mainCamera;
    private List<Camera> cameras = null;
    private GameObject focusPlaneObject = null;
    private GameObject cameraParent = null;

    private Material material;
#if G3D_HDRP
    private HDAdditionalCameraData.AntialiasingMode antialiasingMode = HDAdditionalCameraData
        .AntialiasingMode
        .None;
#endif
#if G3D_URP
    private G3DUrpScriptableRenderPass customPass;
    private AntialiasingMode antialiasingMode = AntialiasingMode.None;
#endif

    private ShaderHandles shaderHandles;
    private G3DShaderParameters shaderParameters;

    private Vector2Int cachedWindowPosition;
    private Vector2Int cachedWindowSize;

    // half of the width of field of view at start at focus distance
    private float halfCameraWidthAtStart = 1.0f;

    private Queue<string> headPositionLog;

    /// <summary>
    /// This calue is calculated based on the calibration file
    /// </summary>
    private float baseFieldOfView = 16.0f;

    /// <summary>
    /// Focus distance scaled by scene scale factor.
    /// </summary>
    public float scaledFocusDistance
    {
        get { return focusDistance * sceneScaleFactor; }
    }

    public float scaledFocusDistanceAndDolly
    {
        get
        {
            float dollyZoomFactor = scaledFocusDistance - scaledFocusDistance * dollyZoom;
            float focusDistanceWithDollyZoom = scaledFocusDistance - dollyZoomFactor;
            return focusDistanceWithDollyZoom;
        }
    }

    private float scaledViewSeparation
    {
        get { return viewSeparation * sceneScaleFactor * viewOffsetScale; }
    }

    private float scaledHalfCameraWidthAtStart
    {
        get { return halfCameraWidthAtStart * sceneScaleFactor; }
    }

    #endregion

    private bool generateViews = true;
    private bool useVectorMapViewGeneration = true;
    private Material viewGenerationMaterial;

    // TODO Handle viewport resizing/ moving

    #region Initialization
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        oldRenderResolutionScale = renderResolutionScale;
        setupCameras();

        // create a focus plane object at focus distance from camera.
        // then parent the camera parent to that object
        // this way we can place the camera parent relative to the focus plane

        // disable rendering on main camera after other cameras have been created and settings have been copied over
        // otherwise the secondary cameras are initialized wrong
        mainCamera.cullingMask = 0; //disable rendering of the main camera
        mainCamera.clearFlags = CameraClearFlags.Color;

        reinitializeShader();
#if G3D_HDRP
        initCustomPass();

        antialiasingMode = mainCamera.GetComponent<HDAdditionalCameraData>().antialiasing;
#endif

#if G3D_URP
        customPass = new G3DUrpScriptableRenderPass(material);
        antialiasingMode = mainCamera.GetUniversalAdditionalCameraData().antialiasing;
        mainCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
#endif

        shaderHandles = new ShaderHandles()
        {
            leftViewportPosition = Shader.PropertyToID("viewport_pos_x"),
            bottomViewportPosition = Shader.PropertyToID("viewport_pos_y"),
            screenWidth = Shader.PropertyToID("screen_width"),
            screenHeight = Shader.PropertyToID("screen_height"),
            nativeViewCount = Shader.PropertyToID("nativeViewCount"),
            angleRatioNumerator = Shader.PropertyToID("zwinkel"),
            angleRatioDenominator = Shader.PropertyToID("nwinkel"),
            leftLensOrientation = Shader.PropertyToID("isleft"),
            BGRPixelLayout = Shader.PropertyToID("isBGR"),
            mstart = Shader.PropertyToID("mstart"),
            showTestFrame = Shader.PropertyToID("test"),
            showTestStripe = Shader.PropertyToID("stest"),
            testGapWidth = Shader.PropertyToID("testgap"),
            track = Shader.PropertyToID("track"),
            hqViewCount = Shader.PropertyToID("hqview"),
            hviews1 = Shader.PropertyToID("hviews1"),
            hviews2 = Shader.PropertyToID("hviews2"),
            blur = Shader.PropertyToID("blur"),
            blackBorder = Shader.PropertyToID("bborder"),
            blackSpace = Shader.PropertyToID("bspace"),
            bls = Shader.PropertyToID("bls"),
            ble = Shader.PropertyToID("ble"),
            brs = Shader.PropertyToID("brs"),
            bre = Shader.PropertyToID("bre"),
            zCorrectionValue = Shader.PropertyToID("tvx"),
            zCompensationValue = Shader.PropertyToID("zkom"),
        };

        if (mode == G3DCameraMode.DIORAMA)
        {
            initLibrary();
        }
        updateScreenViewportProperties();

        loadShaderParametersFromCalibrationFile();
        updateShaderParameters();

        if (mode == G3DCameraMode.DIORAMA)
        {
            try
            {
                libInterface.startHeadTracking();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to start head tracking: " + e.Message);
            }
        }

        updateCameras();
        updateShaderRenderTextures();

        // This has to be done after the cameras are updated
        cachedWindowPosition = new Vector2Int(
            Screen.mainWindowPosition.x,
            Screen.mainWindowPosition.y
        );
        cachedWindowSize = new Vector2Int(Screen.width, Screen.height);

        headPositionLog = new Queue<string>(10000);

        if (useVectorMapViewGeneration)
        {
            int invert = shaderParameters.leftLensOrientation == 1 ? 1 : -1;
            Texture2D viewMap = ViewmapGeneratorInterface.getViewMap(
                (uint)shaderParameters.screenWidth, // PixelCountX
                (uint)shaderParameters.screenHeight, // PixelCountY
                (uint)shaderParameters.nativeViewCount, // ViewCount
                (uint)shaderParameters.angleRatioDenominator, // LensWidth
                invert * shaderParameters.angleRatioNumerator, // LensAngleCounter
                false, // ViewOrderInverted
                false, // Rotated
                false, // FullPixel
                shaderParameters.BGRPixelLayout != 0 // BGRMode
            );
            viewMap.Apply();
            material?.SetTexture("_viewMap", viewMap);

            float[] indexMap = new float[shaderParameters.nativeViewCount];
            for (int i = 0; i < shaderParameters.nativeViewCount; i++)
            {
                indexMap[i] = i;
            }
            material?.SetFloatArray("indexMap", indexMap);
        }
    }

#if G3D_HDRP
    private void initCustomPass()
    {
        bool debugRendering = false;
        bool isFillingHoles = false;
        int holeFillingRadius = 3;

        // init fullscreen postprocessing for hd render pipeline
        CustomPassVolume customPassVolume = gameObject.AddComponent<CustomPassVolume>();
        customPassVolume.injectionPoint = CustomPassInjectionPoint.AfterPostProcess;
        customPassVolume.isGlobal = true;
        // Make the volume invisible in the inspector
        customPassVolume.hideFlags = HideFlags.HideInInspector | HideFlags.DontSave;

        if (generateViews)
        {
            // add depth mosaic generation pass
            G3DHDRPDepthMapPrePass depthMosaicPass =
                customPassVolume.AddPassOfType(typeof(G3DHDRPDepthMapPrePass))
                as G3DHDRPDepthMapPrePass;
            depthMosaicPass.cameras = cameras;
            depthMosaicPass.internalCameraCount = internalCameraCount;

            depthMosaicPass.indivDepthTextures = new RenderTexture[internalCameraCount];
            for (int i = 0; i < internalCameraCount; i++)
            {
                RenderTexture depthTexture = new RenderTexture(
                    cameras[0].pixelWidth,
                    cameras[0].pixelHeight,
                    0,
                    RenderTextureFormat.Depth
                );
                depthTexture.Create();
                depthMosaicPass.indivDepthTextures[i] = depthTexture;
            }

            // add multiview generation pass
            G3DHDRPViewGenerationPass viewGenerationPass =
                customPassVolume.AddPassOfType(typeof(G3DHDRPViewGenerationPass))
                as G3DHDRPViewGenerationPass;
            viewGenerationMaterial = new Material(Shader.Find("G3D/ViewGeneration"));
            viewGenerationMaterial.SetInt(Shader.PropertyToID("grid_size_x"), 4);
            viewGenerationMaterial.SetInt(Shader.PropertyToID("grid_size_y"), 4);

            viewGenerationPass.fullscreenPassMaterial = viewGenerationMaterial;
            viewGenerationPass.materialPassName = "G3DViewGeneration";
            viewGenerationPass.cameras = cameras;
            viewGenerationPass.internalCameraCount = internalCameraCount;
            viewGenerationPass.computeShaderResultTexture = new RenderTexture(
                mainCamera.pixelWidth,
                mainCamera.pixelHeight,
                0
            );
            viewGenerationPass.computeShaderResultTexture.enableRandomWrite = true;
            viewGenerationPass.computeShaderResultTexture.Create();
            viewGenerationPass.computeShaderResultTextureHandle = RTHandles.Alloc(
                viewGenerationPass.computeShaderResultTexture
            );
            for (int i = 0; i < internalCameraCount; i++)
            {
                viewGenerationPass.fullscreenPassMaterial.SetTexture(
                    "_depthMap" + i,
                    depthMosaicPass.indivDepthTextures[i],
                    RenderTextureSubElement.Depth
                );
            }
            viewGenerationPass.indivDepthMaps = depthMosaicPass.indivDepthTextures;
            viewGenerationPass.debugRendering = debugRendering;
            viewGenerationPass.fillHoles = isFillingHoles;
            viewGenerationPass.holeFillingRadius = holeFillingRadius;

            // add autostereo mosaic generation pass
            RenderTexture mosaicTexture = new RenderTexture(
                mainCamera.pixelWidth,
                mainCamera.pixelHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB
            );
            mosaicTexture.enableRandomWrite = true;

            if (debugRendering == false)
            {
                G3DHDRPViewGenerationMosaicPass finalAutostereoGeneration =
                    customPassVolume.AddPassOfType(typeof(G3DHDRPViewGenerationMosaicPass))
                    as G3DHDRPViewGenerationMosaicPass;
                finalAutostereoGeneration.fullscreenPassMaterial = material;
                finalAutostereoGeneration.materialPassName = "G3DFullScreen3D";
            }

            RTHandle rtHandleMosaic = RTHandles.Alloc(mosaicTexture);
            material.SetTexture("_colorMosaic", rtHandleMosaic);
            viewGenerationPass.mosaicImageHandle = rtHandleMosaic;
        }
        else
        {
            G3DHDRPCustomPass customPass =
                customPassVolume.AddPassOfType(typeof(G3DHDRPCustomPass)) as G3DHDRPCustomPass;
            customPass.fullscreenPassMaterial = material;
            customPass.materialPassName = "G3DFullScreen3D";
        }
    }
#endif

    void OnApplicationQuit()
    {
        deinitLibrary();
    }

    /// <summary>
    /// this variable is onle here to track changes made to the public calibration file from the editor.
    /// </summary>
    private TextAsset previousCalibrationFile = null;
    private G3DCameraMode previousMode = G3DCameraMode.DIORAMA;
    private float previousSceneScaleFactor = 1.0f;

    /// <summary>
    /// OnValidate gets called every time the script is changed in the editor.
    /// This is used to react to changes made to the parameters.
    /// </summary>
    void OnValidate()
    {
        if (enabled == false)
        {
            // do not run this code if the script is not enabled
            return;
        }

        if (
            calibrationFile != previousCalibrationFile
            || previousSceneScaleFactor != sceneScaleFactor
        )
        {
            previousCalibrationFile = calibrationFile;
            setupCameras();
        }

        if (previousMode != mode)
        {
            previousMode = mode;
            if (mode == G3DCameraMode.MULTIVIEW)
            {
                CalibrationProvider calibration = CalibrationProvider.getFromString(
                    calibrationFile.text
                );
                // internalCameraCount = getCameraCountFromCalibrationFile(calibration);
                // TODO DO NOT HARD CODE THIS VALUE!
                internalCameraCount = 16;
                loadMultiviewViewSeparationFromCalibration(calibration);
            }
            else
            {
                internalCameraCount = 2;
                viewSeparation = 0.065f;
            }
        }
    }

    public void loadShaderParametersFromCalibrationFile()
    {
        if (calibrationFile == null)
        {
            Debug.LogError(
                "No calibration file set. Please set a calibration file. Using default values."
            );
            return;
        }

        lock (shaderLock)
        {
            CalibrationProvider calibrationProvider = CalibrationProvider.getFromString(
                calibrationFile.text
            );
            shaderParameters = calibrationProvider.getShaderParameters();
        }
    }

    private void loadMultiviewViewSeparationFromCalibration(CalibrationProvider calibration)
    {
        if (mode != G3DCameraMode.MULTIVIEW)
        {
            return;
        }

        int BasicWorkingDistanceMM = calibration.getInt("BasicWorkingDistanceMM");
        int NativeViewcount = calibration.getInt("NativeViewcount");
        float ApertureAngle = 14.0f;
        try
        {
            ApertureAngle = calibration.getFloat("ApertureAngle");
        }
        catch (Exception e)
        {
            Debug.LogWarning(e.Message);
        }

        float BasicWorkingDistanceMeter = BasicWorkingDistanceMM / 1000.0f;
        float halfZoneOpeningAngleRad = ApertureAngle * Mathf.Deg2Rad / 2.0f;
        float halfWidthZoneAtbasicDistance =
            Mathf.Tan(halfZoneOpeningAngleRad) * BasicWorkingDistanceMeter;

        // calculate eye separation/ view separation
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            viewSeparation = halfWidthZoneAtbasicDistance * 2 / NativeViewcount;
        }
    }

    /// <summary>
    /// Updates all camera parameters based on the calibration file (i.e. focus distance, fov, etc.).
    /// Includes shader parameters (i.e. lense shear angle, camera count, etc.).
    ///
    /// Updates calibration file as well.
    /// </summary>
    public void setupCameras(TextAsset calibrationFile)
    {
        this.calibrationFile = calibrationFile;
        setupCameras();
    }

    /// <summary>
    /// Sets up all camera parameters based on the calibration file (i.e. focus distance, fov, etc.).
    /// Includes shader parameters (i.e. lense shear angle, camera count, etc.).
    ///
    /// Does not update calibration file.
    /// </summary>
    public void setupCameras()
    {
        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
        }
        if (Application.isPlaying)
        {
            // only run this code if not in editor mode (this function (setupCameras()) is called from OnValidate as well -> from editor ui)
            initCamerasAndParents();
        }
        if (calibrationFile == null)
        {
            Debug.LogError(
                "No calibration file set. Please set a calibration file. Using default values."
            );
            mainCamera.fieldOfView = 16.0f;
            focusDistance = 0.7f;
            viewSeparation = 0.065f;
            if (mode == G3DCameraMode.MULTIVIEW)
            {
                viewSeparation = 0.031f;
            }
            focusPlaneObject.transform.localPosition = new Vector3(0, 0, scaledFocusDistance);
            return;
        }

        // load values from calibration file
        CalibrationProvider calibration = CalibrationProvider.getFromString(calibrationFile.text);
        int BasicWorkingDistanceMM = calibration.getInt("BasicWorkingDistanceMM");
        float PhysicalSizeInch = calibration.getFloat("PhysicalSizeInch");
        int NativeViewcount = calibration.getInt("NativeViewcount");
        int HorizontalResolution = calibration.getInt("HorizontalResolution");
        int VerticalResolution = calibration.getInt("VerticalResolution");

        // calculate intermediate values
        float BasicWorkingDistanceMeter = BasicWorkingDistanceMM / 1000.0f;
        float physicalSizeInMeter = PhysicalSizeInch * 0.0254f;
        float aspectRatio = (float)HorizontalResolution / (float)VerticalResolution;
        float FOV =
            2 * Mathf.Atan(physicalSizeInMeter / 2.0f / BasicWorkingDistanceMeter) * Mathf.Rad2Deg;

        // set focus distance
        focusDistance = (float)BasicWorkingDistanceMeter;

        // set camera fov
        baseFieldOfView = Camera.HorizontalToVerticalFieldOfView(FOV, aspectRatio);
        mainCamera.fieldOfView = baseFieldOfView;
        // calculate eye separation/ view separation
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            loadMultiviewViewSeparationFromCalibration(calibration);
            // TODO DO NOT HARD CODE THIS VALUE!
            internalCameraCount = 16; // default value for multiview mode
            // internalCameraCount = NativeViewcount;
        }
        else
        {
            viewSeparation = 0.065f;
            internalCameraCount = 2;
        }

        // only run this code if not in editor mode (this function (setupCameras()) is called from OnValidate as well -> from editor ui)
        if (Application.isPlaying)
        {
            // update focus plane distance
            focusPlaneObject.transform.localPosition = new Vector3(0, 0, scaledFocusDistance);
            cameraParent.transform.localPosition = new Vector3(0, 0, -scaledFocusDistance);
        }

        halfCameraWidthAtStart =
            Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad / 2) * focusDistance;

        loadShaderParametersFromCalibrationFile();
    }

    /// <summary>
    /// Initializes the cameras and their parents.
    /// if cameras are already initialized, this function only returns the focus plane.
    /// </summary>
    /// <returns>
    /// The focus plane game object.
    /// </returns>
    private void initCamerasAndParents()
    {
        if (focusPlaneObject == null)
        {
            focusPlaneObject = new GameObject("focus plane center");
            focusPlaneObject.transform.parent = transform;
            focusPlaneObject.transform.localPosition = new Vector3(0, 0, scaledFocusDistance);
            focusPlaneObject.transform.localRotation = Quaternion.identity;
        }

        //initialize cameras
        if (cameraParent == null)
        {
            cameraParent = new GameObject("g3dcams");
            cameraParent.transform.parent = focusPlaneObject.transform;
            cameraParent.transform.localPosition = new Vector3(0, 0, -scaledFocusDistance);
            cameraParent.transform.localRotation = Quaternion.identity;
        }

        if (cameras == null)
        {
            cameras = new List<Camera>();
            for (int i = 0; i < MAX_CAMERAS; i++)
            {
                cameras.Add(new GameObject(CAMERA_NAME_PREFIX + i).AddComponent<Camera>());
                cameras[i].transform.SetParent(cameraParent.transform, true);
                cameras[i].gameObject.SetActive(false);
                cameras[i].transform.localRotation = Quaternion.identity;
                cameras[i].clearFlags = mainCamera.clearFlags;
                cameras[i].backgroundColor = mainCamera.backgroundColor;
                cameras[i].targetDisplay = mainCamera.targetDisplay;

#if G3D_HDRP
                cameras[i].gameObject.AddComponent<HDAdditionalCameraData>();
#endif
            }
        }
    }

    private int getCameraCountFromCalibrationFile()
    {
        if (calibrationFile == null)
        {
            Debug.LogError(
                "No calibration file set. Please set a calibration file. Using default values."
            );
            return 2;
        }

        CalibrationProvider calibration = CalibrationProvider.getFromString(calibrationFile.text);
        return getCameraCountFromCalibrationFile(calibration);
    }

    private int getCameraCountFromCalibrationFile(CalibrationProvider calibration)
    {
        int NativeViewcount = calibration.getInt("NativeViewcount");
        return NativeViewcount;
    }

    private Vector2Int getDisplayResolutionFromCalibrationFile()
    {
        if (calibrationFile == null)
        {
            Debug.LogError(
                "No calibration file set. Please set a calibration file. Using default values."
            );
            return new Vector2Int(1920, 1080);
        }

        CalibrationProvider calibration = CalibrationProvider.getFromString(calibrationFile.text);
        int HorizontalResolution = calibration.getInt("HorizontalResolution");
        int VerticalResolution = calibration.getInt("VerticalResolution");
        return new Vector2Int(HorizontalResolution, VerticalResolution);
    }

    private void initLibrary()
    {
        string applicationName = Application.productName;
        if (string.IsNullOrEmpty(applicationName))
        {
            applicationName = "Unity";
        }
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        applicationName = String
            .Join("_", applicationName.Split(invalids, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
        applicationName = applicationName + "_G3D_Config.ini";

        try
        {
            bool useHimaxD2XXDevices = true;
            bool useHimaxRP2040Devices = true;
            bool usePmdFlexxDevices = true;

            libInterface = LibInterface.Instance;
            libInterface.init(
                calibrationPath,
                Application.persistentDataPath,
                applicationName,
                this,
                this,
                this,
                debugMessages,
                useHimaxD2XXDevices,
                useHimaxRP2040Devices,
                usePmdFlexxDevices
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize library: " + e.Message);
            return;
        }

        // set initial values
        // intialize head position at focus distance from focus plane
        headPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = -focusDistance
        };
        filteredHeadPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = -focusDistance
        };

        if (usePositionFiltering())
        {
            try
            {
                libInterface.initializePositionFilter(
                    headPositionFilter.x,
                    headPositionFilter.y,
                    headPositionFilter.z
                );
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to initialize position filter: " + e.Message);
            }
        }
    }

    private void deinitLibrary()
    {
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        try
        {
            libInterface.stopHeadTracking();
            libInterface.unregisterHeadPositionChangedCallback(this);
            libInterface.unregisterShaderParametersChangedCallback(this);
            libInterface.unregisterMessageCallback(this);
            libInterface.deinit();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private void reinitializeShader()
    {
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            if (useVectorMapViewGeneration && generateViews)
            {
                material = new Material(Shader.Find("G3D/MultiviewMosaicVector"));
            }
            else if (!useVectorMapViewGeneration && generateViews)
            {
                material = new Material(Shader.Find("G3D/AutostereoMultiviewMosaic"));
            }
            else
            {
                material = new Material(Shader.Find("G3D/AutostereoMultiview"));
            }
        }
        else
        {
            material = new Material(Shader.Find("G3D/Autostereo"));
        }
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

        if (mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing)
        {
            for (int i = 0; i < MAX_CAMERAS; i++)
            {
                cameras[i].GetUniversalAdditionalCameraData().renderPostProcessing = true;
            }
        }
    }
#endif

    #endregion

    #region Updates
    void Update()
    {
        // update the shader parameters (only in diorama mode)
        if (mode == G3DCameraMode.DIORAMA)
        {
            libInterface.calculateShaderParameters(latencyCorrectionMode);
            lock (shaderLock)
            {
                shaderParameters = libInterface.getCurrentShaderParameters();
            }
        }

        bool cameraCountChanged = updateCameraCountBasedOnMode();
        updateCameras();
        updateShaderParameters();

        bool screenSizeChanged = false;
        if (windowResized() || windowMoved())
        {
            updateScreenViewportProperties();
            screenSizeChanged = true;
        }

        if (
            screenSizeChanged
            || cameraCountChanged
            || oldRenderResolutionScale != renderResolutionScale
        )
        {
            oldRenderResolutionScale = renderResolutionScale;
            updateShaderRenderTextures();
        }
    }

    private void updateScreenViewportProperties()
    {
        Vector2Int displayResolution = getDisplayResolutionFromCalibrationFile();
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            shaderParameters.screenWidth = displayResolution.x;
            shaderParameters.screenHeight = displayResolution.y;
            shaderParameters.leftViewportPosition = Screen.mainWindowPosition.x;
            shaderParameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height;
        }
        else
        {
            try
            {
                // This is the size of the entire monitor screen
                libInterface.setScreenSize(displayResolution.x, displayResolution.y);

                // this refers to the window in which the 3D effect is rendered (including eg windows top window menu)
                libInterface.setWindowSize(Screen.width, Screen.height);
                libInterface.setWindowPosition(
                    Screen.mainWindowPosition.x,
                    Screen.mainWindowPosition.y
                );

                // This refers to the actual viewport in which the 3D effect is rendered
                libInterface.setViewportSize(Screen.width, Screen.height);
                libInterface.setViewportOffset(0, 0);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to update screen viewport properties: " + e.Message);
            }
        }

        // this parameter is used in the shader to invert the y axis
        material?.SetInt(Shader.PropertyToID("viewportHeight"), Screen.height);
        material?.SetInt(Shader.PropertyToID("viewportWidth"), Screen.width);
    }

    private void updateShaderParameters()
    {
        lock (shaderLock)
        {
            material?.SetInt(
                shaderHandles.leftViewportPosition,
                shaderParameters.leftViewportPosition
            );
            material?.SetInt(
                shaderHandles.bottomViewportPosition,
                shaderParameters.bottomViewportPosition
            );
            material?.SetInt(shaderHandles.screenWidth, shaderParameters.screenWidth);
            material?.SetInt(shaderHandles.screenHeight, shaderParameters.screenHeight);
            material?.SetInt(shaderHandles.nativeViewCount, shaderParameters.nativeViewCount);
            material?.SetInt(
                shaderHandles.angleRatioNumerator,
                shaderParameters.angleRatioNumerator
            );
            material?.SetInt(
                shaderHandles.angleRatioDenominator,
                shaderParameters.angleRatioDenominator
            );
            material?.SetInt(
                shaderHandles.leftLensOrientation,
                shaderParameters.leftLensOrientation
            );
            material?.SetInt(shaderHandles.mstart, shaderParameters.mstart);

            // test frame and stripe
            material?.SetInt(shaderHandles.showTestFrame, showTestFrame ? 1 : 0);
            material?.SetInt(shaderHandles.showTestStripe, shaderParameters.showTestStripe);

            material?.SetInt(shaderHandles.testGapWidth, shaderParameters.testGapWidth);
            material?.SetInt(shaderHandles.track, shaderParameters.track);
            material?.SetInt(shaderHandles.hqViewCount, shaderParameters.hqViewCount);
            material?.SetInt(shaderHandles.hviews1, shaderParameters.hviews1);
            material?.SetInt(shaderHandles.hviews2, shaderParameters.hviews2);
            material?.SetInt(shaderHandles.blur, shaderParameters.blur);
            material?.SetInt(shaderHandles.blackBorder, shaderParameters.blackBorder);
            material?.SetInt(shaderHandles.blackSpace, shaderParameters.blackSpace);
            material?.SetInt(shaderHandles.bls, shaderParameters.bls);
            material?.SetInt(shaderHandles.ble, shaderParameters.ble);
            material?.SetInt(shaderHandles.brs, shaderParameters.brs);
            material?.SetInt(shaderHandles.bre, shaderParameters.bre);
            material?.SetInt(shaderHandles.zCorrectionValue, shaderParameters.zCorrectionValue);
            material?.SetInt(shaderHandles.zCompensationValue, shaderParameters.zCompensationValue);
            material?.SetInt(shaderHandles.BGRPixelLayout, shaderParameters.BGRPixelLayout);

            material?.SetInt(Shader.PropertyToID("cameraCount"), internalCameraCount);

            material?.SetInt(Shader.PropertyToID("mirror"), mirrorViews ? 1 : 0);

            material?.SetInt(Shader.PropertyToID("mosaic_rows"), 4);
            material?.SetInt(Shader.PropertyToID("mosaic_columns"), 4);
        }
    }

    void updateCameras()
    {
        Vector3 targetPosition = new Vector3(0, 0, -scaledFocusDistance); // position for the camera center (base position from which all other cameras are offset)
        float targetViewSeparation = 0.0f;

        // calculate the camera center position and eye separation if head tracking and the diorama effect are enabled
        if (mode == G3DCameraMode.DIORAMA)
        {
            HeadPosition headPosition;
            if (usePositionFiltering())
            {
                headPosition = getFilteredHeadPosition();
            }
            else
            {
                headPosition = getHeadPosition();
            }
            // head detected
            if (headPosition.headDetected)
            {
                Vector3 headPositionWorld = new Vector3(
                    (float)headPosition.worldPosX,
                    (float)headPosition.worldPosY,
                    (float)headPosition.worldPosZ
                );

                targetPosition = headPositionWorld;
                targetViewSeparation = scaledViewSeparation;
            }
        }
        else if (mode == G3DCameraMode.MULTIVIEW)
        {
            targetViewSeparation = scaledViewSeparation;
        }

        cameraParent.transform.localPosition = targetPosition;

        float horizontalOffset = targetPosition.x;
        float verticalOffset = targetPosition.y;

        float currentFocusDistance = -cameraParent.transform.localPosition.z;
        float dollyZoomOffset = currentFocusDistance - currentFocusDistance * dollyZoom;

        float focusDistanceWithDollyZoom = currentFocusDistance - dollyZoomOffset;
        mainCamera.fieldOfView =
            2
            * Mathf.Atan(scaledHalfCameraWidthAtStart / focusDistanceWithDollyZoom)
            * Mathf.Rad2Deg;

        // set the camera parent position to the focus distance
        cameraParent.transform.localPosition = new Vector3(
            horizontalOffset,
            verticalOffset,
            -currentFocusDistance
        );

        //calculate camera positions and matrices
        for (int i = 0; i < internalCameraCount; i++)
        {
            var camera = cameras[i];
            //copy any changes to the main camera
            camera.fieldOfView = mainCamera.fieldOfView;
            camera.farClipPlane = mainCamera.farClipPlane;
            camera.nearClipPlane = mainCamera.nearClipPlane;
            camera.projectionMatrix = mainCamera.projectionMatrix;
            camera.transform.localRotation = cameraParent.transform.localRotation;
#if G3D_URP
            camera.GetUniversalAdditionalCameraData().antialiasing = antialiasingMode;
#endif
#if G3D_HDRP
            HDAdditionalCameraData hdAdditionalCameraData =
                camera.gameObject.GetComponent<HDAdditionalCameraData>();
            if (hdAdditionalCameraData != null)
                hdAdditionalCameraData.antialiasing = antialiasingMode;
#endif

            float localCameraOffset = calculateCameraOffset(
                i,
                targetViewSeparation,
                internalCameraCount
            );

            // apply new projection matrix
            Matrix4x4 projMatrix = calculateCameraProjectionMatrix(
                localCameraOffset,
                horizontalOffset,
                verticalOffset,
                focusDistanceWithDollyZoom,
                camera.projectionMatrix
            );

            camera.projectionMatrix = projMatrix;

            camera.transform.localPosition = new Vector3(localCameraOffset, 0, 0);

            // if generate views only enable the leftmost and rightmost camera
            if (generateViews)
            {
                if (i == 0 || i == internalCameraCount / 2 || i == internalCameraCount - 1)
                {
                    camera.gameObject.SetActive(true);
                }
                else
                {
                    camera.gameObject.SetActive(false);
                }
            }
            else
            {
                // enable all cameras
                camera.gameObject.SetActive(true);
            }
        }

        //disable all the other cameras, we are not using them with this cameracount
        for (int i = internalCameraCount; i < MAX_CAMERAS; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Sets the camera count to two if we are in diorama mode. Sets it to the maximum amount of views the display is capable of if we are in multiview mode.
    /// </summary>
    /// <returns>true if camera count was changed.</returns>
    private bool updateCameraCountBasedOnMode()
    {
        int previousCameraCount = internalCameraCount;
        if (mode == G3DCameraMode.DIORAMA)
        {
            internalCameraCount = 2;
        }
        else if (mode == G3DCameraMode.MULTIVIEW)
        {
            // internalCameraCount = getCameraCountFromCalibrationFile();
            // TODO DO NOT HARD CODE THIS VALUE!
            internalCameraCount = 16;
            if (internalCameraCount > MAX_CAMERAS)
            {
                internalCameraCount = MAX_CAMERAS;
            }
        }

        if (internalCameraCount != previousCameraCount)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// adds rendertextres for the cameras to the shader and sets them as target textures for the cameras.
    /// </summary>
    /// <param name="renderTextures"></param>
    /// <param name="renderTextureIndex"></param>
    /// <param name="cameraIndex"></param>
    /// <param name="texNameInShader">only used if view generation is turned on. used to specify the texture name (left, right, middle)</param>
    private void addRenderTextureToCamera(
        RenderTexture[] renderTextures,
        int renderTextureIndex,
        int cameraIndex,
        string texNameInShader = "texture"
    )
    {
        int width = Screen.width;
        int height = Screen.height;

        if (adaptRenderResolutionToViews)
        {
            // TODO: This is a temporary fix for the resolution scaling issue. Use an actually correct formula here.
            width = width / internalCameraCount;
        }
        else
        {
            width = (int)(width * ((float)renderResolutionScale / 100f));
            height = (int)(height * ((float)renderResolutionScale / 100f));
        }

        renderTextures[renderTextureIndex] = new RenderTexture(width, height, 0)
        {
            format = RenderTextureFormat.ARGB32,
            depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm,
        };
        cameras[cameraIndex].targetTexture = renderTextures[renderTextureIndex];
        material.SetTexture(
            "texture" + renderTextureIndex,
            renderTextures[renderTextureIndex],
            RenderTextureSubElement.Color
        );

        if (generateViews)
        {
            viewGenerationMaterial.SetTexture(
                texNameInShader,
                renderTextures[renderTextureIndex],
                RenderTextureSubElement.Color
            );
        }
    }

    public void updateShaderRenderTextures()
    {
        if (material == null)
            return;
        if (cameras == null)
            return;

        //prevent any memory leaks
        for (int i = 0; i < MAX_CAMERAS; i++)
            cameras[i].targetTexture?.Release();

        RenderTexture[] renderTextures = new RenderTexture[internalCameraCount];

        if (generateViews)
        {
            addRenderTextureToCamera(renderTextures, 0, 0, "_leftCamTex"); // left camera
            addRenderTextureToCamera(renderTextures, 1, internalCameraCount / 2, "_middleCamTex"); // middle camera
            addRenderTextureToCamera(renderTextures, 2, internalCameraCount - 1, "_rightCamTex"); // right camera
        }
        else
        {
            //set only those we need
            for (int i = 0; i < internalCameraCount; i++)
            {
                addRenderTextureToCamera(renderTextures, i, i);
            }
        }
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="localCameraOffset">Offset (along the x axis) of this camera compared to zero position.</param>
    /// <param name="horizontalOffset">general offset (x axis) of the "zero position" compared to start position due to head tracking.</param>
    /// <param name="verticalOffset">general offset (y axis) of the "zero position" compared to start position due to head tracking.</param>
    /// <param name="focusDistance">general offset (z axis) of the "zero position" compared to start position due to head tracking.</param>
    /// <param name="mainCamProjectionMatrix"></param>
    /// <returns></returns>
    private Matrix4x4 calculateCameraProjectionMatrix(
        float localCameraOffset,
        float horizontalOffset,
        float verticalOffset,
        float focusDistance,
        Matrix4x4 mainCamProjectionMatrix
    )
    {
        // horizontal obliqueness
        float horizontalObl = -(localCameraOffset + horizontalOffset) / focusDistance;
        float vertObl = -verticalOffset / focusDistance;

        // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
        Matrix4x4 shearMatrix = Matrix4x4.identity;
        shearMatrix[0, 2] = horizontalObl;
        shearMatrix[1, 2] = vertObl;
        // apply new projection matrix
        return mainCamProjectionMatrix * shearMatrix;
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
    /// always use this method to get the current head position.
    /// NEVER access headPosition directly, as it is updated in a different thread.
    ///
    /// </summary>
    /// <returns></returns>
    public HeadPosition getHeadPosition()
    {
        lock (headPosLock)
        {
            return headPosition;
        }
    }

    /// <summary>
    /// always use this method to get the smoothed head position.
    /// NEVER access headPosition directly, as it is updated in a different thread.
    ///
    /// </summary>
    /// <returns></returns>
    public HeadPosition getFilteredHeadPosition()
    {
        lock (headPosLock)
        {
            return filteredHeadPosition;
        }
    }

    #region callback handling
    void ITNewHeadPositionCallback.NewHeadPositionCallback(
        bool headDetected,
        bool imagePosIsValid,
        int imagePosX,
        int imagePosY,
        double worldPosX,
        double worldPosY,
        double worldPosZ
    )
    {
        lock (headPosLock)
        {
            string logEntry =
                DateTime.Now.ToString("HH:mm::ss.fff")
                + ";"
                + worldPosX
                + ";"
                + worldPosY
                + ";"
                + worldPosZ
                + ";"
                + headDetected
                + ";"
                + imagePosIsValid
                + ";";

            headPosition.headDetected = headDetected;
            headPosition.imagePosIsValid = imagePosIsValid;

            int millimeterToMeter = 1000;

            Vector3 headPos = new Vector3(
                (float)-worldPosX / millimeterToMeter,
                (float)worldPosY / millimeterToMeter,
                (float)-worldPosZ / millimeterToMeter
            );

            int scaleFactorInt = (int)sceneScaleFactor * (int)headTrackingScale;
            float scaleFactor = sceneScaleFactor * headTrackingScale;

            headPosition.imagePosX = imagePosX / (int)millimeterToMeter * scaleFactorInt;
            headPosition.imagePosY = imagePosY / (int)millimeterToMeter * scaleFactorInt;
            headPosition.worldPosX = headPos.x * scaleFactor;
            headPosition.worldPosY = headPos.y * scaleFactor;
            headPosition.worldPosZ = headPos.z * sceneScaleFactor;

            if (usePositionFiltering())
            {
                double filteredPositionX;
                double filteredPositionY;
                double filteredPositionZ;

                if (headDetected)
                {
                    try
                    {
                        libInterface.applyPositionFilter(
                            worldPosX,
                            worldPosY,
                            worldPosZ,
                            out filteredPositionX,
                            out filteredPositionY,
                            out filteredPositionZ
                        );

                        filteredHeadPosition.worldPosX =
                            -filteredPositionX / millimeterToMeter * scaleFactor;
                        filteredHeadPosition.worldPosY =
                            filteredPositionY / millimeterToMeter * scaleFactor;
                        filteredHeadPosition.worldPosZ =
                            -filteredPositionZ / millimeterToMeter * sceneScaleFactor;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to apply position filter: " + e.Message);
                    }
                }

                filteredHeadPosition.headDetected = headDetected;
                filteredHeadPosition.imagePosIsValid = imagePosIsValid;

                logEntry +=
                    filteredHeadPosition.worldPosX
                    + ";"
                    + filteredHeadPosition.worldPosY
                    + ";"
                    + filteredHeadPosition.worldPosZ
                    + ";";
            }

            headPositionLog.Enqueue(logEntry);
        }
    }

    void ITNewErrorMessageCallback.NewErrorMessageCallback(
        EMessageSeverity severity,
        string sender,
        string caption,
        string cause,
        string remedy
    )
    {
        string messageText = formatErrorMessage(caption, cause, remedy);
        switch (severity)
        {
            case EMessageSeverity.MS_EXCEPTION:
                Debug.LogError(messageText);
                break;
            case EMessageSeverity.MS_ERROR:
                Debug.LogError(messageText);
                break;
            case EMessageSeverity.MS_WARNING:
                Debug.LogWarning(messageText);
                break;
            case EMessageSeverity.MS_INFO:

                Debug.Log(messageText);
                break;
            default:
                Debug.Log(messageText);
                break;
        }
    }

    /// <summary>
    /// The shader parameters contain everything necessary for the shader to render the 3D effect.
    /// These are updated every time a new head position is received.
    /// They do not update the head position itself.
    /// </summary>
    /// <param name="shaderParameters"></param>
    void ITNewShaderParametersCallback.NewShaderParametersCallback(
        G3DShaderParameters shaderParameters
    )
    {
        lock (shaderLock)
        {
            this.shaderParameters = shaderParameters;
        }
    }

    private string formatErrorMessage(string caption, string cause, string remedy)
    {
        string messageText = caption + ": " + cause;

        if (string.IsNullOrEmpty(remedy) == false)
        {
            messageText = messageText + "\\n" + remedy;
        }

        return messageText;
    }
    #endregion

    #region Debugging
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }

        if (enabled == false)
        {
            // do not run this code if the script is not enabled
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = GetComponent<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError(
                    "No main camera found. Please add a camera to the G3DCamera object."
                );
                return;
            }
        }

        float dollyZoomFactor = scaledFocusDistance - scaledFocusDistance * dollyZoom;

        float tmpHalfCameraWidthAtStart =
            Mathf.Tan(baseFieldOfView * Mathf.Deg2Rad / 2) * scaledFocusDistance;

        float focusDistanceWithDollyZoom = scaledFocusDistance - dollyZoomFactor;
        float tmpFieldOfView =
            2 * Mathf.Atan(tmpHalfCameraWidthAtStart / focusDistanceWithDollyZoom) * Mathf.Rad2Deg;
        float fieldOfViewWithoutDolly =
            2 * Mathf.Atan(tmpHalfCameraWidthAtStart / scaledFocusDistance) * Mathf.Rad2Deg;

        Vector3 basePosition = new Vector3(0, 0, focusDistanceWithDollyZoom);

        Vector3 position;
        // draw eye separation
        Gizmos.color = new Color(0, 0, 1, 0.75F);
        Gizmos.matrix = transform.localToWorldMatrix;
        // draw camera position spheres
        for (int i = 0; i < internalCameraCount; i++)
        {
            float localCameraOffset = calculateCameraOffset(
                i,
                scaledViewSeparation,
                internalCameraCount
            );
            Vector3 camPos = basePosition - new Vector3(0, 0, focusDistanceWithDollyZoom);
            camPos += new Vector3(1, 0, 0) * localCameraOffset;
            Gizmos.DrawSphere(camPos, 0.2f * gizmoSize * sceneScaleFactor);
        }

        // draw camera frustums
        Gizmos.color = new Color(0, 0, 1, 1); // set color to one wo improve visibility
        for (int i = 0; i < internalCameraCount; i++)
        {
            float localCameraOffset = calculateCameraOffset(
                i,
                scaledViewSeparation,
                internalCameraCount
            );
            position = transform.position + transform.right * localCameraOffset;

            // apply new projection matrix
            Matrix4x4 localProjectionMatrix = Matrix4x4.TRS(
                position,
                transform.rotation,
                Vector3.one
            );
            Matrix4x4 projMatrix = calculateCameraProjectionMatrix(
                localCameraOffset,
                0,
                0,
                focusDistanceWithDollyZoom,
                localProjectionMatrix
            );

            Gizmos.matrix = projMatrix;

            Gizmos.DrawFrustum(
                Vector3.zero,
                tmpFieldOfView,
                mainCamera.farClipPlane,
                mainCamera.nearClipPlane,
                mainCamera.aspect
            );
        }

        Gizmos.matrix = transform.localToWorldMatrix;

        // draw focus plane
        Gizmos.color = new Color(0, 0, 1, 0.25F);
        position = new Vector3(0, 0, 1) * focusDistanceWithDollyZoom;
        float frustumWidth =
            Mathf.Tan(fieldOfViewWithoutDolly * Mathf.Deg2Rad / 2) * scaledFocusDistance * 2;
        float frustumHeight =
            Mathf.Tan(
                Camera.VerticalToHorizontalFieldOfView(fieldOfViewWithoutDolly, mainCamera.aspect)
                    * Mathf.Deg2Rad
                    / 2
            )
            * scaledFocusDistance
            * 2;
        Gizmos.DrawCube(position, new Vector3(frustumHeight, frustumWidth, 0.001f));
    }
#endif
    #endregion

    private float calculateCameraOffset(
        int currentCamera,
        float targetEyeSeparation,
        int tmpCameraCount
    )
    {
        int currentView = -tmpCameraCount / 2 + currentCamera;
        if (tmpCameraCount % 2 == 0 && currentView >= 0)
        {
            currentView += 1;
        }

        // it is used to scale the offset to the ends for a mosaic texture where the middle textures are missing
        float offset = currentView * targetEyeSeparation;

        // when the camera count is even, one camera is placed half the eye separation to the right of the center
        // same for the other to the left
        // therefore we need to add the correction term to the offset to get the correct position
        if (tmpCameraCount % 2 == 0)
        {
            // subtract half of the eye separation to get the correct offset
            float correctionTerm = targetEyeSeparation / 2;
            if (currentView > 0)
            {
                correctionTerm *= -1;
            }
            offset = offset + correctionTerm;
        }

        int flip = mirrorViews ? 1 : -1;

        return offset * flip;
    }

    /// <summary>
    /// Returns false if all values of the position filter are set to zero.
    /// </summary>
    /// <returns></returns>
    private bool usePositionFiltering()
    {
        return headPositionFilter.x != 0 || headPositionFilter.y != 0 || headPositionFilter.z != 0;
    }

    public void logCameraPositionsToFile()
    {
        System.IO.StreamWriter writer = new System.IO.StreamWriter(
            Application.dataPath + "/HeadPositionLog.csv",
            false
        );
        writer.WriteLine(
            "Camera update time; Camera X; Camera Y; Camera Z; Head detected; Image position valid; Filtered X; Filtered Y; Filtered Z"
        );
        string[] headPoitionLogArray = headPositionLog.ToArray();
        for (int i = 0; i < headPoitionLogArray.Length; i++)
        {
            writer.WriteLine(headPoitionLogArray[i]);
        }
        writer.Close();
    }

    public void shiftViewToLeft()
    {
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            return;
        }
        try
        {
            libInterface.shiftViewToLeft();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to shift view to left: " + e.Message);
        }
    }

    public void shiftViewToRight()
    {
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            return;
        }
        try
        {
            libInterface.shiftViewToRight();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to shift view to right: " + e.Message);
        }
    }

    public void toggleHeadTracking()
    {
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            return;
        }
        Debug.Log("Toggling head tracking status");
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        try
        {
            HeadTrackingStatus headTrackingState = libInterface.getHeadTrackingStatus();
            if (headTrackingState.hasTrackingDevice)
            {
                if (!headTrackingState.isTrackingActive)
                {
                    libInterface.startHeadTracking();
                }
                else
                {
                    libInterface.stopHeadTracking();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to toggle head tracking status: " + e.Message);
        }
    }
}
