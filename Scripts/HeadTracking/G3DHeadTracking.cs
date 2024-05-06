using System;
using System.Runtime.InteropServices;
using UnityEngine;

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

public class G3DHeadTracking
    : MonoBehaviour,
        ITNewHeadPositionCallback,
        ITNewShaderParametersCallback,
        ITNewErrorMessageCallback
{
    public string calibrationPath = "";
    public string configPath = "";
    public string configFileName = "";

    public bool registerCallbacks = true;

    public bool useHimaxD2XXDevices = true;
    public bool usePmdFlexxDevices = true;

    private LibInterface libInterface;

    private HeadPosition headPosition;

    private static object headPosLock = new object();

    // Start is called before the first frame update
    void Start()
    {
        libInterface = LibInterface.Instance;
        libInterface.init(
            calibrationPath,
            configPath,
            configFileName,
            true,
            useHimaxD2XXDevices,
            usePmdFlexxDevices
        );

        if (registerCallbacks)
        {
            libInterface.registerHeadPositionChangedCallback(this);
            libInterface.registerShaderParametersChangedCallback(this);
            libInterface.registerMessageCallback(this);
        }

        headPosition = new HeadPosition();
        headPosition.headDetected = false;
        headPosition.imagePosIsValid = false;
        headPosition.imagePosX = 0;
        headPosition.imagePosY = 0;
        headPosition.worldPosX = 0.0;
        headPosition.worldPosY = 0.0;
        headPosition.worldPosZ = 0.0;
    }

    // Update is called once per frame
    void Update() { }

    void OnDestroy()
    {
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        if (registerCallbacks)
        {
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
        }

        // manual garbage collection here to force library destruction when unity "playback" is stopped
        // TODO Remove this
        libInterface = null;
    }

    public HeadPosition getHeadPosition()
    {
        lock (headPosLock)
        {
            return headPosition;
        }
    }

    public void toggleHeadTrackingStatus()
    {
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
            headPosition.imagePosX = imagePosX;
            headPosition.imagePosY = imagePosY;
            headPosition.worldPosX = worldPosX;
            headPosition.worldPosY = worldPosY;
            headPosition.worldPosZ = worldPosZ;
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
        Debug.Log("Shader parameters: " + shaderParameters);
    }

    private string formatErrorMessage(string caption, string cause, string remedy)
    {
        string messageText = caption + "\n\n" + cause;

        if (String.IsNullOrEmpty(remedy) == false)
        {
            messageText = messageText + "\n\n" + remedy;
        }

        return messageText;
    }
}
