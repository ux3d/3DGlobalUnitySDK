using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class LibInterface
{
    public static bool logToConsole = true;
    //function definitions
    public static void initLibrary() {
        int result = LibInterfaceCpp.initLibrary();
        if(logToConsole) {
            Debug.Log("G3D library initialization result: " + result);
        }
        if(result == -100) {
            throw new G3D_AlreadyInitializedException("G3D library already initialized.");
        }
        if(result == -101) {
            throw new G3D_NotInitializedException("G3D library not initialized.");
        }
        if(result == -102) {
            throw new G3D_IndexOutOfRangeException("G3D library a provided index is out of range.");
        }
        if(result == -200) {
            throw new Exception("G3D library an unknown error occurred.");
        }
    }

    public static int deinitLibrary();

    public static int setCalibrationPath(in byte[] path);
    public static int setConfigPath(in byte[] path);
    public static int setConfigFileName(in byte[] path);

    // ------------------------------------------------
    
    public static int registerHeadPositionChangedCallback(out IntPtr listener, TNewHeadPositionCallback callback);
    public static int unregisterHeadPositionChangedCallback(IntPtr listener);
    public static int registerShaderParametersChangedCallback(IntPtr listener, TNewShaderParametersCallback callback);
    public static int unregisterShaderParametersChangedCallback(IntPtr listener);
    public static int registerMessageCallback(IntPtr listener, TNewErrorMessageCallback callback);
    public static int unregisterMessageCallback(IntPtr listener);
    
    // ------------------------------------------------

    public static int useHimaxD2XXDevices();
    public static int usePmdFlexxDevices();
    public static int initHeadTracking();
    public static int deinitHeadTracking();

    // ------------------------------------------------

    public static int getHeadTrackingDeviceCount(out int deviceCount);
    public static int getHeadTrackingDeviceName(int deviceNumber, out IntPtr deviceName);
    public static int getCurrentHeadTrackingDevice(out IntPtr deviceName);
    public static int setCurrentHeadTrackingDevice(in byte[] device);
    public static int getHeadTrackingDeviceResolution(out int horizontalResolution, out int verticalResolution);
    
    // ------------------------------------------------

    public static int getFirstValidCalibrationMatrixCol(out int value);
    public static int getLastValidCalibrationMatrixCol(out int value);
    public static int getFirstValidCalibrationMatrixRow(out int value);
    public static int getLastValidCalibrationMatrixRow(out int value);
    
    // ------------------------------------------------

    public static int startHeadTracking();
    public static int stopHeadTracking();
    public static int getHeadTrackingStatus([MarshalAs(UnmanagedType.U1)] out bool hasDevice, [MarshalAs(UnmanagedType.U1)] out bool isTracking);
    
    // ------------------------------------------------

    public static int getCurrentShaderParameters(out CG3DShaderParameters shaderParameters);
    public static int setWindowPosition(int left, int top);
    public static int setWindowSize(int width, int height);
    public static int setViewportOffset(int left, int top);
    public static int setViewportSize(int width, int height);
    public static int setScreenSize(int width, int height);
    
    // ------------------------------------------------

    public static int shiftViewToLeft();
    public static int shiftViewToRight();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TNewErrorMessageCallback(
        EMessageSeverity severity, in byte[] sender, in byte[] caption, in byte[] cause, in byte[] remedy, out IntPtr listener);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TNewHeadPositionCallback(
        [MarshalAs(UnmanagedType.U1)] bool headDetected, [MarshalAs(UnmanagedType.U1)] bool imagePosIsValid, int imagePosX, int imagePosY,
        double worldPosX, double worldPosY, double worldPosZ, out IntPtr listener);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void TNewShaderParametersCallback(
        in CG3DShaderParameters shaderParameters, out IntPtr listener);
}
