using System;
using System.Runtime.InteropServices;
using UnityEngine;

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
    }

    // Update is called once per frame
    void Update() { }

    void OnDestroy()
    {
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

    public void NewHeadPositionCallback(
        bool headDetected,
        bool imagePosIsValid,
        int imagePosX,
        int imagePosY,
        double worldPosX,
        double worldPosY,
        double worldPosZ
    )
    {
        if (headDetected)
        {
            Debug.Log("Head detected");
            Debug.Log("Image position is valid: " + imagePosIsValid);
            Debug.Log("Image position X: " + imagePosX);
            Debug.Log("Image position Y: " + imagePosY);
            Debug.Log("World position X: " + worldPosX);
            Debug.Log("World position Y: " + worldPosY);
            Debug.Log("World position Z: " + worldPosZ);
        }
        else
        {
            Debug.Log("Head not detected");
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
        Debug.Log("Error message received");
        Debug.Log("Severity: " + severity);
        Debug.Log("Sender: " + sender);
        Debug.Log("Caption: " + caption);
        Debug.Log("Cause: " + cause);
        Debug.Log("Remedy: " + remedy);
    }

    void ITNewShaderParametersCallback.NewShaderParametersCallback(
        G3DShaderParameters shaderParameters
    )
    {
        Debug.Log("New shader parameters received");
        Debug.Log("Shader parameters: " + shaderParameters);
    }
}
