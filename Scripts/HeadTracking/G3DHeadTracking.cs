using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class G3DHeadTracking : MonoBehaviour, ITNewHeadPositionCallback
{
    public string calibrationPath = "";
    public string configPath = "";
    public string configFileName = "";

    private IntPtr headTrackingDevice = new IntPtr();

    private LibInterface libInterface;

    // Start is called before the first frame update
    void Start()
    {
        libInterface = new LibInterface(calibrationPath, configPath, configFileName, true);
        libInterface.registerHeadPositionChangedCallback(this, NewHeadPositionCallback);
    }

    // Update is called once per frame
    void Update() { }

    void OnDestroy()
    {
        libInterface.unregisterHeadPositionChangedCallback(this);
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
}
