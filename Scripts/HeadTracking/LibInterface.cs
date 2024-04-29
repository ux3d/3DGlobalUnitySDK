using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class LibInterface
{
    public bool logToConsole = true;

    public LibInterface(
        string calibrationPath,
        string configPath,
        string configFileName,
        bool logToConsole = true
    )
    {
        this.logToConsole = logToConsole;

        initLibrary();

        setCalibrationPath(calibrationPath);
        setConfigPath(configPath);
        setConfigFileName(configFileName);
    }

    ~LibInterface()
    {
        deinitLibrary();
    }

    private void initLibrary()
    {
        int result = LibInterfaceCpp.initLibrary();
        if (logToConsole)
        {
            Debug.Log("G3D library initialization result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when initializing the library."
            );
        }
    }

    private void deinitLibrary()
    {
        int result = LibInterfaceCpp.initLibrary();
        if (logToConsole)
        {
            Debug.Log("G3D library initialization result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when initializing the library."
            );
        }
    }

    public void setCalibrationPath(string calibrationPath)
    {
        byte[] calibPathBytes = System.Text.Encoding.ASCII.GetBytes(calibrationPath);
        int result = LibInterfaceCpp.setCalibrationPath(calibPathBytes);
        if (logToConsole)
        {
            Debug.Log("G3D library: setCalibrationPath result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: calibration path already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the calibration path."
            );
        }
    }

    public void setConfigPath(string configPath)
    {
        byte[] configPathBytes = System.Text.Encoding.ASCII.GetBytes(configPath);
        int result = LibInterfaceCpp.setConfigPath(configPathBytes);
        if (logToConsole)
        {
            Debug.Log("G3D library: setConfigPath result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: config path already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the config path."
            );
        }
    }

    public void setConfigFileName(string configFileName)
    {
        byte[] configFileNameBytes = System.Text.Encoding.ASCII.GetBytes(configFileName);
        int result = LibInterfaceCpp.setConfigFileName(configFileNameBytes);
        if (logToConsole)
        {
            Debug.Log("G3D library: setConfigFileName result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: config file name already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the config file name."
            );
        }
    }

    // ------------------------------------------------

    public void registerHeadPositionChangedCallback(
        IntPtr listener,
        TNewHeadPositionCallback callback
    ) { }

    public void unregisterHeadPositionChangedCallback(
        IntPtr listener
    ) { // TODO
    }

    public void registerShaderParametersChangedCallback(
        IntPtr listener,
        TNewShaderParametersCallback callback
    ) { // TODO
    }

    public void unregisterShaderParametersChangedCallback(
        IntPtr listener
    ) { // TODO
    }

    public void registerMessageCallback(
        IntPtr listener,
        TNewErrorMessageCallback callback
    ) { // TODO
    }

    public void unregisterMessageCallback(
        IntPtr listener
    ) { // TODO
    }

    // ------------------------------------------------

    public void useHimaxD2XXDevices()
    {
        int result = LibInterfaceCpp.useHimaxD2XXDevices();
        if (logToConsole)
        {
            Debug.Log("G3D library: useHimaxD2XXDevices result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when using Himax D2XX devices."
            );
        }
    }

    public void usePmdFlexxDevices()
    {
        int result = LibInterfaceCpp.usePmdFlexxDevices();
        if (logToConsole)
        {
            Debug.Log("G3D library: usePmdFlexxDevices result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when using PMD Flexx devices."
            );
        }
    }

    public void initHeadTracking()
    {
        int result = LibInterfaceCpp.initHeadTracking();
        if (logToConsole)
        {
            Debug.Log("G3D library: initHeadTracking result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: head tracking already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: head tracking not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when initializing head tracking."
            );
        }
    }

    public void deinitHeadTracking()
    {
        int result = LibInterfaceCpp.deinitHeadTracking();
        if (logToConsole)
        {
            Debug.Log("G3D library: deinitHeadTracking result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: head tracking already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: head tracking not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when deinitializing head tracking."
            );
        }
    }

    // ------------------------------------------------

    public int getHeadTrackingDeviceCount()
    {
        int deviceCount;
        int result = LibInterfaceCpp.getHeadTrackingDeviceCount(out deviceCount);
        if (logToConsole)
        {
            Debug.Log("G3D library: getHeadTrackingDeviceCount result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: head tracking device count already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the head tracking device count."
            );
        }

        return deviceCount;
    }

    public string getHeadTrackingDeviceName(int deviceNumber)
    {
        IntPtr deviceName = new IntPtr();
        int result = LibInterfaceCpp.getHeadTrackingDeviceName(deviceNumber, out deviceName);
        if (logToConsole)
        {
            Debug.Log("G3D library: getHeadTrackingDeviceName result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: head tracking device name already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: the provided deviceNumber is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the head tracking device name."
            );
        }

        return Marshal.PtrToStringAnsi(deviceName);
    }

    public string getCurrentHeadTrackingDevice()
    {
        IntPtr device = new IntPtr();
        int result = LibInterfaceCpp.getCurrentHeadTrackingDevice(out device);
        if (logToConsole)
        {
            Debug.Log("G3D library: getCurrentHeadTrackingDevice result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: current head tracking device already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the current head tracking device."
            );
        }

        return Marshal.PtrToStringAnsi(device);
    }

    public void setCurrentHeadTrackingDevice(string device)
    {
        byte[] deviceNameBytes = System.Text.Encoding.ASCII.GetBytes(device);
        int result = LibInterfaceCpp.setCurrentHeadTrackingDevice(deviceNameBytes);
        if (logToConsole)
        {
            Debug.Log("G3D library: setCurrentHeadTrackingDevice result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: current head tracking device already initialized."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the current head tracking device."
            );
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>(horizontal resolution, vertical resolution)</returns>
    /// <exception cref="G3D_AlreadyInitializedException"></exception>
    /// <exception cref="G3D_NotInitializedException"></exception>
    /// <exception cref="G3D_IndexOutOfRangeException"></exception>
    /// <exception cref="Exception"></exception>
    public Tuple<int, int> getHeadTrackingDeviceResolution()
    {
        int horizontalResolution = -1;
        int verticalResolution = -1;
        int result = LibInterfaceCpp.getHeadTrackingDeviceResolution(
            out horizontalResolution,
            out verticalResolution
        );
        if (logToConsole)
        {
            Debug.Log("G3D library: getHeadTrackingDeviceResolution result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the head tracking device resolution."
            );
        }

        return new Tuple<int, int>(horizontalResolution, verticalResolution);
    }

    // ------------------------------------------------

    public int getFirstValidCalibrationMatrixCol()
    {
        int value = -1;
        int result = LibInterfaceCpp.getFirstValidCalibrationMatrixCol(out value);
        if (logToConsole)
        {
            Debug.Log("G3D library: getFirstValidCalibrationMatrixCol result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the first valid calibration matrix column."
            );
        }

        return value;
    }

    public int getLastValidCalibrationMatrixCol()
    {
        int value = -1;
        int result = LibInterfaceCpp.getLastValidCalibrationMatrixCol(out value);
        if (logToConsole)
        {
            Debug.Log("G3D library: getLastValidCalibrationMatrixCol result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the last valid calibration matrix column."
            );
        }

        return value;
    }

    public int getFirstValidCalibrationMatrixRow()
    {
        int value = -1;
        int result = LibInterfaceCpp.getFirstValidCalibrationMatrixRow(out value);
        if (logToConsole)
        {
            Debug.Log("G3D library: getFirstValidCalibrationMatrixRow result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the first valid calibration matrix row."
            );
        }

        return value;
    }

    public int getLastValidCalibrationMatrixRow()
    {
        int value = -1;
        int result = LibInterfaceCpp.getLastValidCalibrationMatrixRow(out value);
        if (logToConsole)
        {
            Debug.Log("G3D library: getLastValidCalibrationMatrixRow result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the last valid calibration matrix row."
            );
        }

        return value;
    }

    // ------------------------------------------------

    public void startHeadTracking()
    {
        int result = LibInterfaceCpp.startHeadTracking();
        if (logToConsole)
        {
            Debug.Log("G3D library: startHeadTracking result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already started.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: head tracking not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when starting head tracking."
            );
        }
    }

    public void stopHeadTracking()
    {
        int result = LibInterfaceCpp.stopHeadTracking();
        if (logToConsole)
        {
            Debug.Log("G3D library: stopHeadTracking result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: head tracking already stopped.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when stopping head tracking."
            );
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns>(has device, is tracking)</returns>
    /// <exception cref="G3D_AlreadyInitializedException"></exception>
    /// <exception cref="G3D_NotInitializedException"></exception>
    /// <exception cref="G3D_IndexOutOfRangeException"></exception>
    /// <exception cref="Exception"></exception>
    public Tuple<bool, bool> getHeadTrackingStatus()
    {
        bool hasDevice = false;
        bool isTracking = false;
        int result = LibInterfaceCpp.getHeadTrackingStatus(out hasDevice, out isTracking);
        if (logToConsole)
        {
            Debug.Log("G3D library: getHeadTrackingStatus result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the head tracking status."
            );
        }

        return new Tuple<bool, bool>(hasDevice, isTracking);
    }

    // ------------------------------------------------

    public G3DShaderParameters getCurrentShaderParameters()
    {
        G3DShaderParameters parameters = new G3DShaderParameters();
        int result = LibInterfaceCpp.getCurrentShaderParameters(out parameters);
        if (logToConsole)
        {
            Debug.Log("G3D library: getCurrentShaderParameters result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when getting the current shader parameters."
            );
        }

        return parameters;
    }

    public void setWindowPosition(int left, int top)
    {
        int result = LibInterfaceCpp.setWindowPosition(left, top);
        if (logToConsole)
        {
            Debug.Log("G3D library: setWindowPosition result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the window position."
            );
        }
    }

    public void setWindowSize(int width, int height)
    {
        int result = LibInterfaceCpp.setWindowSize(width, height);
        if (logToConsole)
        {
            Debug.Log("G3D library: setWindowSize result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the window size."
            );
        }
    }

    public void setViewportOffset(int left, int top)
    {
        int result = LibInterfaceCpp.setViewportOffset(left, top);
        if (logToConsole)
        {
            Debug.Log("G3D library: setViewportOffset result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the viewport offset."
            );
        }
    }

    public void setViewportSize(int width, int height)
    {
        int result = LibInterfaceCpp.setViewportSize(width, height);
        if (logToConsole)
        {
            Debug.Log("G3D library: setViewportSize result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the viewport size."
            );
        }
    }

    public void setScreenSize(int width, int height)
    {
        int result = LibInterfaceCpp.setScreenSize(width, height);
        if (logToConsole)
        {
            Debug.Log("G3D library: setScreenSize result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when setting the screen dimensions."
            );
        }
    }

    // ------------------------------------------------

    public void shiftViewToLeft()
    {
        int result = LibInterfaceCpp.shiftViewToLeft();
        if (logToConsole)
        {
            Debug.Log("G3D library: shiftViewToLeft result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when shifting the view to the left."
            );
        }
    }

    public void shiftViewToRight()
    {
        int result = LibInterfaceCpp.shiftViewToRight();
        if (logToConsole)
        {
            Debug.Log("G3D library: shiftViewToRight result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if (result == -102)
        {
            throw new G3D_IndexOutOfRangeException(
                "G3D library: a provided index is out of range."
            );
        }
        if (result == -200)
        {
            throw new Exception(
                "G3D library: an unknown error occurred when shifting the view to the right."
            );
        }
    }
}
