using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;

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

    [Range(0.00001f, 0.1f)]
    public float eyeSeparation = 0.065f;

    [Range(0f, 1f)]
    public float stereo_depth = 0.0f;

    [Range(-5f, 5f)]
    public float stereo_plane = 5f;
    private const int MAX_CAMERAS = 2; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public const string CAMERA_NAME_PREFIX = "g3dcam_";
    #endregion

    #region Device settings
    [Header("Device settings")]
    public bool useHimaxD2XXDevices = true;
    public bool usePmdFlexxDevices = true;
    public int scaleCorrectionFactor = 500;
    #endregion

    #region Debugging
    [Header("Debugging")]
    [Tooltip("If set to true, the library will print debug messages to the console.")]
    public bool debugMessages = false;
    #endregion

    private LibInterface libInterface;

    /// <summary>
    /// This struct is used to store the current head position.
    /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
    /// NEVER use headPosition directly.
    /// </summary>
    private HeadPosition headPosition;

    private static object headPosLock = new object();

    private Camera mainCamera;
    private Vector3 cameraStartPosition;
    private List<Camera> cameras = null;
    private GameObject cameraParent = null;

    private static Material material;

    void Start()
    {
        initLibrary();

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

        // TODO Initialize the material
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
    }

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
            float currentView = -cameraCount / 2 + i;

            //copy any changes to the main camera
            camera.fieldOfView = mainCamera.fieldOfView;
            camera.farClipPlane = mainCamera.farClipPlane;
            camera.nearClipPlane = mainCamera.nearClipPlane;
            camera.projectionMatrix = mainCamera.projectionMatrix;
            camera.transform.position = cameraParent.transform.position;
            camera.transform.rotation = cameraParent.transform.rotation;

            int ScreenWidth = Screen.currentResolution.width;

            // eye distance
            float EyeDistance = eyeSeparation * 100;

            // calculate eye distance in pixel
            // TODO this was the original code here. Handle correctly (see mi.MonitorWidth in the original code)
            // int StereoViewIPDOffset =
            //     (int)currentView * (int)(EyeDistance / mi.MonitorWidth * ScreenWidth / 2); // offset for left/right eye in pixel (eye distance (in mm) / monitor width (in mm) * monitor width (in pixel) / 2)
            int StereoViewIPDOffset = (int)currentView * (int)(EyeDistance / ScreenWidth / 2); // offset for left/right eye in pixel (eye distance (in mm) / monitor width (in mm) * monitor width (in pixel) / 2)

            // get view size
            int ViewWidth = camera.pixelWidth;

            // calculate offset for projection matrix
            float ProjOffset = StereoViewIPDOffset * stereo_depth / ViewWidth; // real offset (pixel offset * factor / view size (fullscreen here))

            // calculate adjusted projection matrix
            Matrix4x4 tempMatrix = camera.projectionMatrix; // original matrix
            tempMatrix[0, 2] = tempMatrix[0, 2] + ProjOffset; // apply offset

            // calculate offset for view matrix
            float ViewOffset = 0.0f;
            float FC = tempMatrix[2, 2];
            float FD = tempMatrix[2, 3];
            if ((Math.Abs(tempMatrix[0, 0]) > 1E-3) && (Math.Abs(FC - 1) > 1E-4)) // projection matrix is valid and calculation possible
            {
                float Near = ((FC + 1) / (FC - 1) - 1) / 2 * FD; // near of current projection matrix
                float DataWidth = 2 * Near / tempMatrix[0, 0]; // width
                ViewOffset =
                    (float)StereoViewIPDOffset
                    / (float)ViewWidth
                    * DataWidth
                    * (float)(stereo_depth - (stereo_plane));
            }

            // apply new projection matrix
            camera.projectionMatrix = tempMatrix;

            camera.transform.localPosition = new Vector3(ViewOffset, 0, 0);

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
        for (int i = 0; i < cameracount; i++)
        {
            Texture tex;
            if (testviews_active)
                tex = testviews[i % 16];
            else if (testcolors_active)
                tex = testcolors[i % 4];
            else
                tex = cameras[i].targetTexture = new RenderTexture(
                    (int)(Screen.width * resolution / 100),
                    (int)(Screen.height * resolution / 100),
                    0
                );

            material.SetTexture(id_View[i], tex);
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

    void ITNewShaderParametersCallback.NewShaderParametersCallback(
        G3DShaderParameters shaderParameters
    )
    {
        Debug.Log("New shader parameters received");
        // Debug.Log("Shader parameters: " + shaderParameters.ToString());
    }

    private string formatErrorMessage(string caption, string cause, string remedy)
    {
        string messageText = caption + ": " + cause;

        if (String.IsNullOrEmpty(remedy) == false)
        {
            messageText = messageText + "\n" + remedy;
        }

        return messageText;
    }
    #endregion
}
