using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Codice.Client.Common.GameUI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

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
public class G3DHeadTracking
    : MonoBehaviour,
        ITNewHeadPositionCallback,
        ITNewShaderParametersCallback,
        ITNewErrorMessageCallback
{
    #region Callibration
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
    #endregion

    #region 3D Effect settings
    [Header("3D Effect settings")]
    [Tooltip(
        "The amount of used cameras. The maximum amount of cameras is 16. Two corresponds to a stereo setup."
    )]
    [Range(1, 16)]
    private int cameraCount = 2;

    [Tooltip(
        "The distance between the two eyes in meters. This value is used to calculate the stereo effect."
    )]
    [Range(0.00001f, 0.1f)]
    public float eyeSeparation = 0.065f;

    private const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public const string CAMERA_NAME_PREFIX = "g3dcam_";

    [Range(1.0f, 100.0f)]
    public float resolution = 100.0f;
    #endregion

    #region Device settings
    [Header("Device settings")]
    public bool useHimaxD2XXDevices = true;
    public bool usePmdFlexxDevices = true;
    public int scaleCorrectionFactor = -1000;
    #endregion

    #region Debugging
    [Header("Debugging")]
    [Tooltip("If set to true, the library will print debug messages to the console.")]
    public bool debugMessages = false;
    public KeyCode toggleHeadTrackingKey = KeyCode.Space;
    public KeyCode shiftViewLeftKey = KeyCode.LeftArrow;
    public KeyCode shiftViewRightKey = KeyCode.RightArrow;
    #endregion

    private LibInterface libInterface;

    /// <summary>
    /// This struct is used to store the current head position.
    /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
    /// NEVER use headPosition directly.
    /// </summary>
    private HeadPosition headPosition;

    private static object headPosLock = new object();
    private static object shaderLock = new object();

    private Camera mainCamera;
    private Vector3 cameraStartPosition;
    private List<Camera> cameras = null;
    private GameObject cameraParent = null;

    private static Material material;
    private int[] id_View = new int[MAX_CAMERAS];
    private ShaderHandles shaderHandles;
    private G3DShaderParameters shaderParameters;

    // TODO Handle viewport resizing/ moving

    void Start()
    {
        mainCamera = GetComponent<Camera>();

        cameraStartPosition = transform.position;

        //initialize cameras

        cameraParent = new GameObject("g3dcams");
        cameraParent.transform.parent = transform;

        cameras = new List<Camera>();
        for (int i = 0; i < MAX_CAMERAS; i++)
        {
            cameras.Add(new GameObject(CAMERA_NAME_PREFIX + i).AddComponent<Camera>());
            cameras[i].transform.SetParent(cameraParent.transform, true);
            cameras[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < MAX_CAMERAS; i++)
            id_View[i] = Shader.PropertyToID("_View_" + i);

        shaderHandles = new ShaderHandles()
        {
            leftViewportPosition = Shader.PropertyToID("v_pos_x"),
            bottomViewportPosition = Shader.PropertyToID("v_pos_y"),
            screenWidth = Shader.PropertyToID("s_width"),
            screenHeight = Shader.PropertyToID("s_height"),
            nativeViewCount = Shader.PropertyToID("viewcount"),
            angleRatioNumerator = Shader.PropertyToID("zwinkel"),
            angleRatioDenominator = Shader.PropertyToID("nwinkel"),
            leftLensOrientation = Shader.PropertyToID("isleft"),
            // BGRPixelLayout = Shader.PropertyToID("windowPosition"),
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
        updateScreenViewportProperties();
        updateCameras();
        reinitializeShader();
    }

    void OnApplicationQuit()
    {
        deinitLibrary();
    }

    private void initLibrary()
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
        shaderParameters = libInterface.getCurrentShaderParameters();
    }

    private void updateScreenViewportProperties()
    {
        // This is the size of the entire monitor screen
        libInterface.setScreenSize(Screen.width, Screen.height);
        // this revers to the window in which the 3D effect is rendered (including eg windows top window menu)
        libInterface.setWindowPosition(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y);
        libInterface.setWindowSize(
            Screen.mainWindowDisplayInfo.width,
            Screen.mainWindowDisplayInfo.height
        );

        // This revers to the actual viewport in which the 3D effect is rendered
        RectInt vieportRect = Screen.mainWindowDisplayInfo.workArea;
        libInterface.setViewportSize(vieportRect.width, vieportRect.height);
        libInterface.setViewportOffset(vieportRect.x, vieportRect.y);

        libInterface.startHeadTracking();
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
        }
    }

    [ContextMenu("Toggle head tracking status")]
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

    void Update()
    {
        HeadPosition headPosition = getHeadPosition();
        if (headPosition.headDetected)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
            );

            transform.position = cameraStartPosition + headPositionWorld;
        }

        // updateCameras();
        updateShaderParameters();

        handleKeyPresses();
    }

    // TODO call this every time the head position changed callback fires
    void updateCameras()
    {
        //put camera host gameobject in easy-to-handle situation and save its position/rotation for resetting after "parenting" the child cameras
        Vector3 savedCameraPosition = transform.position;
        Quaternion savedCameraRotation = transform.rotation;
        cameraParent.transform.position = new Vector3(0, 0, 0);
        cameraParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

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
            camera.transform.position = cameraParent.transform.position;
            camera.transform.rotation = cameraParent.transform.rotation;

            float offset = currentView * eyeSeparation / 2;

            Matrix4x4 tempMatrix = camera.projectionMatrix; // original matrix
            tempMatrix[0, 2] = tempMatrix[0, 2] + offset; // apply offset

            // apply new projection matrix
            camera.projectionMatrix = tempMatrix;

            camera.transform.localPosition = new Vector3(offset, 0, 0);

            camera.gameObject.SetActive(true);
        }

        //reset parent position/rotation
        cameraParent.transform.position = savedCameraPosition;
        cameraParent.transform.rotation = savedCameraRotation;

        //disable all the other cameras, we are not using them with this cameracount
        for (int i = cameraCount; i < MAX_CAMERAS; i++)
            cameras[i].gameObject.SetActive(false);

        updateShaderViews();
    }

    private void reinitializeShader()
    {
        material = new Material(Shader.Find("G3D/GLSLHeadTracking"));
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

        //set only those we need
        for (int i = 0; i < cameraCount; i++)
        {
            Texture tex;
            tex = cameras[i].targetTexture = new RenderTexture(
                (int)(Screen.width * resolution / 100),
                (int)(Screen.height * resolution / 100),
                0
            );

            material.SetTexture(id_View[i], tex);
        }
    }

    private void handleKeyPresses()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            toggleHeadTrackingStatus();
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            libInterface.shiftViewToLeft();
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            libInterface.shiftViewToRight();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        // This is where the material and shader are applied to the camera image.
        //legacy support (no URP or HDRP)
        if (material == null)
            Graphics.Blit(source, destination);
        else
            Graphics.Blit(source, destination, material);
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
            headPosition.imagePosX = imagePosX / scaleCorrectionFactor;
            headPosition.imagePosY = imagePosY / scaleCorrectionFactor;
            headPosition.worldPosX = worldPosX / scaleCorrectionFactor;
            headPosition.worldPosY = worldPosY / scaleCorrectionFactor;
            headPosition.worldPosZ = worldPosZ / scaleCorrectionFactor;
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
}
