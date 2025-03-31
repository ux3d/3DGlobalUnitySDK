using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR

#endif

#if G3D_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if G3D_URP
using UnityEngine.Rendering.Universal;
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
    #region Calibration
    [Header("Calibration and configuration files")]
    [Tooltip(
        "This path has to be set to the directory where the folder containing the calibration files for your monitor are located. The folder has to have the same name as your camera model."
    )]
    public string calibrationPath = "";

    [Tooltip(
        "This path has to be set to the directory where the folder containing the calibration files for your monitor are located. The folder has to have the same name as your camera model."
    )]
    public string configPath = "";
    public string configFileName = "";

    [Tooltip(
        "If set to true, the library will look for the calibration and config files in a \"config\" named subdirectory of the directory where the executable is located."
    )]
    public bool useExecDirectory = false;

    [Tooltip(
        "If not null the initial set of shader parameters will be based on this calibration file. If null the default shader parameters from the library will be used."
    )]
    public string customDefaultCalibrationFilePath = null;
    #endregion

    #region 3D Effect settings
    [Header("3D Effect settings")]
    [Tooltip(
        "The amount of used cameras. The maximum amount of cameras is 16. Two corresponds to a stereo setup."
    )]
    [Range(1, 16)]
    public int cameraCount = 2;
    private int internalCameraCount = 2;

    [Tooltip(
        "If set to true, the amount of cameras will be limited by the amount of native views the display supports."
    )]
    public bool lockCameraCountToDisplay = false;

    public G3DCameraMode mode = G3DCameraMode.DIORAMA;

    [Tooltip(
        "The distance between the two eyes in meters. This value is used to calculate the stereo effect."
    )]
    [Range(0.00001f, 5.0f)]
    public float eyeSeparation = 0.065f;

    [Tooltip(
        "The amount the eye separation changes when the user presses the eye separation keys (in meter)."
    )]
    public float eyeSeparationChange = 0.001f;
    private float prevEyeSeparation = 0.065f;

    [Tooltip("The distance between the camera and the focus plane in meters. Defaults is 70 cm.")]
    [Min(0.0000001f)]
    public float focusDistance = 0.7f;

    [Tooltip(
        "The maximum distance between the head and the focus plane (i.e. display) in meter. If the head is further away, the head position will not be updated."
    )]
    public float maxHeadDistance = 0.85f;

    [Tooltip("Time it takes till the reset animation starts in seconds.")]
    public float headLostTimeoutInSec = 3.0f;

    [Tooltip("Reset animation duratuion in seconds.")]
    public float transitionDuration = 1.5f;

    private const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public static string CAMERA_NAME_PREFIX = "g3dcam_";

    [Tooltip(
        "This value can be used to scale the real world values used to calibrate the extension. For example if your scene is 10 times larger than the real world, you can set this value to 10. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!"
    )]
    public float sceneScaleFactor = 1.0f;

    [Tooltip(
        "Smoothes the head position (Size of the filter kernel). Not filtering is applied, if set to all zeros. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!"
    )]
    public Vector3Int headPositionFilter = new Vector3Int(5, 5, 5);

    private Vector3 lastHeadPosition = new Vector3(0, 0, 0);

    [Tooltip("If set to true, the views will be flipped horizontally.")]
    public bool mirrorViews = false;

    public LatencyCorrectionMode latencyCorrectionMode = LatencyCorrectionMode.LCM_SIMPLE;

    [Tooltip(
        "If set to true, the render targets for the individual views will be adapted to the resolution actually visible on screen. e.g. for two views each render target will have half the screen width. Overwrites Render Resolution Scale."
    )]
    public bool adaptRenderResolutionToViews = true;
    
    [Tooltip(
        "Set a percentage value to render only that percentage of the width and height per view. E.g. a reduction of 50% will reduce the rendered size by a factor of 4. Adapt Render Resolution To Views takes precedence."
    )]
    [Range(1, 100)]
    public int renderResolutionScale = 100;
    #endregion

    #region Device settings
    [Header("Device settings")]
    public bool useHimaxD2XXDevices = true;
    public bool useHimaxRP2040Devices = true;
    public bool usePmdFlexxDevices = true;
    #endregion

    #region Debugging
    [Header("Debugging")]
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

    private int oldRenderTargetScaleFactor = 5;
    private int oldRenderResolutionScale = 100;

    #endregion

    #region Keys
    [Header("Keys")]
    [Tooltip("If set to true, the library will react to certain keyboard keys.")]
    public bool enableKeys = true;
    public KeyCode toggleHeadTrackingKey = KeyCode.Space;
    public KeyCode shiftViewLeftKey = KeyCode.LeftArrow;
    public KeyCode shiftViewRightKey = KeyCode.RightArrow;

    [Tooltip("Shows a red/green test frame.")]
    public KeyCode toggleTestFrameKey = KeyCode.D;
    public KeyCode switchCameraMode = KeyCode.H;
    public KeyCode decreaseEyeSeparationKey = KeyCode.K;
    public KeyCode increaseEyeSeparationKey = KeyCode.L;
    public KeyCode cameraPositionLogginKey = KeyCode.W;
    public KeyCode increaseRenderScale = KeyCode.UpArrow;
    public KeyCode decreaseRenderScale = KeyCode.DownArrow;
    #endregion

    #region Private variables

    private LibInterface libInterface;

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
    private int renderTargetScaleFactor = 5;

    private Material material;
#if G3D_HDRP
    private G3DHDRPCustomPass customPass;
#endif
#if G3D_URP
    private G3DUrpScriptableRenderPass customPass;
    AntialiasingMode antialiasingMode = AntialiasingMode.None;
#endif

    private int[] id_View = new int[MAX_CAMERAS];
    private ShaderHandles shaderHandles;
    private G3DShaderParameters shaderParameters;

    private Vector2Int cachedWindowPosition;
    private Vector2Int cachedWindowSize;
    private int cachedCameraCount;

    // half of the width of field of view at start at focus distance
    private float halfCameraWidthAtStart = 1.0f;

    private float headLostTimer = 0.0f;
    private float transitionTime = 0.0f;

    private Queue<string> headPoitionLog;

    private enum HeadTrackingState
    {
        TRACKING,
        LOST,
        TRANSITIONTOLOST,
        TRANSITIONTOTRACKING,
        LOSTGRACEPERIOD
    }

    private HeadTrackingState prevHeadTrackingState = HeadTrackingState.LOST;

    #endregion

    // TODO Handle viewport resizing/ moving

    #region Initialization
    void Start()
    {
        maxHeadDistance = maxHeadDistance * sceneScaleFactor;
        eyeSeparation = eyeSeparation * sceneScaleFactor;

        mainCamera = GetComponent<Camera>();

        // create a focus plane object at foxus distance from camera.
        // then parent the camera parent to that object
        // this way we can place the camera parent relative to the focus plane

        focusPlaneObject = new GameObject("focus plane center");
        focusPlaneObject.transform.parent = transform;
        focusPlaneObject.transform.localPosition = new Vector3(0, 0, focusDistance);
        focusPlaneObject.transform.localRotation = Quaternion.identity;

        //initialize cameras

        cameraParent = new GameObject("g3dcams");
        cameraParent.transform.parent = focusPlaneObject.transform;
        cameraParent.transform.localPosition = new Vector3(0, 0, -focusDistance);
        cameraParent.transform.localRotation = Quaternion.identity;

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
        }

        // disable rendering on main camera after other cameras have been created and settings have been copied over
        // otherwise the secondary cameras are initialized wrong
        mainCamera.cullingMask = 0; //disable rendering of the main camera
        mainCamera.clearFlags = CameraClearFlags.Color;

        // initialize shader textures
        for (int i = 0; i < MAX_CAMERAS; i++)
            id_View[i] = Shader.PropertyToID("texture" + i);

        shaderHandles = new ShaderHandles()
        {
            leftViewportPosition = Shader.PropertyToID("v_pos_x"),
            bottomViewportPosition = Shader.PropertyToID("v_pos_y"),
            screenWidth = Shader.PropertyToID("s_width"),
            screenHeight = Shader.PropertyToID("s_height"),
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

        initLibrary();
        reinitializeShader();
        updateScreenViewportProperties();

        lock (shaderLock)
        {
            DefaultCalibrationProvider defaultCalibrationProvider =
                DefaultCalibrationProvider.getFromConfigFile(customDefaultCalibrationFilePath);
            shaderParameters = defaultCalibrationProvider.getDefaultShaderParameters();
        }
        updateShaderParameters();

        try
        {
            libInterface.startHeadTracking();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start head tracking: " + e.Message);
        }

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
        antialiasingMode = mainCamera.GetUniversalAdditionalCameraData().antialiasing;
        mainCamera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.None;
#endif

        halfCameraWidthAtStart =
            Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad / 2) * focusDistance;

        updateShaderRenderTextures(true);
        updateCameras();

        // This has to be done after the cameras are updated
        cachedWindowPosition = new Vector2Int(
            Screen.mainWindowPosition.x,
            Screen.mainWindowPosition.y
        );
        cachedWindowSize = new Vector2Int(Screen.width, Screen.height);

        prevEyeSeparation = eyeSeparation;

        headPoitionLog = new Queue<string>(10000);
    }

    void OnApplicationQuit()
    {
        deinitLibrary();
    }

    private void initLibrary()
    {
        if (useExecDirectory)
        {
            configPath = Application.dataPath + "/config";
            calibrationPath = Application.dataPath + "/config";
        }

        try
        {
            libInterface = LibInterface.Instance;
            libInterface.init(
                calibrationPath,
                configPath,
                configFileName,
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
        lastHeadPosition = new Vector3(0, 0, -focusDistance);
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
            material = new Material(Shader.Find("G3D/AutostereoMultiview"));
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
        int tmpRenderScaleFactor = shaderParameters.nativeViewCount;
        if (cameraCount < tmpRenderScaleFactor)
        {
            tmpRenderScaleFactor = cameraCount;
        }

        renderTargetScaleFactor = tmpRenderScaleFactor;

        // update the shader parameters
        libInterface.calculateShaderParameters(latencyCorrectionMode);
        lock (shaderLock)
        {
            shaderParameters = libInterface.getCurrentShaderParameters();
        }

        updateShaderRenderTextures();
        updateCameras();
        updateShaderParameters();

        if (enableKeys)
        {
            handleKeyPresses();
        }

        if (windowResized() || windowMoved())
        {
            updateScreenViewportProperties();
            updateShaderRenderTextures(true);
        }
    }

    private void updateScreenViewportProperties()
    {
        try
        {
            // This is the size of the entire monitor screen
            libInterface.setScreenSize(Screen.width, Screen.height);

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

        // this parameter is used in the shader to invert the y axis
        material?.SetInt(Shader.PropertyToID("viewportHeight"), Screen.height);
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
        }
    }

    [ContextMenu("Toggle head tracking")]
    public void toggleHeadTrackingStatus()
    {
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

    void updateCameras()
    {
        Vector3 defaultPostion = new Vector3(0, 0, -focusDistance);
        Vector3 targetPosition = defaultPostion; // position for the camera center (base position from which all other cameras are offset)
        float targetEyeSeparation = 0.0f;

        // calculate the camera center position and eye separation if head tracking and the diorama effect are enabled
        handleHeadTrackingState(ref targetPosition, ref targetEyeSeparation);

        cameraParent.transform.localPosition = targetPosition;
        float horizontalOffset = targetPosition.x;
        float verticalOffset = targetPosition.y;

        if (mode == G3DCameraMode.MULTIVIEW)
        {
            targetEyeSeparation = eyeSeparation;
            cameraParent.transform.localPosition = defaultPostion;
        }

        float currentFocusDistance = -cameraParent.transform.localPosition.z;

        mainCamera.fieldOfView =
            2 * Mathf.Atan(halfCameraWidthAtStart / currentFocusDistance) * Mathf.Rad2Deg;

        //calculate camera positions and matrices
        for (int i = 0; i < internalCameraCount; i++)
        {
            var camera = cameras[i];
            //copy any changes to the main camera
            camera.fieldOfView = mainCamera.fieldOfView;
            camera.farClipPlane = mainCamera.farClipPlane;
            camera.nearClipPlane = mainCamera.nearClipPlane;
            camera.projectionMatrix = mainCamera.projectionMatrix;
            camera.transform.localPosition = cameraParent.transform.localPosition;
            camera.transform.localRotation = cameraParent.transform.localRotation;
#if G3D_URP
            camera.GetUniversalAdditionalCameraData().antialiasing = antialiasingMode;
#endif

            float localCameraOffset = calculateCameraOffset(
                i,
                targetEyeSeparation,
                internalCameraCount
            );

            // horizontal obliqueness
            float horizontalObl = -(localCameraOffset + horizontalOffset) / currentFocusDistance;
            float vertObl = -verticalOffset / currentFocusDistance;

            // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
            Matrix4x4 shearMatrix = Matrix4x4.identity;
            shearMatrix[0, 2] = horizontalObl;
            shearMatrix[1, 2] = vertObl;
            // apply new projection matrix
            camera.projectionMatrix = camera.projectionMatrix * shearMatrix;

            camera.transform.localPosition = new Vector3(localCameraOffset, 0, 0);

            camera.gameObject.SetActive(true);
        }

        //disable all the other cameras, we are not using them with this cameracount
        for (int i = internalCameraCount; i < MAX_CAMERAS; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Ensures that the camera count is not higher than the maximum the display is capable of.
    /// </summary>
    /// <returns>true if camera count was changed.</returns>
    private bool lockCameraCountToShaderParameters()
    {
        int shaderMaxCount = shaderParameters.nativeViewCount;

        internalCameraCount = cameraCount;

        if (cameraCount > shaderMaxCount && lockCameraCountToDisplay)
        {
            internalCameraCount = shaderMaxCount;
            return true;
        }

        return false;
    }

    public void updateShaderRenderTextures(bool forceUpdate = false)
    {
        if (material == null)
            return;
        if (cameras == null)
            return;

        // check if shaderviews should be updated
        bool shouldUpdateShaderViews = false;
        if (oldRenderTargetScaleFactor != renderTargetScaleFactor)
        {
            oldRenderTargetScaleFactor = renderTargetScaleFactor;
            shouldUpdateShaderViews = true;
        }
        if (oldRenderResolutionScale != renderResolutionScale)
        {
            oldRenderResolutionScale = renderResolutionScale;
            shouldUpdateShaderViews = true;
        }
        if (lockCameraCountToShaderParameters())
        {
            shouldUpdateShaderViews = true;
        }
        if (cachedCameraCount != internalCameraCount)
        {
            cachedCameraCount = internalCameraCount;
            shouldUpdateShaderViews = true;
        }
        if (shouldUpdateShaderViews == false && forceUpdate == false)
        {
            return;
        }

        //prevent any memory leaks
        for (int i = 0; i < MAX_CAMERAS; i++)
            cameras[i].targetTexture?.Release();

        RenderTexture[] renderTextures = new RenderTexture[internalCameraCount];

        //set only those we need
        for (int i = 0; i < internalCameraCount; i++)
        {
            int width = Screen.width;
            int height = Screen.height;

            if (adaptRenderResolutionToViews) {
                width = width / renderTargetScaleFactor;
            } else {
                width = (int)(width * ((float)renderResolutionScale / 100f));
                height = (int)(height * ((float)renderResolutionScale / 100f));
            }

            renderTextures[i] = new RenderTexture(width, height, 0)
            {
                format = RenderTextureFormat.ARGB32,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm,
            };
            cameras[i].targetTexture = renderTextures[i];
            material.SetTexture("texture" + i, renderTextures[i], RenderTextureSubElement.Color);
        }
    }

    /**
    * Calculate the new camera center position based on the head tracking.
    * If head tracking is lost, or the head moves to far away from the tracking camera a grace periope is started.
    * Afterwards the camera center will be animated back towards the default position.
    */
    private void handleHeadTrackingState(ref Vector3 targetPosition, ref float targetEyeSeparation)
    {
        if (mode == G3DCameraMode.MULTIVIEW)
        {
            return;
        }
        HeadPosition headPosition;
        if (usePositionFiltering())
        {
            headPosition = getFilteredHeadPosition();
        }
        else
        {
            headPosition = getHeadPosition();
        }
        Vector3 defaultPostion = new Vector3(0, 0, -focusDistance);

        HeadTrackingState headTrackingState = HeadTrackingState.LOST;

        if (
            prevHeadTrackingState == HeadTrackingState.LOSTGRACEPERIOD
            || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
            || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            headTrackingState = prevHeadTrackingState;
        }
        // head detected
        if (headPosition.headDetected)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
            );

            // if head within max tracking distance
            if (headPositionWorld.magnitude <= maxHeadDistance * sceneScaleFactor)
            {
                headTrackingState = HeadTrackingState.TRACKING;
                targetPosition = headPositionWorld;
                targetEyeSeparation = eyeSeparation;
            }
        }

        // if reaquired and we were in lost state or transitioning to lost, start transition to tracking
        if (
            headTrackingState == HeadTrackingState.TRACKING
            && (
                prevHeadTrackingState == HeadTrackingState.LOST
                || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
            )
        )
        {
            headTrackingState = HeadTrackingState.TRANSITIONTOTRACKING;
            transitionTime = 0.0f;
        }

        if (
            headTrackingState == HeadTrackingState.TRACKING
            && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            headTrackingState = HeadTrackingState.TRANSITIONTOTRACKING;
        }

        // if lost, start grace period
        if (
            headTrackingState == HeadTrackingState.LOST
            && prevHeadTrackingState == HeadTrackingState.TRACKING
        )
        {
            headTrackingState = HeadTrackingState.LOSTGRACEPERIOD;
            targetPosition = lastHeadPosition;
        }

        if (headTrackingState == HeadTrackingState.LOSTGRACEPERIOD)
        {
            // if we have waited for the timeout
            if (Time.time - headLostTimer > headLostTimeoutInSec)
            {
                headTrackingState = HeadTrackingState.TRANSITIONTOLOST;
                headLostTimer = Time.time;
                transitionTime = 0.0f;
            }
            else
            {
                targetPosition = lastHeadPosition;
                targetEyeSeparation = prevEyeSeparation;
            }
        }

        // if we are in a transition when the transition flips reset the transition time
        if (
            headTrackingState == HeadTrackingState.TRANSITIONTOLOST
                && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
            || headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
                && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
        )
        {
            transitionTime = 0.0f;
        }

        if (
            headTrackingState == HeadTrackingState.TRANSITIONTOLOST
            || headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            // interpolate values
            float transitionPercentage = transitionTime / transitionDuration;
            transitionTime += Time.deltaTime;

            Vector3 transitionTargetPosition = defaultPostion;
            if (headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING)
            {
                transitionTargetPosition = targetPosition;
            }

            Vector3 interpolatedPosition = Vector3.Lerp(
                cameraParent.transform.localPosition,
                transitionTargetPosition,
                transitionPercentage
            );
            float interpolatedEyeSeparation = Mathf.Lerp(
                prevEyeSeparation,
                targetEyeSeparation,
                transitionPercentage
            );

            // apply values
            float distance = Vector3.Distance(interpolatedPosition, transitionTargetPosition);
            // only use interpolated position if we are not close enough to the target position
            if (distance > 0.0001f)
            {
                targetPosition = interpolatedPosition;
                targetEyeSeparation = interpolatedEyeSeparation;
            }
            else
            {
                // if we have reached the target position, we are no longer in transition
                if (headTrackingState == HeadTrackingState.TRANSITIONTOLOST)
                {
                    headTrackingState = HeadTrackingState.LOST;
                }
                else
                {
                    headTrackingState = HeadTrackingState.TRACKING;
                }
            }
        }

        // store last known position data for tracking loss case
        if (headTrackingState == HeadTrackingState.TRACKING)
        {
            lastHeadPosition = targetPosition;
            prevEyeSeparation = targetEyeSeparation;
        }

        prevHeadTrackingState = headTrackingState;
    }

    private void handleKeyPresses()
    {
        if (Input.GetKeyDown(toggleHeadTrackingKey))
        {
            toggleHeadTrackingStatus();
        }
        if (Input.GetKeyDown(shiftViewLeftKey))
        {
            try
            {
                libInterface.shiftViewToLeft();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to shift view to left: " + e.Message);
            }
        }
        if (Input.GetKeyDown(shiftViewRightKey))
        {
            try
            {
                libInterface.shiftViewToRight();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to shift view to right: " + e.Message);
            }
        }
        if (Input.GetKeyDown(toggleTestFrameKey))
        {
            showTestFrame = !showTestFrame;
        }
        if (Input.GetKeyDown(switchCameraMode))
        {
            if (mode == G3DCameraMode.DIORAMA)
            {
                mode = G3DCameraMode.MULTIVIEW;
            }
            else
            {
                mode = G3DCameraMode.DIORAMA;
            }
        }
        if (Input.GetKeyDown(decreaseEyeSeparationKey))
        {
            eyeSeparation -= eyeSeparationChange * sceneScaleFactor;
            if (eyeSeparation < 0.0f)
                eyeSeparation = 0.0f;
        }
        if (Input.GetKeyDown(increaseEyeSeparationKey))
        {
            eyeSeparation += eyeSeparationChange * sceneScaleFactor;
        }

        if (Input.GetKeyDown(cameraPositionLogginKey))
        {
            System.IO.StreamWriter writer = new System.IO.StreamWriter(
                Application.dataPath + "/HeadPositionLog.csv",
                false
            );
            writer.WriteLine(
                "Camera update time; Camera X; Camera Y; Camera Z; Head detected; Image position valid; Unity head tracking state; Used head X; Used head Y; Used head Z; Filtered X; Filtered Y; Filtered Z"
            );
            string[] headPoitionLogArray = headPoitionLog.ToArray();
            for (int i = 0; i < headPoitionLogArray.Length; i++)
            {
                writer.WriteLine(headPoitionLogArray[i]);
            }
            writer.Close();
        }

        if (Input.GetKeyDown(increaseRenderScale))
        {
            renderResolutionScale = Math.Min(renderResolutionScale + 10, 100);
        }

        if (Input.GetKeyDown(decreaseRenderScale))
        {
            renderResolutionScale = Math.Max(renderResolutionScale - 10, 10);
        }
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
                + ";"
                + headTrackingStateToString()
                + ";"
                + lastHeadPosition.x
                + ";"
                + lastHeadPosition.y
                + ";"
                + lastHeadPosition.z
                + ";";

            headPosition.headDetected = headDetected;
            headPosition.imagePosIsValid = imagePosIsValid;

            int millimeterToMeter = 1000;

            Vector3 headPos = new Vector3(
                (float)-worldPosX / millimeterToMeter,
                (float)worldPosY / millimeterToMeter,
                (float)-worldPosZ / millimeterToMeter
            );

            headPosition.imagePosX = imagePosX / (int)millimeterToMeter * (int)sceneScaleFactor;
            headPosition.imagePosY = imagePosY / (int)millimeterToMeter * (int)sceneScaleFactor;
            headPosition.worldPosX = headPos.x * sceneScaleFactor;
            headPosition.worldPosY = headPos.y * sceneScaleFactor;
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
                            -filteredPositionX / millimeterToMeter * sceneScaleFactor;
                        filteredHeadPosition.worldPosY =
                            filteredPositionY / millimeterToMeter * sceneScaleFactor;
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

            headPoitionLog.Enqueue(logEntry);
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
            messageText = messageText + "\n" + remedy;
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

        Vector3 position;
        // draw eye separation
        Gizmos.color = new Color(0, 0, 1, 0.75F);
        for (int i = 0; i < cameraCount; i++)
        {
            float cameraOffset = calculateCameraOffset(i, eyeSeparation, cameraCount);
            position = transform.position + transform.right * cameraOffset;
            Gizmos.DrawSphere(position, 0.3f * gizmoSize * sceneScaleFactor);
        }

        // draw focus distance
        Gizmos.color = new Color(0, 1, 0, 0.75F);
        position = transform.position + transform.forward * focusDistance;
        Gizmos.DrawSphere(position, 0.5f * gizmoSize * sceneScaleFactor);
    }
#endif

    private string headTrackingStateToString()
    {
        switch (prevHeadTrackingState)
        {
            case HeadTrackingState.TRACKING:
                return "TRACKING";
            case HeadTrackingState.LOST:
                return "LOST";
            case HeadTrackingState.LOSTGRACEPERIOD:
                return "LOSTGRACEPERIOD";
            case HeadTrackingState.TRANSITIONTOLOST:
                return "TRANSITIONTOLOST";
            case HeadTrackingState.TRANSITIONTOTRACKING:
                return "TRANSITIONTOTRACKING";
            default:
                return "UNKNOWN";
        }
    }
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

        float offset = currentView * targetEyeSeparation * sceneScaleFactor;

        // when the camera count is even, one camera is placed half the eye separation to the right of the center
        // same for the other to the left
        // therefore we need to add the correction term to the offset to get the correct position
        if (tmpCameraCount % 2 == 0)
        {
            // subtract half of the eye separation to get the correct offset
            float correctionTerm = targetEyeSeparation * sceneScaleFactor / 2;
            if (currentView > 0)
            {
                correctionTerm *= -1;
            }
            return (offset + correctionTerm) * -1;
        }

        return offset * -1;
    }

    /// <summary>
    /// Returns false if all values of the position filter are set to zero.
    /// </summary>
    /// <returns></returns>
    private bool usePositionFiltering()
    {
        return headPositionFilter.x != 0 || headPositionFilter.y != 0 || headPositionFilter.z != 0;
    }

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
            DefaultCalibrationProvider defaultCalibrationProvider =
                DefaultCalibrationProvider.getFromURI(
                    uri,
                    (DefaultCalibrationProvider provider) =>
                    {
                        lock (shaderLock)
                        {
                            shaderParameters = provider.getDefaultShaderParameters();
                        }
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
            lock (shaderLock)
            {
                DefaultCalibrationProvider defaultCalibrationProvider =
                    DefaultCalibrationProvider.getFromConfigFile(filePath);
                shaderParameters = defaultCalibrationProvider.getDefaultShaderParameters();
            }
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
            lock (shaderLock)
            {
                DefaultCalibrationProvider defaultCalibrationProvider =
                    DefaultCalibrationProvider.getFromString(iniFile);
                shaderParameters = defaultCalibrationProvider.getDefaultShaderParameters();
            }
            updateShaderParameters();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update shader parameters from json: " + e.Message);
        }
    }
}
