using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;


public class G3DAPI
{

#region api initialization

    private static IntPtr handle_dll = IntPtr.Zero;
    private static IntPtr handle_window = IntPtr.Zero;
    private static IntPtr handle_monitor = IntPtr.Zero;
    private static bool initialized = false;

    private static Dictionary<IntPtr, TG3DMonitorInfoV1> cache_monitorInfo;
    private static Dictionary<IntPtr, Viewmap> cache_viewmaps;
    private static Dictionary<IntPtr, Vectormap> cache_vectormaps;

    private static TG3DMonitorInfoV1? currentMonitorInfo = null;

    private static string getLibraryPath()
    {
    #if UNITY_64
        return Path.Combine(Application.streamingAssetsPath, "G3D", "api", "x64", "G3DClientAPI.dll");
    #else
        return Path.Combine(Application.streamingAssetsPath, "G3D", "api", "x86", "G3DClientAPI.dll");
    #endif
    }

    public static void init(string windowTitle)
    {
        //dll
        handle_dll = LoadLibrary(getLibraryPath());
        if (handle_dll == IntPtr.Zero)
        {
            Debug.LogError("DLL loading failed");
            return;
        }

        //functions
        _G3DHeadTrackingSetupA = ExtractFunction<f_G3DHeadTrackingSetupA>(handle_dll, "G3DHeadTrackingSetupA");
        _G3DHeadTrackingGetStateA = ExtractFunction<f_G3DHeadTrackingGetStateA>(handle_dll, "G3DHeadTrackingGetStateA");
        _G3DBuildVectorMapFromParametersA = ExtractFunction<f_G3DBuildVectorMapFromParametersA>(handle_dll, "G3DBuildVectorMapFromParametersA");
        _G3DGetVectorMapInformationA = ExtractFunction<f_G3DGetVectorMapInformationA>(handle_dll, "G3DGetVectorMapInformationA");
        _G3DGetVectorMapIndexListA = ExtractFunction<f_G3DGetVectorMapIndexListA>(handle_dll, "G3DGetVectorMapIndexListA");
        _G3DGetVectorMapDataA = ExtractFunction<f_G3DGetVectorMapDataA>(handle_dll, "G3DGetVectorMapDataA");
        _G3DFreeVectorMapData = ExtractFunction<f_G3DFreeVectorMapData>(handle_dll, "G3DFreeVectorMapData");
        _G3DGetMonitorInfoByHandle = ExtractFunction<f_G3DGetMonitorInfoByHandle>(handle_dll, "G3DGetMonitorInfoByHandleA");
        _G3DRefreshMonitorList = ExtractFunction<f_G3DRefreshMonitorList>(handle_dll, "G3DRefreshMonitorList");
        _G3DGetViewMapFromMonitorHandleA = ExtractFunction<f_G3DGetViewMapFromMonitorHandleA>(handle_dll, "G3DGetViewMapFromMonitorHandleA");
        _G3DGetVectorMapFromMonitorHandleA = ExtractFunction<f_G3DGetVectorMapFromMonitorHandleA>(handle_dll, "G3DGetVectorMapFromMonitorHandleA");

        //caches
        cache_monitorInfo = new Dictionary<IntPtr, TG3DMonitorInfoV1>();
        cache_viewmaps = new Dictionary<IntPtr, Viewmap>();
        cache_vectormaps = new Dictionary<IntPtr, Vectormap>();

        //monitor handling
        handle_window = FindWindow(null, windowTitle);
        if (handle_window == IntPtr.Zero) Debug.LogError($"Monitor Handle could not be found for {windowTitle}");

        G3DRefreshMonitorList();
        detectMonitorChange();

        initialized = true;
    }
    public static void free()
    {
        observe_state_stop();

        if (handle_dll != IntPtr.Zero)
            FreeLibrary(handle_dll);
    }

    public static bool update(bool invertViewmapY = true)
    {
        if (_G3DGetViewMapFromMonitorHandleA == null) return false;
        if (_G3DGetVectorMapFromMonitorHandleA == null) return false;
        if (currentMonitorInfo == null) return false;

        //update current viewmap
        Viewmap vm;
        if (cache_viewmaps.ContainsKey(currentMonitorInfo.Value.MonitorHandle))
        {
            vm = cache_viewmaps[currentMonitorInfo.Value.MonitorHandle];
        }
        else
        {
            //first load
            vm = new Viewmap();
            vm.width = currentMonitorInfo.Value.Width;
            vm.height = currentMonitorInfo.Value.Height;
            vm.data = new byte[vm.width * 3 * vm.height];
            uint IsLoadFromFile = 0;

            lock (lock_api)
            {
                if (_G3DGetViewMapFromMonitorHandleA(
                    (uint)currentMonitorInfo.Value.Width,
                    (uint)currentMonitorInfo.Value.Height,
                    vm.data,
                    3,
                    (uint)currentMonitorInfo.Value.Width * 3,
                    (uint)(invertViewmapY ? 1 : 0),
                    currentMonitorInfo.Value.HQSupported,
                    currentMonitorInfo.Value.MonitorHandle,
                    ref vm.viewcount,
                    ref vm.hqSimple,
                    ref IsLoadFromFile,
                    buffer_ErrorMessage,
                    (uint)buffer_ErrorMessage.Length) == 0)
                {
                    Debug.LogError("ERROR|Viewmap: " + ErrorMessage);
                    return false;
                }
                else
                {
                    cache_viewmaps[currentMonitorInfo.Value.MonitorHandle] = vm;
                }
            }
        }

        //update current vectormap
        Vectormap vecm;
        if (cache_vectormaps.ContainsKey(currentMonitorInfo.Value.MonitorHandle))
        {
            vecm = cache_vectormaps[currentMonitorInfo.Value.MonitorHandle];
        } 
        else
        {
            vecm = new Vectormap();
            lock (lock_api)
            {
                var handle_vectormap = IntPtr.Zero;
                if (_G3DGetVectorMapFromMonitorHandleA(currentMonitorInfo.Value.MonitorHandle, ref handle_vectormap, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length) == 0)
                {
                    Debug.LogError("ERROR|Vectormap: " + ErrorMessage);
                    return false;
                }
                else
                {
                    vecm.info = G3DGetVectorMapInformationA(handle_vectormap);
                    vecm.data = G3DGetVectorMapDataA(handle_vectormap, vecm.info);
                    vecm.indexMaps = new VectorMapIndexList[vecm.info.Repetition];
                    for (int currentRepitition = 0; currentRepitition < vecm.info.Repetition; currentRepitition++)
                    {
                        vecm.indexMaps[currentRepitition] = G3DGetVectorMapIndexListA(handle_vectormap, vecm.info, (byte)currentRepitition, false);
                    }
                    cache_vectormaps[currentMonitorInfo.Value.MonitorHandle] = vecm;
                }
            }
        }

        //update headtracking configuration
        var setup = new TG3DHeadTrackingConfigurationV1
        {
            Size = (ushort)Marshal.SizeOf<TG3DHeadTrackingConfigurationV1>(),    // size of record, must be set before use structure
            Version = 1,                                                         // version (1..n)

            Enabled = 1,                                                            // if headtracking can be used (call HeadTrackingSetup with enabled=false will disconnect)
            StereoViewHQMode = 0,                                                   // hq mode (only used in view optimizer or if forced; means to calculate used view count by MonitorViewCount * MonitorLensWidth)
            MonitorViewCount = vm.viewcount,                                        // view count (only used in view optimizer or if forced; -1 means to use default configuration which is only supported in head tracking (so not in view optimizer fallback))
            MonitorLensWidth = currentMonitorInfo.Value.LensWidth,                  // lens width (in number of pixel; only used in view optimizer or if forced)
            MonitorLensAngleCounter = currentMonitorInfo.Value.LensAngleCounter,    // lens angle counter (fallback for lens width; only for very old view optimizer versions and only in view optimizer or if forced)
            NeedEyeCenterPosMMRaw = 0,                                              // if raw eye center position is required(camera world, without hysteresis)
            NeedEyeCenterPosMMVer1 = 1,                                             // if real eye center position is required(screen world, with hysteresis)
            NeedUserPosStatus = 0,                                                  // if user position status is required
            NeedFreezeHysteresis = 1,                                               // if freeze and hysteresis is required
            NeedDistanceCorrection = 0,                                             // if distance correction value is required
            NeedAngleHorizontal = 0,                                                // if horizontal angle is required(not suggested since calculated in camera world)
            NeedUserPosHystAvg = 0,                                                 // if user position hysteresisand average parameters are required
            MonitorHandle = currentMonitorInfo.Value.MonitorHandle,                 // monitor handle(if set(< >0), connection to this monitor is established(headtracking server instead of old viewoptimizer))
            ViewOptimizerFallbackUsed = 1,                                          // if monitor handle set and headtracking server not available, try to connect old viewoptimizer (if monitorhandle=0, old viewoptimizer is used in any case)
            ViewOptimizerFallbackTime = 3000,                                       // wait time between lost connection (or start connect) and switch type (in ms)
            ForceWriteLensConfig = 0                                                // write lens configuration also, if head tracking is used(with separate command; view optimizer will request values in any case)
        };

        if (!G3DHeadTrackingSetupA(setup))
        {
            Debug.LogError("ERROR|Headtracking Setup: " + ErrorMessage);
            return false;
        }

        return true;
    }

    public static bool isInitialized()
    {
        return initialized;
    }

#endregion

#region api types

    public enum TG3DHeadTrackingUserPosStatusSimple
    {
        upss_NoUser,            // no user detected
        upss_Ready,             // user detected in valid position
        upss_WarnLeft,          // user is at border of valid range left (-x)
        upss_WarnRight,         // user is at border of valid range right (+x)
        upss_WarnTop,           // user is at border of valid range top (+y)
        upss_WarnBottom,        // user is at border of valid range bottom (-y)
        upss_WarnNear,          // user is at border of valid range near (-z)
        upss_WarnFar,           // user is at border of valid range far (+z)
        upss_ErrorLeft,         // user is outside valid range left (-x)
        upss_ErrorRight,        // user is outside valid range right (+x)
        upss_ErrorTop,          // user is outside valid range top (+y)
        upss_ErrorBottom,       // user is outside valid range bottom (-y)
        upss_ErrorNear,         // user is outside valid range near (-z)
        upss_ErrorFar,          // user is outside valid range far (+z)
        upss_Error              // error in calculation or communication
    };

    public struct TG3DHeadTrackingPoint3D
    {
        public float X;
        public float Y;
        public float Z;
    };

    public struct TG3DHeadTrackingConfigurationV1
    {
        public ushort Size;                     // size of record, must be set before use structure
        public ushort Version;                  // version (1..n)

        public uint Enabled;                    // if headtracking can be used (call HeadTrackingSetup with enabled=false will disconnect)
        public uint StereoViewHQMode;           // hq mode (only used in view optimizer or if forced; means to calculate used view count by MonitorViewCount * MonitorLensWidth)
        public int MonitorViewCount;            // view count (only used in view optimizer or if forced; -1 means to use default configuration which is only supported in head tracking (so not in view optimizer fallback))
        public int MonitorLensWidth;            // lens width (in number of pixel; only used in view optimizer or if forced)
        public int MonitorLensAngleCounter;     // lens angle counter (fallback for lens width; only for very old view optimizer versions and only in view optimizer or if forced)
        public uint NeedEyeCenterPosMMRaw;      // if raw eye center position is required(camera world, without hysteresis)
        public uint NeedEyeCenterPosMMVer1;     // if real eye center position is required(screen world, with hysteresis)
        public uint NeedUserPosStatus;          // if user position status is required
        public uint NeedFreezeHysteresis;       // if freezeand hysteresis is required
        public uint NeedDistanceCorrection;     // if distance correction value is required
        public uint NeedAngleHorizontal;        // if horizontal angle is required(not suggested since calculated in camera world)
        public uint NeedUserPosHystAvg;         // if user position hysteresisand average parameters are required
        public IntPtr MonitorHandle;            // monitor handle(if set(< >0), connection to this monitor is established(headtracking server instead of old viewoptimizer))
        public uint ViewOptimizerFallbackUsed;  // if monitor handle set and headtracking server not available, try to connect old viewoptimizer (if monitorhandle=0, old viewoptimizer is used in any case)
        public uint ViewOptimizerFallbackTime;  // wait time between lost connection (or start connect) and switch type (in ms)
        public uint ForceWriteLensConfig;       // write lens configuration also, if head tracking is used(with separate command; view optimizer will request values in any case)
    };

    public struct TG3DHeadTrackingStateHeaderV1
    {
        public ushort Size;                                                 // size of record, must be set before use structure
        public ushort Version;                                              // version (1..n)

        public uint Enabled;                                                // if head tracking is enabled
        public uint Connected;                                              // if head tracking is connected
        public uint Force2D;                                                // if tracking force 2d mode (no user detected)
        public int TrackingOffset;                                          // tracking offset in number of views
        public uint FreezeHysteresisSupported;                              // if freeze and hysteresis is supported by interface
        public uint TrackingFreezed;                                        // if tracking is freezed(FALSE, if not supported)
        public int TrackingHysteresis;                                      // hysteresis in degree (only used, if catch tracking is active)
        public uint CatchTracking;                                          // if catch tracking is active(use hysteresis)
        public uint UserPosStatusSupported;                                 // if user position status is supported by interface
        public TG3DHeadTrackingUserPosStatusSimple UserPosStatusSimple;     // current user position status
        public uint EyeCenterPosMMV1Supported;                              // if corrected center position between eyes of first user is supported
        public TG3DHeadTrackingPoint3D EyeCenterPosMMV1;                    // corrected center position between eyes of first user
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TG3DMonitorEDIDData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 3)]
        public string ManufacturerID;                           // manufacturer code converted to string
        public ushort ManufacturerProductCode;                  // product code of manufacturer
        public uint SerialNumber;                               // serial number
        public byte Week;                                       // production week ($FF, if year is model year and week not available; week is not consistent between manufacturers)
        public ushort Year;                                     // production or model year
        public int Width;                                       // width in mm
        public int Height;                                      // height in mm
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Manufacturer;                             // manufacturer id converted to long name (only, if id is known; return id if not known)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
        public string DisplayName;                              // display name read from EDID record
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
        public string DisplaySerial;                            // display serial number read from EDID record
        public uint Valid;                                      // if EDID record read
    };


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct TG3DMonitorInfoV1
    {
        public ushort Size;                                        // size of record, must be set before use structure
        public ushort Version;                                     // version (1..n)

        // version 1 fields
        public IntPtr MonitorHandle;                               // handle to monitor
        public int MonitorNum;                                     // number in monitor list
        public int Width;                                          // width in pixel (from rcMonitor of MONITORINFO)
        public int Height;                                         // height in pixel (from rcMonitor of MONITORINFO)
        public int Left;                                           // left position in pixel (from rcMonitor of MONITORINFO)
        public int Top;                                            // top position in pixel (from rcMonitor of MONITORINFO)

        // data read from windows and EDID record
        // these data are unchangable
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DeviceString;                           // device string (monitor name from device manager)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DevicePath;                             // unique device id (path to device; can be used in setup api)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string DeviceID;                               // unique device id (extracted from device path; maybe equal to device path)
        public TG3DMonitorEDIDData EDIDData;                       // EDID record decoded
        public uint Valid;                                         // if data are valid (4 values above)

        // additional data (read from registry/ini, can be modified)
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string MonitorName;                            // name of monitor (for better identification, is initialized with devicestring from device manager)
        public int ViewCount;                                      // view count (if 0, 3d is not supported)
        public int LensWidth;                                      // lens width (in number of pixel)
        public int LensAngleCounter;                               // angle counter (including sign)
        public uint ViewOrderInverted;                             // if view order is inverted (left and right mirrored)
        public uint Rotated;                                       // if monitor is rotated by 90° (not supported by deprecated algorithm)
        public uint FullPixel;                                     // if only full pixel available (lens is always over full pixel; no sub pixel addressable)
        public uint HQSupported;                                   // if hq mode is supported (e.g. not, if monitor is rotated by 90° and deprecated view map algorithm is used)
        public uint BGRMode;                                       // if red and blue channel are exchanged (display rotated by 180°)
        public float MonitorWidth;                                 // width in mm (is initialized with dimension read from EDID record)
        public float MonitorHeight;                                // height in mm (is initialized with dimension read from EDID record)
        public float ViewAreaWidth;                                // viewing area width in mm (is initialized with width read from EDID record)
        public float ViewAreaDistance;                             // viewing area distance in mm
        public float StereoViewWidthHeadTracking;                  // width of stereo views if headtracking is enabled in mm (for left/right eye; < 0 means unknown; used for black matrix)
        public float StereoViewDistanceHeadTracking;               // distance between stereo views if headtracking is enabled in mm (between eyes; < 0 means unknown; used for black matrix)
        public float StereoViewWidthStatic;                        // width of stereo views if headtracking is disabled in mm (for left/right eye; < 0 means unknown; used for black matrix)
        public float StereoViewDistanceStatic;                     // distance between stereo views if headtracking is disabled in mm (between eyes; < 0 means unknown; used for black matrix)
        public uint ViewMapUseDeprecatedAlgorithm;                 // if deprecated alghorithm should be used to calculate view map
        public int VectorMapCenterView;                            // center view for vector map creation (which view number is visible, if eye is at screen center)
        public float VectorMapCenterOffset;                        // offset of center view for vector map creation (offset to see center view homogen; if not exactly at screen center)
        public int VectorMapRepetitionCount;                       // number of view repetitions for vector map creation (1=only first; 2=first and left/right half view; 3=first and left/right full view; 5=first and left/right 2 views; all other values are not supported)
        public float VectorMapAccuracy;                            // maximum difference in x at viewing area distance between vectors to be identical (in mm; used to reduce number of vectors; suggested value is pixel distance; used for vector map creation)
        public float VectorMapRandomOffset;                        // random offset for vectors at viewing area distance (in mm; used to prevent from interference patterns; suggested value is 1/4 of one view width; used for vector map creation)
        public float VectorMapStereoZoneWidthHeadTracking;         // width of stereo zones if headtracking is enabled in mm (for left/right eye; < 0 means unknown; used for black matrix)
        public float VectorMapStereoZoneDistanceHeadTracking;      // distance between stereo zones if headtracking is enabled in mm (between eyes; < 0 means unknown; used for black matrix)
        public float VectorMapStereoZoneWidthStatic;               // width of stereo zones if headtracking is disabled in mm (for left/right eye; < 0 means unknown; used for black matrix)
        public float VectorMapStereoZoneDistanceStatic;            // distance between stereo zones if headtracking is disabled in mm (between eyes; < 0 means unknown; used for black matrix)
        public float LensFactorY;                                  // lens factor for y correction of head tracking (cached value, calculated, if lens configuration changed)
    };

    public class Viewmap
    {
        public int viewcount;
        public uint hqSimple;
        public byte[] data;
        public int width;
        public int height;
    }

    public class Vectormap
    {
        public VectorMapInfo info;
        public VectorMapData data;
        public VectorMapIndexList[] indexMaps;
    }

#endregion

#region api functions

    private static object lock_api = new object();

#region info

    private delegate uint f_G3DGetMonitorInfoByHandle(IntPtr MonitorHandle, ref TG3DMonitorInfoV1 Data, byte[] ErrorMessage, uint ErrorMessageMaxChars);
    private static f_G3DGetMonitorInfoByHandle? _G3DGetMonitorInfoByHandle;
    
    public static TG3DMonitorInfoV1? G3DGetMonitorInfoByHandle(IntPtr monitorHandle)
    {
        if (_G3DGetMonitorInfoByHandle == null) return null;
        if (cache_monitorInfo.ContainsKey(monitorHandle)) return cache_monitorInfo[monitorHandle];

        TG3DMonitorInfoV1 data = new TG3DMonitorInfoV1()
        {
            Size = (ushort)Marshal.SizeOf<TG3DMonitorInfoV1>(),
            Version = 1
        };
        lock (lock_api)
        {
            if (_G3DGetMonitorInfoByHandle(monitorHandle, ref data, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length) == 0)
            {
                Debug.LogError("ERROR|Info: " + ErrorMessage);
                return null;
            }
            cache_monitorInfo[monitorHandle] = data; 
            return data;
        }
    }
    public static TG3DMonitorInfoV1? GetCurrentMonitorInfo()
    {
        return currentMonitorInfo;
    }

#endregion

#region setup

    private delegate uint f_G3DHeadTrackingSetupA(ref TG3DHeadTrackingConfigurationV1 setup, byte[] ErrorMessage, uint ErrorMessageMaxChars);
    private static f_G3DHeadTrackingSetupA? _G3DHeadTrackingSetupA;
    
    public static bool G3DHeadTrackingSetupA(TG3DHeadTrackingConfigurationV1 setup)
    {
        if (_G3DHeadTrackingSetupA == null) return false;

        lock (lock_api)
        {
            if (_G3DHeadTrackingSetupA(ref setup, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length) == 0)
            {
                Debug.LogError("ERROR|Setup: " + ErrorMessage);
                return false;
            }
            return true;
        }
    }
    
    
    private delegate uint f_G3DRefreshMonitorList();
    private static f_G3DRefreshMonitorList? _G3DRefreshMonitorList;
    public static void G3DRefreshMonitorList()
    {
        if (_G3DRefreshMonitorList == null) return;
        _G3DRefreshMonitorList();
    }

#endregion

#region state

    private delegate uint f_G3DHeadTrackingGetStateA(ref TG3DHeadTrackingStateHeaderV1 data, byte[] ErrorMessage, uint ErrorMessageMaxChars);
    private static f_G3DHeadTrackingGetStateA? _G3DHeadTrackingGetStateA;
    public static TG3DHeadTrackingStateHeaderV1? G3DHeadTrackingGetStateA()
    {
        if (_G3DHeadTrackingGetStateA == null) return null;

        var data = new TG3DHeadTrackingStateHeaderV1
        {
            Size = (ushort)Marshal.SizeOf<TG3DHeadTrackingStateHeaderV1>(),
            Version = 1
        };

        lock (lock_api)
        {
            if (_G3DHeadTrackingGetStateA(ref data, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length) == 0)
            {
                Debug.LogError("ERROR|State: " + ErrorMessage);
                return null;
            }
            else
            {
                return data;
            }
        }
    }
    public static long ViewOffset
    {
        get
        {
            var state = G3DHeadTrackingGetStateA();
            return state?.TrackingOffset ?? 0;
        }
    }

    public static Vector3 UserPosition
    {
        get
        {
            var state = G3DHeadTrackingGetStateA();
            if (state == null) return Vector3.zero;
            return new Vector3(state.Value.EyeCenterPosMMV1.X, state.Value.EyeCenterPosMMV1.Y, state.Value.EyeCenterPosMMV1.Z);
        }
    }

#endregion

#region viewmap

    private delegate uint f_G3DGetViewMapFromMonitorHandleA(
        uint Width,
        uint Height,
        byte[] destination,
        byte PixelSize,
        uint LineSize,
        uint InvertYOrder,
        uint HQMode,
        IntPtr MonitorHandle,
        ref int ViewCount,
        ref uint SimpleHQMode,
        ref uint IsLoadFromFile, 
        byte[] ErrorMessage, 
        uint ErrorMessageMaxChars);
    private static f_G3DGetViewMapFromMonitorHandleA? _G3DGetViewMapFromMonitorHandleA;
    public static Viewmap? GetCurrentViewmap()
    {
        if (currentMonitorInfo == null) return null; 

        if(cache_viewmaps.ContainsKey(currentMonitorInfo.Value.MonitorHandle)) 
            return cache_viewmaps[currentMonitorInfo.Value.MonitorHandle];

        return null;
    }

#endregion

#region vectormap


#region vectormap_build

    private delegate uint f_G3DBuildVectorMapFromParametersA(
        uint Width,                             // width in pixel (should be monitor width)
        uint Height,                            // height in pixel (should be monitor height)
        int MonitorViewCount,                   // view count (if 0, 3d is not supported)
        int MonitorLensWidth,                   // lens width (in number of pixel)
        int MonitorLensAngleCounter,            // angle counter (including sign)
        uint MonitorViewOrderInverted,          // if view order is inverted (left and right mirrored)
        uint MonitorRotated,                    // if monitor is rotated by 90° (not supported by deprecated algorithm)
        uint MonitorFullPixel,                  // if only full pixel available (lens is always over full pixel; no sub pixel addressable)
        uint MonitorHQSupported,                // if hq mode is supported (e.g. not, if monitor is rotated by 90° and deprecated view map algorithm is used)
        uint MonitorBGRMode,                    // if red and blue channel are exchanged (display rotated by 180°)
        uint MonitorWidth,                      // Width in mm
        uint MonitorHeight,                     // Height in mm
        float ViewAreaWidth,                    // Viewing area width in mm
        float ViewAreaHeight,                   // Viewing area distance in mm
        int CenterView,                         // center view (light rays of this view match screen center in viewing area distance)
        float CenterViewOffset,                 // offset between eye seeing center view at viewing area distance and center of screen (required since center view not exactly at screen center)
        int RepetitionCount,                    // number of view repetitions for vector map creation (1=only first; 2=first and left/right half view; 3=first and left/right full view; 5=first and left/right 2 views; all other values are not supported)
        float Accuracy,                         // maximum difference in x at viewing area distance between vectors to be identical (in mm; used to reduce number of vectors; suggested value is pixel distance)
        float RandomOffset,                     // random offset for vectors at viewing area distance (in mm; used to prevent from interference patterns; suggested value is 1/4 of one view width), byte[] ErrorMessage, uint ErrorMessageMaxChars);
        ref IntPtr ResultData,                  // result data
        byte[] ErrorMessage,                    // space for error details
        uint ErrorMessageMaxChars               // number of characters (including final zero) for error details
    );
    private static f_G3DBuildVectorMapFromParametersA? _G3DBuildVectorMapFromParametersA;
    public static IntPtr? G3DBuildVectorMapFromParametersA(
        uint Width, uint Height, int MonitorViewCount, int MonitorLensWidth, int MonitorLensAngleCounter, uint MonitorViewOrderInverted, uint MonitorRotated,
        uint MonitorFullPixel, uint MonitorHQSupported, uint MonitorBGRMode, uint MonitorWidth, uint MonitorHeight, float ViewAreaWidth, float ViewAreaHeight,
        int CenterView, float CenterViewOffset,
        int RepetitionCount, float Accuracy, float RandomOffset
    )
    {
        if (_G3DBuildVectorMapFromParametersA == null) return null;

        bool success = false;
        var handle_vectormap = IntPtr.Zero;
        lock (lock_api)
        {
            success = _G3DBuildVectorMapFromParametersA(
                Width, Height, MonitorViewCount, MonitorLensWidth, MonitorLensAngleCounter, MonitorViewOrderInverted, MonitorRotated,
                MonitorFullPixel, MonitorHQSupported, MonitorBGRMode, MonitorWidth, MonitorHeight, ViewAreaWidth, ViewAreaHeight,
                CenterView, CenterViewOffset,
                RepetitionCount, Accuracy, RandomOffset, ref handle_vectormap, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length
            ) != 0;
        }
        if (!success) Debug.Log("ERROR|G3DBuildVectorMapFromParametersA: " + ErrorMessage);
        return handle_vectormap;
    }

#endregion


#region vectormap_from_handle

    private delegate uint f_G3DGetVectorMapFromMonitorHandleA(IntPtr MonitorHandle, ref IntPtr ResultData, byte[] ErrorMessage, uint ErrorMessageMaxChars);
    private static f_G3DGetVectorMapFromMonitorHandleA? _G3DGetVectorMapFromMonitorHandleA;
    public static Vectormap GetCurrentVectormap()
    {
        if (currentMonitorInfo == null) return null;

        if (cache_vectormaps.ContainsKey(currentMonitorInfo.Value.MonitorHandle))
            return cache_vectormaps[currentMonitorInfo.Value.MonitorHandle];

        return null;
    }

#endregion


#region vectormap_info

    public class VectorMapInfo
    {
        public byte Repetition;
        public uint Width;
        public uint Height;
        public bool IndexMapIs16Bit;
        public uint PositionMapSize;
        public ushort VectorMapSize;
        public int ViewCount;
        public bool SimpleHQMode;
        public bool IsLoadFromFile;
    }

    private delegate uint f_G3DGetVectorMapInformationA(
        IntPtr Source,               // view map source
        ref byte Repetition,         // repetition (must match available repetitions in vectormap)
        ref uint Width,              // width of index map in pixel (will be equal to width used for creation of map; only written if <> nil)
        ref uint Height,             // height of index map in pixel (will be equal to height used for creation of map; only written if <> nil)
        ref bool IndexMapIs16Bit,    // if vector index map use 16 bit for every value (up to 65535 vectors instead 256 vectors; only written if <> nil)
        ref uint PositionMapSize,    // number of values in pixel position map in x (will be equal to width used for creation of map; only written if <> nil)
        ref ushort VectorMapSize,    // number of values in vector map (only written if <> nil)
        ref int ViewCount,           // number of views used in view map (MonitorViewCount if not in HQMode; MonitorViewCount*FMonitorLensWidth if in HQMode; other values possible (e.g. if BuildViewMapEx used); only written if <> nil)
        ref bool SimpleHQMode,       // if currently in simple hq mode (can be set to false, if ViewCount calculated with BuildViewMapEx and ViewCount <> MonitorViewCount*FMonitorLensWidth; only written if <> nil)
        ref bool IsLoadFromFile,     // if vector map was load from file (only written if <> nil)
        byte[] ErrorMessage,         // space for error details
        uint ErrorMessageMaxChars    // number of characters (including final zero) for error details
    );
    private static f_G3DGetVectorMapInformationA _G3DGetVectorMapInformationA;

    public static VectorMapInfo G3DGetVectorMapInformationA(IntPtr handle_vectormap)
    {
        if (_G3DGetVectorMapInformationA == null) return null;
        if (handle_vectormap == IntPtr.Zero) return null;

        bool success = false;
        var vmi = new VectorMapInfo();
        lock (lock_api)
        {
            success = _G3DGetVectorMapInformationA(
                handle_vectormap, ref vmi.Repetition, ref vmi.Width, ref vmi.Height, ref vmi.IndexMapIs16Bit, ref vmi.PositionMapSize, ref vmi.VectorMapSize,
                ref vmi.ViewCount, ref vmi.SimpleHQMode, ref vmi.IsLoadFromFile, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length
            ) != 0;
        }
        if (!success) Debug.Log("ERROR|G3DGetVectorMapInformationA: " + ErrorMessage);
        return success ? vmi : null;
    }

#endregion


#region vectormap_indexmap

    public class VectorMapIndexList
    {
        public byte[] Indices;
        public VectorMapIndexList(VectorMapInfo info, byte pixelsize)
        {
            Indices = new byte[info.Width * pixelsize * info.Height];
        }
    }
    private delegate uint f_G3DGetVectorMapIndexListA(
        IntPtr Source,              // view map source
        byte Repetition,            // repetition (must match available repetitions in vectormap)
        uint Width,                 // width in pixel (must be vector width)
        uint Height,                // height in pixel (must be vector height)
        byte[] Destination,         // destination
        byte PixelSize,             // size of one pixel (only 3/6 (RGB) or 4/8 (RGBA) allowed)
        uint LineSize,              // size of one line
        uint InvertYOrder,          // if to invert y direction
        byte[] ErrorMessage,        // space for error details
        uint ErrorMessageMaxChars   // number of characters (including final zero) for error details
    );
    private static f_G3DGetVectorMapIndexListA? _G3DGetVectorMapIndexListA;
    public static VectorMapIndexList G3DGetVectorMapIndexListA(IntPtr handle_vectormap, VectorMapInfo info, byte repition = 0, bool rgba = true)
    {
        if (_G3DGetVectorMapIndexListA == null) return null;
        if (handle_vectormap == IntPtr.Zero) return null;
        if (info == null) return null;

        byte pixelsize = (byte)((rgba ? 4 : 3) * (info.IndexMapIs16Bit ? 2 : 1));

        bool success = false;
        var vmil = new VectorMapIndexList(info, pixelsize);
        lock (lock_api)
        {
            success = _G3DGetVectorMapIndexListA(
                handle_vectormap, repition, info.Width, info.Height,
                vmil.Indices, pixelsize, info.Width * pixelsize, 1,
                buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length
            ) != 0;
        }

        if (!success)
        {
            Debug.Log("ERROR|G3DGetVectorMapIndexListA: " + ErrorMessage);
            return null;
        }
        else
        {
            return vmil;
        }
    }

#endregion


#region vectormap_data

    public class VectorMapData
    {
        public float[] PositionMap;
        public float[] VectorMap;
        public VectorMapData(uint positionMapSize, uint vectorMapSize)
        {
            PositionMap = new float[positionMapSize];
            VectorMap = new float[vectorMapSize];
        }
    }
    private delegate uint f_G3DGetVectorMapDataA(
        IntPtr Source,              // view map source
        float[] PositionMap,        // position map (pointer to singles)
        uint PositionMapSize,       // size of position map (number of values; must be equal to size used in map)
        float[] VectorMap,          // vector map (pointer to singles)
        uint VectorMapSize,         // size of vector map (number of values; must be equal to size used in map)
        byte[] ErrorMessage,        // space for error details
        uint ErrorMessageMaxChars   // number of characters (including final zero) for error details
    );
    private static f_G3DGetVectorMapDataA? _G3DGetVectorMapDataA;
    public static VectorMapData G3DGetVectorMapDataA(IntPtr handle_vectormap, VectorMapInfo info)
    {
        if (_G3DGetVectorMapDataA == null) return null;
        if (handle_vectormap == IntPtr.Zero) return null;

        bool success = false;
        var vmd = new VectorMapData(info.PositionMapSize, info.VectorMapSize);
        lock (lock_api)
        {
            success = _G3DGetVectorMapDataA(
                handle_vectormap, vmd.PositionMap, (uint)vmd.PositionMap.Length, vmd.VectorMap, (uint)vmd.VectorMap.Length, buffer_ErrorMessage, (uint)buffer_ErrorMessage.Length
            ) != 0;
        }
        if (!success) Debug.Log("ERROR|G3DGetVectorMapData: " + ErrorMessage);
        return success ? vmd : null;
    }

#endregion


#region vectormap data free

    private delegate void f_G3DFreeVectorMapData(IntPtr VectorMap);
    private static f_G3DFreeVectorMapData? _G3DFreeVectorMapData;
    public static void G3DFreeVectorMapData(IntPtr handle_vectormap)
    {
        if (_G3DFreeVectorMapData == null) return;
        if (handle_vectormap == IntPtr.Zero) return;

        lock (lock_api)
        {
            _G3DFreeVectorMapData(handle_vectormap);
        }
    }

#endregion


#endregion


#endregion


#region observers

    private class LoopThread
    {
        private bool running = false;
        private Thread? th = null;

        public void Start(Action loopCode, int ms)
        {
            running = true;
            th = new Thread(() => {
                try
                {
                    while (running)
                    {
                        loopCode();
                        if (ms != 0) Thread.Sleep(ms);
                    }
                }
                catch (ThreadInterruptedException tiex) { }
            });
            th.Start();
        }

        public void Stop()
        {
            //try to kill the thread somewhat gracefully
            running = false;
            th?.Interrupt();
            th?.Join();
            th = null;
        }
    }

    //state
    private static LoopThread? thread_observe_state = null;
    public delegate void observe_state_callback(TG3DHeadTrackingStateHeaderV1? data);
    public static void observe_state(observe_state_callback callback, int ms)
    {
        if (callback == null || ms < 0) return;

        observe_state_stop();
        thread_observe_state = new LoopThread();
        thread_observe_state.Start(() => callback.Invoke(G3DHeadTrackingGetStateA()), ms);
    }
    public static void observe_state_stop()
    {
        thread_observe_state?.Stop();
    }

#endregion

#region utility

    //message buffer
    private static byte[] buffer_ErrorMessage = new byte[1024];
    private static string ErrorMessage
    {
        get
        {
            return Encoding.ASCII.GetString(buffer_ErrorMessage);
        }
    }

    public static bool detectMonitorChange()
    {
        if (handle_window == IntPtr.Zero) return false;

        var ptr = MonitorFromWindow(handle_window, MonitorFromWindowFlags.MONITOR_DEFAULTTONEAREST);

        bool changed = ptr != handle_monitor;
        if(changed)
        {
            handle_monitor = ptr;
            currentMonitorInfo = G3DGetMonitorInfoByHandle(handle_monitor);
        }
        return changed;
    }


    //WINAPI
    [DllImport("kernel32.dll")]
    private static extern IntPtr LoadLibrary(string dllToLoad);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

    [DllImport("kernel32.dll")]
    private static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("user32.dll", EntryPoint = "MonitorFromWindow", CharSet = CharSet.Unicode)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, MonitorFromWindowFlags dwFlags);

    public enum MonitorFromWindowFlags
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }



    private static T ExtractFunction<T>(IntPtr dll, string name) where T : Delegate
    {
        var handle_function = GetProcAddress(dll, name);
        if (handle_function == IntPtr.Zero)
        {
            Debug.LogError($"failed to load {name} from DLL.");
            return null;
        }
        return (T)Marshal.GetDelegateForFunctionPointer(handle_function, typeof(T));
    }

#endregion

}
