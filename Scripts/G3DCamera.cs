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

    public bool useMultiview = false;

    [Tooltip(
        "The distance between the two eyes in meters. This value is used to calculate the stereo effect."
    )]
    [Range(0.00001f, 2.0f)]
    public float eyeSeparation = 0.065f;

    public bool useFocusDistance = false;

    [Min(0.0000001f)]
    public float focusDistance = 2.3f;

    private const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public static string CAMERA_NAME_PREFIX = "g3dcam_";

    [Range(1.0f, 100.0f)]
    public float resolution = 100.0f;

    public float headMovementScaleFactor = 1000.0f;
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
    public KeyCode toggleHeadTrackingKey = KeyCode.Space;
    public KeyCode toggleAutostereo = KeyCode.A;
    public KeyCode shiftViewLeftKey = KeyCode.LeftArrow;
    public KeyCode shiftViewRightKey = KeyCode.RightArrow;

    public bool showTestFrame = false;
    public bool showTestStripes = false;

    public bool enableDioramaEffect = false;

    [Tooltip(
        "If set to true, the gizmos for the focus distance (green) and eye separation (blue) will be shown."
    )]
    public bool showGizmos = true;

    [Range(0.1f, 1.0f)]
    public float gizmoSize = 0.2f;

    public bool renderAutostereo = true;

    #endregion

    #region Private variables

    private LibInterface libInterface;

    /// <summary>
    /// This struct is used to store the current head position.
    /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
    /// NEVER use headPosition directly.
    /// </summary>
    private HeadPosition headPosition;
    private HeadPosition smoothedHeadPosition;

    private static object headPosLock = new object();
    private static object shaderLock = new object();

    private Camera mainCamera;
    private List<Camera> cameras = null;
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

    private Vector3 focusPlaneCenterAtStart = new Vector3();

    #endregion

    // TODO Handle viewport resizing/ moving

    #region Initialization
    void Start()
    {
        mainCamera = GetComponent<Camera>();

        //initialize cameras

        cameraParent = new GameObject("g3dcams");
        cameraParent.transform.parent = transform;
        cameraParent.transform.localPosition = new Vector3(0, 0, 0);
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

        focusPlaneCenterAtStart = mainCamera.transform.position + mainCamera.transform.forward * focusDistance;
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
        headPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = 0.0
        };
        smoothedHeadPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = 0.0
        };
        shaderParameters = libInterface.getCurrentShaderParameters();
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
    }
#endif

    #endregion

    #region Updates
    void Update()
    {
        updateCameras();
        updateShaderParameters();

        handleKeyPresses();

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

    public bool test = false;

    // TODO call this every time the head position changed callback fires
    void updateCameras()
    {
        HeadPosition headPosition = getSmoothedHeadPosition();
        float horizontalOffset = 0.0f;
        float verticalOffset = 0.0f;
        if (headPosition.headDetected && enableDioramaEffect)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
            );

            cameraParent.transform.localPosition = headPositionWorld;

            horizontalOffset = headPositionWorld.x;
            verticalOffset = headPositionWorld.y;

        }
        else
        {
            cameraParent.transform.localPosition = new Vector3(0, 0, 0);
        }

        float currentFocusDistance = Vector3.Distance(focusPlaneCenterAtStart, cameraParent.transform.position);

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
                float shearFactor = -(localCameraOffset + horizontalOffset) / currentFocusDistance;
                float vertObl = -verticalOffset / currentFocusDistance;

                // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
                Matrix4x4 shearMatrix = Matrix4x4.identity;
                shearMatrix[0, 2] = shearFactor;
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
            );
            Texture tex = renderTextures[i];
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
        if (Input.GetKeyDown(KeyCode.D))
        {
            showTestFrame = !showTestFrame;
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            enableDioramaEffect = !enableDioramaEffect;
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            eyeSeparation -= 0.01f;
            if (eyeSeparation < 0.0f)
                eyeSeparation = 0.0f;
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            eyeSeparation += 0.01f;
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
    public HeadPosition getSmoothedHeadPosition()
    {
        lock (headPosLock)
        {
            return smoothedHeadPosition;
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

            Vector3 headPos = new Vector3(
                (float)-worldPosX / headMovementScaleFactor,
                (float)worldPosY / headMovementScaleFactor,
                (float)-worldPosZ / headMovementScaleFactor
            );

            headPositions.PushFront(headPos);

            headPosition.imagePosX = imagePosX / (int)headMovementScaleFactor;
            headPosition.imagePosY = imagePosY / (int)headMovementScaleFactor;
            headPosition.worldPosX = headPos.x;
            headPosition.worldPosY = headPos.y;
            headPosition.worldPosZ = headPos.z;

            if (headDetected)
            {
                Vector3 smoothedHeadPosVec = new Vector3(0, 0, 0);
                int divisor = 0;
                int factor = 3;
                for (int i = 0; i < headPositions.Size; i++)
                {
                    smoothedHeadPosVec += headPositions[i] * factor;
                    divisor += factor;
                    factor--;
                }
                smoothedHeadPosVec /= divisor;
                smoothedHeadPosition.worldPosX = smoothedHeadPosVec.x;
                smoothedHeadPosition.worldPosY = smoothedHeadPosVec.y;
                smoothedHeadPosition.worldPosZ = smoothedHeadPosVec.z;
            }
            smoothedHeadPosition.headDetected = headDetected;
            smoothedHeadPosition.imagePosIsValid = imagePosIsValid;
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
}
