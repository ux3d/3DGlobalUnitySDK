using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.EditorTools;
#endif

#if HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if URP
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

[RequireComponent(typeof(Camera))]
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
    #endregion

    #region 3D Effect settings
    [Header("3D Effect settings")]
    [Tooltip(
        "The amount of used cameras. The maximum amount of cameras is 16. Two corresponds to a stereo setup."
    )]
    [Range(1, 16)]
    public int cameraCount = 2;

    public bool enableDioramaEffect = false;

    public bool useMultiview = false;

    [Tooltip(
        "The distance between the two eyes in meters. This value is used to calculate the stereo effect."
    )]
    [Range(0.00001f, 2.0f)]
    public float eyeSeparation = 0.065f;

    public bool useFocusDistance = false;

    [Min(0.0000001f)]
    public float focusDistance = 0.7f;

    [Tooltip(
        "If set to true, the head position will only be updated if the head is closer than the maxHeadDistance."
    )]
    public bool useMaxHeadDistance = true;

    [Tooltip(
        "The maximum distance between the head and the focus plane (i.e. display). If the head is further away, the head position will not be updated."
    )]
    public float maxHeadDistance = 0.85f;

    [Tooltip("Time it takes till the reset animation starts in seconds.")]
    public float headLostTimeoutInSec = 3.0f;

    [Tooltip("Reset animation duratuion in seconds.")]
    public float transitionDuration = 1.5f;

    private const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public static string CAMERA_NAME_PREFIX = "g3dcam_";

    [Range(1.0f, 100.0f)]
    public float resolution = 100.0f;

    public float headMovementScaleFactor = 1000.0f;

    [Tooltip(
        "Smoothes the head position (Size of the filter kernel). Not filtering is applied, if set to all zeros. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!"
    )]
    public Vector3Int headPositionFilter = new Vector3Int(0, 0, 0);

    private Vector3 lastHeadPosition = new Vector3(0, 0, 0);
    #endregion

    #region Device settings
    [Header("Device settings")]
    public bool useHimaxD2XXDevices = true;
    public bool usePmdFlexxDevices = true;
    #endregion

    #region Debugging
    [Header("Debugging")]
    [Tooltip("If set to true, the library will print debug messages to the console.")]
    public bool debugMessages = false;
    public bool showTestFrame = false;
    public bool showTestStripes = false;

    [Tooltip(
        "If set to true, the gizmos for the focus distance (green) and eye separation (blue) will be shown."
    )]
    public bool showGizmos = true;

    [Range(0.01f, 1.0f)]
    public float gizmoSize = 0.2f;

    public bool renderAutostereo = true;

    #endregion

    #region Keys
    [Header("Keys")]
    [Tooltip("If set to true, the library will react to certain keyboard keys.")]
    public bool enableKeys = true;
    public KeyCode toggleHeadTrackingKey = KeyCode.Space;
    public KeyCode toggleAutostereo = KeyCode.A;
    public KeyCode shiftViewLeftKey = KeyCode.LeftArrow;
    public KeyCode shiftViewRightKey = KeyCode.RightArrow;

    [Tooltip("Shows a red/green test frame.")]
    public KeyCode toggleTestFrameKey = KeyCode.D;
    public KeyCode toggleDioramaEffectKey = KeyCode.H;
    public KeyCode decreaseEyeSeparationKey = KeyCode.K;
    public KeyCode increaseEyeSeparationKey = KeyCode.L;
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

    private Material material;
#if HDRP
    private G3DHDRPCustomPass customPass;
#endif
#if URP
    private G3DUrpScriptableRenderPass customPass;
#endif

    private int[] id_View = new int[MAX_CAMERAS];
    private ShaderHandles shaderHandles;
    private G3DShaderParameters shaderParameters;

    private Vector2Int cachedWindowPosition;
    private Vector2Int cachedWindowSize;
    private int cachedCameraCount;

    // half of the width of field of view at start at focus distance
    private float halfCameraWidthAtStart = 1.0f;

    private CircularBuffer<Vector3> headPositions = new CircularBuffer<Vector3>(3);

    // start with true to prevent bugs
    private bool headDetectionPrevFrame = true;
    private float headLostTimer = 0.0f;
    private bool headLost = false;

    private bool isInTransition = false;
    private float lastHeadDistance = 0;
    private float transitionTime = 0.0f;
    private bool headWasToFarToLong = false;

    #endregion

    // TODO Handle viewport resizing/ moving

    #region Initialization
    void Start()
    {
        mainCamera = GetComponent<Camera>();
        mainCamera.cullingMask = 0; //disable rendering of the main camera
        mainCamera.clearFlags = CameraClearFlags.Color;

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
        }

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
            shaderParameters = libInterface.getCurrentShaderParameters();
        }
        updateShaderParameters();
        libInterface.startHeadTracking();

#if HDRP
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

#if URP
        customPass = new G3DUrpScriptableRenderPass(material);
#endif

        halfCameraWidthAtStart =
            Mathf.Tan(mainCamera.fieldOfView * Mathf.Deg2Rad / 2) * focusDistance;

        updateCameras();

        // This has to be done after the cameras are updated
        cachedWindowPosition = new Vector2Int(
            Screen.mainWindowPosition.x,
            Screen.mainWindowPosition.y
        );
        cachedWindowSize = new Vector2Int(Screen.width, Screen.height);
        cachedCameraCount = cameraCount;
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
            usePmdFlexxDevices
        );

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
        shaderParameters = libInterface.getCurrentShaderParameters();

        if (usePositionFiltering())
        {
            libInterface.initializePositionFilter(
                headPositionFilter.x,
                headPositionFilter.y,
                headPositionFilter.z
            );
        }
    }

    private void deinitLibrary()
    {
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        libInterface.stopHeadTracking();

        try
        {
            libInterface.unregisterHeadPositionChangedCallback(this);
            libInterface.unregisterShaderParametersChangedCallback(this);
            libInterface.unregisterMessageCallback(this);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }

        libInterface.deinit();
    }

    private void reinitializeShader()
    {
        if (useMultiview)
        {
            material = new Material(Shader.Find("G3D/AutostereoMultiview"));
        }
        else
        {
            material = new Material(Shader.Find("G3D/Autostereo"));
        }
    }

#if URP
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
        updateCameras();
        updateShaderParameters();

        if (enableKeys)
        {
            handleKeyPresses();
        }

        if (windowResized() || windowMoved())
        {
            updateScreenViewportProperties();
        }

#if HDRP || URP
        customPass.renderAutostereo = renderAutostereo;
#endif
    }

    private void updateScreenViewportProperties()
    {
        DisplayInfo mainDisplayInfo = Screen.mainWindowDisplayInfo;
        // This is the size of the entire monitor screen
        libInterface.setScreenSize(mainDisplayInfo.width, mainDisplayInfo.height);

        // this revers to the window in which the 3D effect is rendered (including eg windows top window menu)
        libInterface.setWindowSize(Screen.width, Screen.height);
        libInterface.setWindowPosition(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y);

        // This revers to the actual viewport in which the 3D effect is rendered
        libInterface.setViewportSize(Screen.width, Screen.height);
        libInterface.setViewportOffset(0, 0);

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
            material?.SetInt(shaderHandles.showTestFrame, shaderParameters.showTestFrame);
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

            material?.SetInt(Shader.PropertyToID("cameraCount"), cameraCount - 1);
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

    // TODO call this every time the head position changed callback fires
    void updateCameras()
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

        Vector3 targetPosition;
        // head detected
        if (headPosition.headDetected && enableDioramaEffect)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
            );

            if (headDetectionPrevFrame == false)
            {
                transitionTime = 0.0f;
                isInTransition = true;
            }

            targetPosition = headPositionWorld;
            if (headPositionWorld.magnitude > maxHeadDistance)
            {
                bool headOutsideForTooLong = Time.time - headLostTimer > headLostTimeoutInSec;
                if (lastHeadDistance <= maxHeadDistance)
                {
                    transitionTime = 0.0f;
                    headLostTimer = Time.time;
                }
                targetPosition = lastHeadPosition;

                // start transition if head outside for to long
                if (headOutsideForTooLong)
                {
                    isInTransition = true;
                    targetPosition = new Vector3(0, 0, -focusDistance);
                    headWasToFarToLong = true;
                }
            }

            if (headWasToFarToLong)
            {
                if (headPositionWorld.magnitude < maxHeadDistance)
                {
                    headWasToFarToLong = false;
                    // transition back to head position if head reaquired after it was to long outside
                    if (lastHeadDistance > maxHeadDistance)
                    {
                        transitionTime = 0.0f;
                        headLostTimer = Time.time;
                        isInTransition = true;
                    }
                }
            }

            lastHeadDistance = headPositionWorld.magnitude;
            headLost = false;
        }
        else
        {
            headLost = true;

            // always set head to last known position (it gests set to interpolation of head lost to long)
            targetPosition = lastHeadPosition;

            bool headLostForTooLong = headLost && Time.time - headLostTimer > headLostTimeoutInSec;
            // reset transition time the first time the detection is lost
            if (headDetectionPrevFrame == true)
            {
                transitionTime = 0.0f;
                headLostTimer = Time.time;
            }
            // start transition
            if (headLostForTooLong)
            {
                isInTransition = true;
                targetPosition = new Vector3(0, 0, -focusDistance);
            }
        }

        if (isInTransition)
        {
            Debug.Log("Transitioning");
            float transitionPercentage = transitionTime / transitionDuration;
            transitionTime += Time.deltaTime;
            Vector3 interpolatedPosition = Vector3.Lerp(
                cameraParent.transform.localPosition,
                targetPosition,
                transitionPercentage
            );

            float distance = Vector3.Distance(interpolatedPosition, targetPosition);
            // only use interpolated position if we are not close enough to the target position
            if (distance > 0.0001f)
            {
                targetPosition = interpolatedPosition;
            }
            else
            {
                isInTransition = false;
            }

            if (transitionTime > transitionDuration)
            {
                isInTransition = false;
            }
        }

        cameraParent.transform.localPosition = targetPosition;
        float horizontalOffset = targetPosition.x;
        float verticalOffset = targetPosition.y;

        // store last known position data for tracking loss case
        lastHeadPosition = targetPosition;

        float currentFocusDistance = -cameraParent.transform.localPosition.z;

        mainCamera.fieldOfView =
            2 * Mathf.Atan(halfCameraWidthAtStart / currentFocusDistance) * Mathf.Rad2Deg;

        //calculate camera positions and matrices
        for (int i = 0; i < cameraCount; i++)
        {
            var camera = cameras[i];
            int currentView = -cameraCount / 2 + i;
            if (cameraCount % 2 == 0 && currentView >= 0)
            {
                currentView += 1;
            }

            // invert to keep the same order as in the shader
            currentView = -currentView;

            //copy any changes to the main camera
            camera.fieldOfView = mainCamera.fieldOfView;
            camera.farClipPlane = mainCamera.farClipPlane;
            camera.nearClipPlane = mainCamera.nearClipPlane;
            camera.projectionMatrix = mainCamera.projectionMatrix;
            camera.transform.localPosition = cameraParent.transform.localPosition;
            camera.transform.localRotation = cameraParent.transform.localRotation;

            float localCameraOffset = currentView * (eyeSeparation / 2);

            if (useFocusDistance)
            {
                // horizontal obliqueness
                float horizontalObl =
                    -(localCameraOffset + horizontalOffset) / currentFocusDistance;
                float vertObl = -verticalOffset / currentFocusDistance;

                // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
                Matrix4x4 shearMatrix = Matrix4x4.identity;
                shearMatrix[0, 2] = horizontalObl;
                shearMatrix[1, 2] = vertObl;
                // apply new projection matrix
                camera.projectionMatrix = camera.projectionMatrix * shearMatrix;
            }

            camera.transform.localPosition = new Vector3(localCameraOffset, 0, 0);

            camera.gameObject.SetActive(true);
        }

        //disable all the other cameras, we are not using them with this cameracount
        for (int i = cameraCount; i < MAX_CAMERAS; i++)
        {
            cameras[i].gameObject.SetActive(false);
        }

        if (cachedCameraCount != cameraCount)
        {
            cachedCameraCount = cameraCount;
            updateShaderViews();
        }

        // do this last
        headDetectionPrevFrame = headPosition.headDetected;
    }

    public void updateShaderViews()
    {
        if (material == null)
            return;
        if (cameras == null)
            return;

        //prevent any memory leaks
        for (int i = 0; i < MAX_CAMERAS; i++)
            cameras[i].targetTexture?.Release();

        RenderTexture[] renderTextures = new RenderTexture[cameraCount];

        //set only those we need
        for (int i = 0; i < cameraCount; i++)
        {
            renderTextures[i] = new RenderTexture(
                (int)(Screen.width * resolution / 100),
                (int)(Screen.height * resolution / 100),
                0
            )
            {
                format = RenderTextureFormat.ARGB32,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm
            };
            cameras[i].targetTexture = renderTextures[i];
            material.SetTexture("texture" + i, renderTextures[i], RenderTextureSubElement.Color);
        }
    }

    private void handleKeyPresses()
    {
        if (Input.GetKeyDown(toggleHeadTrackingKey))
        {
            toggleHeadTrackingStatus();
        }
        if (Input.GetKeyDown(shiftViewLeftKey))
        {
            libInterface.shiftViewToLeft();
        }
        if (Input.GetKeyDown(shiftViewRightKey))
        {
            libInterface.shiftViewToRight();
        }
        if (Input.GetKeyDown(toggleAutostereo))
        {
            renderAutostereo = !renderAutostereo;
        }
        if (Input.GetKeyDown(toggleTestFrameKey))
        {
            showTestFrame = !showTestFrame;
        }
        if (Input.GetKeyDown(toggleDioramaEffectKey))
        {
            enableDioramaEffect = !enableDioramaEffect;
        }
        if (Input.GetKeyDown(decreaseEyeSeparationKey))
        {
            eyeSeparation -= 0.005f;
            if (eyeSeparation < 0.0f)
                eyeSeparation = 0.0f;
        }
        if (Input.GetKeyDown(increaseEyeSeparationKey))
        {
            eyeSeparation += 0.005f;
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // This is where the material and shader are applied to the camera image.
        //legacy support (no URP or HDRP)
#if HDRP || URP
#else
        if (material == null || renderAutostereo == false)
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
            headPosition.headDetected = headDetected;
            headPosition.imagePosIsValid = imagePosIsValid;

            int millimeterToMeter = 1000;

            Vector3 headPos = new Vector3(
                (float)-worldPosX / millimeterToMeter,
                (float)worldPosY / millimeterToMeter,
                (float)-worldPosZ / millimeterToMeter
            );

            headPositions.PushFront(headPos);

            headPosition.imagePosX = imagePosX / (int)millimeterToMeter;
            headPosition.imagePosY = imagePosY / (int)millimeterToMeter;
            headPosition.worldPosX = headPos.x;
            headPosition.worldPosY = headPos.y;
            headPosition.worldPosZ = headPos.z;

            if (usePositionFiltering())
            {
                double filteredPositionX;
                double filteredPositionY;
                double filteredPositionZ;

                if (headDetected)
                {
                    libInterface.applyPositionFilter(
                        worldPosX,
                        worldPosY,
                        worldPosZ,
                        out filteredPositionX,
                        out filteredPositionY,
                        out filteredPositionZ
                    );

                    filteredHeadPosition.worldPosX = -filteredPositionX / millimeterToMeter;
                    filteredHeadPosition.worldPosY = filteredPositionY / millimeterToMeter;
                    filteredHeadPosition.worldPosZ = -filteredPositionZ / millimeterToMeter;
                }

                filteredHeadPosition.headDetected = headDetected;
                filteredHeadPosition.imagePosIsValid = imagePosIsValid;
            }
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

            if (showTestFrame)
            {
                this.shaderParameters.showTestFrame = 1;
            }
            else
            {
                this.shaderParameters.showTestFrame = 0;
            }
            if (showTestStripes)
            {
                this.shaderParameters.showTestStripe = 1;
            }
            else
            {
                this.shaderParameters.showTestStripe = 0;
            }
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
            int currentView = -cameraCount / 2 + i;
            if (cameraCount % 2 == 0 && currentView >= 0)
            {
                currentView += 1;
            }

            float cameraOffset = currentView * (eyeSeparation / 2);
            position = transform.position + transform.right * cameraOffset;
            Gizmos.DrawSphere(position, 0.3f * gizmoSize);
        }

        if (useFocusDistance)
        {
            // draw focus distance
            Gizmos.color = new Color(0, 1, 0, 0.75F);
            position = transform.position + transform.forward * focusDistance;
            Gizmos.DrawSphere(position, 0.5f * gizmoSize);
        }
    }
#endif
    #endregion

    /// <summary>
    /// Returns false if all values of the position filter are set to zero.
    /// </summary>
    /// <returns></returns>
    private bool usePositionFiltering()
    {
        return headPositionFilter.x != 0 || headPositionFilter.y != 0 || headPositionFilter.z != 0;
    }
}
