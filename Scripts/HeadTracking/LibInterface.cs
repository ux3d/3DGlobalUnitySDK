using System;
using System.Runtime.InteropServices;
using UnityEngine;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TNewShaderParametersCallback(
    in G3DShaderParameters shaderParameters,
    IntPtr listener
);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void TNewHeadPositionCallbackInternal(
    [MarshalAs(UnmanagedType.U1)] bool headDetected,
    [MarshalAs(UnmanagedType.U1)] bool imagePosIsValid,
    int imagePosX,
    int imagePosY,
    double worldPosX,
    double worldPosY,
    double worldPosZ,
    IntPtr listener
);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate void TNewErrorMessageCallbackInternal(
    EMessageSeverity severity,
    byte[] sender,
    byte[] caption,
    byte[] cause,
    byte[] remedy,
    IntPtr listener
);

public enum EMessageSeverity
{
    MS_INFO = 1,
    MS_WARNING,
    MS_ERROR,
    MS_EXCEPTION
};

public struct G3DShaderParameters
{
    // Viewport properties
    int leftViewportPosition; //< The left   position of the viewport in screen coordinates
    int bottomViewportPosition; //< The bottom position of the viewport in screen coordinates

    // Monitor properties
    int screenWidth; //< The screen width in pixels
    int screenHeight; //< The screen height in pixels

    int nativeViewCount; // OLD: viewcount
    int angleRatioNumerator; // OLD: zwinkel
    int angleRatioDenominator; // OLD: nwinkel
    int leftLensOrientation; // OLD: isleft
    int BGRPixelLayout; // OLD: isbgr

    int mstart; // TODO:   rename to viewOffset
    int showTestFrame; // OLD: test
    int showTestStripe; // OLD: stest
    int testGapWidth; // OLD: testgap
    int track;
    int hqViewCount; // OLD: hqview
    int hviews1;
    int hviews2;
    int blur;
    int blackBorder; // OLD: bborder
    int blackSpace; // OLD: bspace
    int bls;
    int ble;
    int brs;
    int bre;

    int zCorrectionValue; // OLD: tvx
    int zCompensationValue; // OLD: zkom
};

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

        // setCalibrationPath(calibrationPath);
        // setConfigPath(configPath);
        // setConfigFileName(configFileName);

        useHimaxD2XXDevices();
        usePmdFlexxDevices();
        initHeadTracking();

        Debug.Log("tracking device count " + getHeadTrackingDeviceCount());
    }

    ~LibInterface()
    {
        deinitHeadTracking();
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
        int result = LibInterfaceCpp.deinitLibrary();
        if (logToConsole)
        {
            Debug.Log("G3D library deinitialization result: " + result);
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

    private void TranslateNewHeadPositionCallback(
        [MarshalAs(UnmanagedType.U1)] bool headDetected,
        [MarshalAs(UnmanagedType.U1)] bool imagePosIsValid,
        int imagePosX,
        int imagePosY,
        double worldPosX,
        double worldPosY,
        double worldPosZ,
        IntPtr listener
    )
    {
        // translate intptr to interface instance
        // call interface instance callback
        GCHandle gch = GCHandle.FromIntPtr(listener);
        ITNewHeadPositionCallback interfaceInstance = (ITNewHeadPositionCallback)gch.Target;

        interfaceInstance.NewHeadPositionCallback(
            headDetected,
            imagePosIsValid,
            imagePosX,
            imagePosY,
            worldPosX,
            worldPosY,
            worldPosZ
        );
    }

    public void registerHeadPositionChangedCallback(
        ITNewHeadPositionCallback inferfaceInstance,
        TNewHeadPositionCallback callback
    )
    {
        GCHandle gch = GCHandle.Alloc(inferfaceInstance);

        TNewHeadPositionCallbackInternal cppTranslationCallback =
            new TNewHeadPositionCallbackInternal(TranslateNewHeadPositionCallback);

        int result = LibInterfaceCpp.registerHeadPositionChangedCallback(
            GCHandle.ToIntPtr(gch),
            cppTranslationCallback
        );
        gch.Free();
        if (logToConsole)
        {
            Debug.Log("G3D library: registerHeadPositionChangedCallback result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: this callback has already been registered."
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
                "G3D library: an unknown error occurred when registering the head position changed callback."
            );
        }
    }

    public void unregisterHeadPositionChangedCallback(ITNewHeadPositionCallback inferfaceInstance)
    {
        GCHandle gch = GCHandle.Alloc(inferfaceInstance);
        int result = LibInterfaceCpp.unregisterHeadPositionChangedCallback(GCHandle.ToIntPtr(gch));
        gch.Free();

        if (logToConsole)
        {
            Debug.Log("G3D library: unregisterHeadPositionChangedCallback result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: this callback has already been unregistered."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: callback not registered.");
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
                "G3D library: an unknown error occurred when unregistering the head position changed callback."
            );
        }
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

    private void TranslateNewErrorMessageCallback(
        EMessageSeverity severity,
        byte[] sender,
        byte[] caption,
        byte[] cause,
        byte[] remedy,
        IntPtr listener
    )
    {
        // translate intptr to interface instance
        // call interface instance callback
        GCHandle gch = GCHandle.FromIntPtr(listener);
        ITNewErrorMessageCallback interfaceInstance = (ITNewErrorMessageCallback)gch.Target;

        interfaceInstance.NewErrorMessageCallback(
            severity,
            System.Text.Encoding.ASCII.GetString(sender),
            System.Text.Encoding.ASCII.GetString(caption),
            System.Text.Encoding.ASCII.GetString(cause),
            System.Text.Encoding.ASCII.GetString(remedy)
        );
    }

    public void registerMessageCallback(
        ITNewErrorMessageCallback inferfaceInstance,
        TNewErrorMessageCallback callback
    )
    {
        GCHandle gch = GCHandle.Alloc(listener);

        TNewErrorMessageCallbackInternal cppTranslationCallback =
            new TNewErrorMessageCallbackInternal(TranslateNewErrorMessageCallback);

        int result = LibInterfaceCpp.registerMessageCallback(
            GCHandle.ToIntPtr(gch),
            cppTranslationCallback
        );
        gch.Free();
        if (logToConsole)
        {
            Debug.Log("G3D library: registerMessageCallback result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: this callback has already been registered."
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
                "G3D library: an unknown error occurred when registering the message callback."
            );
        }
    }

    public void unregisterMessageCallback(ITNewErrorMessageCallback inferfaceInstance)
    {
        GCHandle gch = GCHandle.Alloc(listener);
        int result = LibInterfaceCpp.unregisterMessageCallback(GCHandle.ToIntPtr(gch));
        gch.Free();

        if (logToConsole)
        {
            Debug.Log("G3D library: unregisterMessageCallback result: " + result);
        }
        if (result == -100)
        {
            throw new G3D_AlreadyInitializedException(
                "G3D library: this callback has already been unregistered."
            );
        }
        if (result == -101)
        {
            throw new G3D_NotInitializedException("G3D library: callback not registered.");
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
                "G3D library: an unknown error occurred when unregistering the message callback."
            );
        }
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

/// <summary>
/// This class provides the raw C++ interface to the G3D Universal Head Tracking Library.
/// </summary>
internal static class LibInterfaceCpp
{
    // Error codes
    const int E_OK = 0;
    const int E_INITIALIZED_ALREADY = -100;
    const int E_NOT_INITIALIZED = -101;
    const int E_INDEX_OUT_OF_RANGE = -102;
    const int E_EXCEPTION_ERROR = -200;

    //function definitions
    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?initLibrary@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int initLibrary();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?deinitLibrary@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int deinitLibrary();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setCalibrationPath@G3D_UHTL@@YAHPEBD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setCalibrationPath(in byte[] path);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setConfigPath@G3D_UHTL@@YAHPEBD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setConfigPath(in byte[] path);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setConfigFileName@G3D_UHTL@@YAHPEBD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setConfigFileName(in byte[] path);

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?registerHeadPositionChangedCallback@G3D_UHTL@@YAHPEAXP6AX_N1HHNNN0@Z@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int registerHeadPositionChangedCallback(
        IntPtr listener,
        TNewHeadPositionCallbackInternal callback
    );

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?unregisterHeadPositionChangedCallback@G3D_UHTL@@YAHPEAX@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int unregisterHeadPositionChangedCallback(IntPtr listener);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?registerShaderParametersChangedCallback@G3D_UHTL@@YAHPEAXP6AXPEBUG3DShaderParameters@@0@Z@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int registerShaderParametersChangedCallback(
        IntPtr listener,
        TNewShaderParametersCallback callback
    );

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?unregisterShaderParametersChangedCallback@G3D_UHTL@@YAHPEAX@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int unregisterShaderParametersChangedCallback(IntPtr listener);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?registerMessageCallback@G3D_UHTL@@YAHPEAXP6AXW4EMessageSeverity@@PEBD2220@Z@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int registerMessageCallback(
        IntPtr listener,
        TNewErrorMessageCallback callback
    );

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?unregisterMessageCallback@G3D_UHTL@@YAHPEAX@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int unregisterMessageCallback(IntPtr listener);

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?useHimaxD2XXDevices@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int useHimaxD2XXDevices();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?usePmdFlexxDevices@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int usePmdFlexxDevices();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?initHeadTracking@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int initHeadTracking();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?deinitHeadTracking@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int deinitHeadTracking();

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getHeadTrackingDeviceCount@G3D_UHTL@@YAHPEAH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getHeadTrackingDeviceCount(out int deviceCount);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getHeadTrackingDeviceName@G3D_UHTL@@YAHHPEAPEAD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getHeadTrackingDeviceName(int deviceNumber, out IntPtr deviceName);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getCurrentHeadTrackingDevice@G3D_UHTL@@YAHPEAPEAD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getCurrentHeadTrackingDevice(out IntPtr deviceName);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setCurrentHeadTrackingDevice@G3D_UHTL@@YAHPEBD@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setCurrentHeadTrackingDevice(in byte[] device);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getHeadTrackingDeviceResolution@G3D_UHTL@@YAHPEAH0@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getHeadTrackingDeviceResolution(
        out int horizontalResolution,
        out int verticalResolution
    );

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getFirstValidCalibrationMatrixCol@G3D_UHTL@@YAHPEAH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getFirstValidCalibrationMatrixCol(out int value);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getLastValidCalibrationMatrixCol@G3D_UHTL@@YAHPEAH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getLastValidCalibrationMatrixCol(out int value);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getFirstValidCalibrationMatrixRow@G3D_UHTL@@YAHPEAH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getFirstValidCalibrationMatrixRow(out int value);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getLastValidCalibrationMatrixRow@G3D_UHTL@@YAHPEAH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getLastValidCalibrationMatrixRow(out int value);

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?startHeadTracking@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int startHeadTracking();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?stopHeadTracking@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int stopHeadTracking();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getHeadTrackingStatus@G3D_UHTL@@YAHPEA_N0@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getHeadTrackingStatus(
        [MarshalAs(UnmanagedType.U1)] out bool hasDevice,
        [MarshalAs(UnmanagedType.U1)] out bool isTracking
    );

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?getCurrentShaderParameters@G3D_UHTL@@YAHPEAUG3DShaderParameters@@@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int getCurrentShaderParameters(out G3DShaderParameters shaderParameters);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setWindowPosition@G3D_UHTL@@YAHHH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setWindowPosition(int left, int top);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setWindowSize@G3D_UHTL@@YAHHH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setWindowSize(int width, int height);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setViewportOffset@G3D_UHTL@@YAHHH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setViewportOffset(int left, int top);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setViewportSize@G3D_UHTL@@YAHHH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setViewportSize(int width, int height);

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?setScreenSize@G3D_UHTL@@YAHHH@Z",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int setScreenSize(int width, int height);

    // ------------------------------------------------

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?shiftViewToLeft@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int shiftViewToLeft();

    [DllImport(
        "G3D_UniversalHeadTrackingLibrary.dll",
        EntryPoint = "?shiftViewToRight@G3D_UHTL@@YAHXZ",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern int shiftViewToRight();
}
