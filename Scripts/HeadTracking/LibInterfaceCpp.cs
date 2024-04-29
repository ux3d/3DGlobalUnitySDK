using System;
using System.Runtime.InteropServices;
using UnityEngine;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TNewErrorMessageCallback(
    EMessageSeverity severity,
    in byte[] sender,
    in byte[] caption,
    in byte[] cause,
    in byte[] remedy,
    out IntPtr listener
);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TNewHeadPositionCallback(
    [MarshalAs(UnmanagedType.U1)] bool headDetected,
    [MarshalAs(UnmanagedType.U1)] bool imagePosIsValid,
    int imagePosX,
    int imagePosY,
    double worldPosX,
    double worldPosY,
    double worldPosZ,
    out IntPtr listener
);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void TNewShaderParametersCallback(
    in G3DShaderParameters shaderParameters,
    out IntPtr listener
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

/// <summary>
/// This class provides the raw C++ interface to the G3D Universal Head Tracking Library.
/// </summary>
public class LibInterfaceCpp
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
        TNewHeadPositionCallback callback
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
