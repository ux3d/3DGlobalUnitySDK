using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class G3DHeadTracking : MonoBehaviour
{
    public string calibrationPath  = "";
    public string configPath = "";
    public string configFileName = "";

    private IntPtr headTrackingDevice = new IntPtr();
    
    // Start is called before the first frame update
    void Start()
    {
        int result = LibInterface.initLibrary();
        Debug.Log("G3D library initialization result: " + result);

        int deviceCount = -1;
        result = LibInterface.getHeadTrackingDeviceCount(out deviceCount);
        Debug.Log("G3D library getHeadTrackingDeviceCount result: " + result);

        byte [] calibPathBytes = System.Text.Encoding.ASCII.GetBytes(calibrationPath);
        result = LibrInterface.setCalibrationPath(calibPathBytes);
        Debug.Log("G3D library setCalibrationPath result: " + result);

        byte [] configPathBytes = System.Text.Encoding.ASCII.GetBytes(configPath);
        result = LibInterface.setConfigPath(configPathBytes);
        Debug.Log("G3D library setConfigPath result: " + result);

        byte [] configFileNameBytes = System.Text.Encoding.ASCII.GetBytes(configFileName);
        result = LibInterface.setConfigFileName(configFileNameBytes);
        Debug.Log("G3D library setConfigFileName result: " + result);

        // int deviceCount = 0;
        // int result = getHeadTrackingDeviceCount(deviceCount);
        // Debug.Log("Result: " + result);
        // Debug.Log("Device Count: " + deviceCount);
        // IntPtr ptr =  new IntPtr();
        // result = getHeadTrackingDeviceName(0, ptr);
        // Debug.Log("Result: " + result);

        // string deviceName = Marshal.PtrToStringAnsi(ptr);
        // Debug.Log("Device Name: " + deviceName);

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        int result = LibInterface.deinitLibrary();
        Debug.Log("deinitLibrary Result: " + result);
    }
}
