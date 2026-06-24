using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace G3D
{
    public static class HeadTrackingSDK
    {
        #region types
        //-----------------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct HeadPosition
        {
            public float world_x;
            public float world_y;
            public float world_z;

            public int image_x;
            public int image_y;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct MonitorParameters
        {
            public int angleNumerator;
            public int angleDenominator;
            public int viewcount;
            public int base_offset;
            public bool left; // lense pattern falls left or right
            public int bgr;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 8)]
        public struct RenderParameters
        {
            public int viewOffset;
            public int showTestFrame;
            public int showTestStripe;
            public int testGapWidth;
            public int track;
            public int hviews1;
            public int hviews2;
            public int blur;
            public int blackBorder;
            public int blackSpace;
            public int bls;
            public int ble;
            public int brs;
            public int bre;
            public int screenWidth;
            public int screenHeight;
            public int leftViewportPosition;
            public int bottomViewportPosition;
            public int zCorrectionValue;
            public int zCompensationValue;
            public int zCorrectionIndex;

            public int basicWorkingDistanceMM;
            public double zCorrectionFunctionFactor;
            public double zCorrectionFunctionBase;
            public int zoneWidthAtBaseDistance;
            public int zCompValueAtMinWorkingDistance;
            public int zCompValueAtMaxWorkingDistance;
        }

        public enum RenderMode
        {
            RENDERMODE_AUTO = 0, // uses ztracking for stereo, algo for anything else
            RENDERMODE_SINGLEVIEW = 1, // only renders the first view
            RENDERMODE_ALGO = 2, // raw rendering, with or without tracking, but no compensation for users distance to the monitor
            RENDERMODE_ZTRACKING = 3 // tracked, takes the users distance to the monitor into account to improve results (only stereo)
        };

        public enum HeadTrackingUserPosCodes
        {
            USER_POS_NO = 0,
            USER_POS_VALID = 1,
            USER_POS_WARN_LEFT = 2,
            USER_POS_WARN_RIGHT = 3,
            USER_POS_WARN_FRONT = 4,
            USER_POS_WARN_BACK = 5,
            USER_POS_WARN_UP = 6,
            USER_POS_WARN_DOWN = 7,
            USER_POS_ERR_LEFT = 8,
            USER_POS_ERR_RIGHT = 9,
            USER_POS_ERR_FRONT = 10,
            USER_POS_ERR_BACK = 11,
            USER_POS_ERR_UP = 12,
            USER_POS_ERR_DOWN = 13
        }

        //-----------------------------------------------------------------------------------------
        #endregion

        #region delegates
        //-----------------------------------------------------------------------------------------

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_get_version(
            out int major,
            out int minor,
            out int patch,
            out int rev
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_init([MarshalAs(UnmanagedType.LPStr)] string path);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_ht_fin();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_ht_start();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_ht_stop();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_is_initialized();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_is_connected();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_has_device();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate HeadPosition PFN_g3d_sdk_ht_get_head_world_pos();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr PFN_g3d_sdk_ht_get_log_file_path();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate MonitorParameters PFN_g3d_sdk_ht_get_monitor_parameters();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate RenderParameters PFN_g3d_sdk_ht_get_render_parameters();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_ht_get_available_predictions(
            out IntPtr predictions,
            out int length
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_set_prediction(
            [MarshalAs(UnmanagedType.LPStr)] string prediction
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_set_prediction_latency(double latencyMs);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PFN_g3d_sdk_ht_get_available_filters(
            out IntPtr filters,
            out int length
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_set_filter(
            [MarshalAs(UnmanagedType.LPStr)] string filter
        );

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool PFN_g3d_sdk_ht_set_filter_size(int filterSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr PFN_g3d_sdk_ht_get_device_name();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate HeadTrackingUserPosCodes PFN_g3d_sdk_ht_get_user_guidance();

        private struct FT_G3D_SDK_TYPE
        {
            // general
            public PFN_g3d_sdk_get_version get_version;

            // headtracking
            public PFN_g3d_sdk_ht_init ht_init;
            public PFN_g3d_sdk_ht_fin ht_fin;
            public PFN_g3d_sdk_ht_start ht_start;
            public PFN_g3d_sdk_ht_stop ht_stop;
            public PFN_g3d_sdk_ht_is_initialized ht_is_initialized;
            public PFN_g3d_sdk_ht_is_connected ht_is_connected;
            public PFN_g3d_sdk_ht_has_device ht_has_device;
            public PFN_g3d_sdk_ht_get_head_world_pos ht_get_head_world_pos;
            public PFN_g3d_sdk_ht_get_log_file_path ht_get_log_file_path;
            public PFN_g3d_sdk_ht_get_monitor_parameters ht_get_monitor_parameters;
            public PFN_g3d_sdk_ht_get_render_parameters ht_get_render_parameters;
            public PFN_g3d_sdk_ht_get_available_predictions ht_get_available_predictions;
            public PFN_g3d_sdk_ht_set_prediction ht_set_prediction;
            public PFN_g3d_sdk_ht_set_prediction_latency ht_set_prediction_latency;
            public PFN_g3d_sdk_ht_get_available_filters ht_get_available_filters;
            public PFN_g3d_sdk_ht_set_filter ht_set_filter;
            public PFN_g3d_sdk_ht_set_filter_size ht_set_filter_size;
            public PFN_g3d_sdk_ht_get_device_name ht_get_device_name;
            public PFN_g3d_sdk_ht_get_user_guidance ht_get_user_guidance;
        };

        private static FT_G3D_SDK_TYPE ft_g3d_sdk;

        //-----------------------------------------------------------------------------------------
        #endregion

        //-----------------------------------------------------------------------------------------

        #region lib
        //-----------------------------------------------------------------------------------------

        private static IntPtr G3D_SDK_LIB_HANDLE;

        public static bool load_from(string path)
        {
            G3D_SDK_LIB_HANDLE = LoadLibrary(path);
            if (G3D_SDK_LIB_HANDLE == IntPtr.Zero)
                return false;

            try
            {
                ft_g3d_sdk.get_version = LoadFunction<PFN_g3d_sdk_get_version>(
                    "g3d_sdk_get_version"
                );

                ft_g3d_sdk.ht_init = LoadFunction<PFN_g3d_sdk_ht_init>("g3d_sdk_ht_init");
                ft_g3d_sdk.ht_fin = LoadFunction<PFN_g3d_sdk_ht_fin>("g3d_sdk_ht_fin");
                ft_g3d_sdk.ht_start = LoadFunction<PFN_g3d_sdk_ht_start>("g3d_sdk_ht_start");
                ft_g3d_sdk.ht_stop = LoadFunction<PFN_g3d_sdk_ht_stop>("g3d_sdk_ht_stop");
                ft_g3d_sdk.ht_is_initialized = LoadFunction<PFN_g3d_sdk_ht_is_initialized>(
                    "g3d_sdk_ht_is_initialized"
                );
                ft_g3d_sdk.ht_is_connected = LoadFunction<PFN_g3d_sdk_ht_is_connected>(
                    "g3d_sdk_ht_is_connected"
                );
                ft_g3d_sdk.ht_has_device = LoadFunction<PFN_g3d_sdk_ht_has_device>(
                    "g3d_sdk_ht_has_device"
                );
                ft_g3d_sdk.ht_get_head_world_pos = LoadFunction<PFN_g3d_sdk_ht_get_head_world_pos>(
                    "g3d_sdk_ht_get_head_world_pos"
                );
                ft_g3d_sdk.ht_get_log_file_path = LoadFunction<PFN_g3d_sdk_ht_get_log_file_path>(
                    "g3d_sdk_ht_get_log_file_path"
                );
                ft_g3d_sdk.ht_get_monitor_parameters =
                    LoadFunction<PFN_g3d_sdk_ht_get_monitor_parameters>(
                        "g3d_sdk_ht_get_monitor_parameters"
                    );
                ft_g3d_sdk.ht_get_render_parameters =
                    LoadFunction<PFN_g3d_sdk_ht_get_render_parameters>(
                        "g3d_sdk_ht_get_render_parameters"
                    );
                ft_g3d_sdk.ht_get_available_predictions =
                    LoadFunction<PFN_g3d_sdk_ht_get_available_predictions>(
                        "g3d_sdk_ht_get_available_predictions"
                    );
                ft_g3d_sdk.ht_set_prediction = LoadFunction<PFN_g3d_sdk_ht_set_prediction>(
                    "g3d_sdk_ht_set_prediction"
                );
                ft_g3d_sdk.ht_set_prediction_latency =
                    LoadFunction<PFN_g3d_sdk_ht_set_prediction_latency>(
                        "g3d_sdk_ht_set_prediction_latency"
                    );
                ft_g3d_sdk.ht_get_available_filters =
                    LoadFunction<PFN_g3d_sdk_ht_get_available_filters>(
                        "g3d_sdk_ht_get_available_filters"
                    );
                ft_g3d_sdk.ht_set_filter = LoadFunction<PFN_g3d_sdk_ht_set_filter>(
                    "g3d_sdk_ht_set_filter"
                );
                ft_g3d_sdk.ht_set_filter_size = LoadFunction<PFN_g3d_sdk_ht_set_filter_size>(
                    "g3d_sdk_ht_set_filter_size"
                );
                ft_g3d_sdk.ht_get_device_name = LoadFunction<PFN_g3d_sdk_ht_get_device_name>(
                    "g3d_sdk_ht_get_device_name"
                );
                ft_g3d_sdk.ht_get_user_guidance = LoadFunction<PFN_g3d_sdk_ht_get_user_guidance>(
                    "g3d_sdk_ht_get_user_guidance"
                );
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static bool load()
        {
            return load_from("G3DSDK64.dll");
        }

        public static bool loaded()
        {
            return G3D_SDK_LIB_HANDLE != IntPtr.Zero;
        }

        public static void unload()
        {
            if (loaded())
            {
                FreeLibrary(G3D_SDK_LIB_HANDLE);
                G3D_SDK_LIB_HANDLE = IntPtr.Zero;
                ft_g3d_sdk = default(FT_G3D_SDK_TYPE);
            }
        }

        private static T LoadFunction<T>(string functionName)
            where T : class
        {
            IntPtr procAddress = GetProcAddress(G3D_SDK_LIB_HANDLE, functionName);
            if (procAddress == IntPtr.Zero)
                throw new EntryPointNotFoundException("function not found: " + functionName);

            return (T)(object)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(T));
        }

        // Marshal.PtrToStringUTF8 is not available on Mono; this manual helper works everywhere.
        private static string PtrToStringUTF8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return "";
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0)
                len++;
            byte[] bytes = new byte[len];
            Marshal.Copy(ptr, bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        //-----------------------------------------------------------------------------------------
        #endregion

        //-----------------------------------------------------------------------------------------

        #region general
        //-----------------------------------------------------------------------------------------

        public static string get_version()
        {
            if (!loaded())
                return "";
            int major,
                minor,
                patch,
                rev;
            ft_g3d_sdk.get_version(out major, out minor, out patch, out rev);
            return major + "." + minor + "." + patch + "." + rev;
        }

        //-----------------------------------------------------------------------------------------
        #endregion

        #region headtracking
        //-----------------------------------------------------------------------------------------

        /// @brief initializes the headtracking
        ///
        /// call this before initializing the rendering
        /// the G3DHTService will be started here and run as a service in the background until the last client disconnects
        ///
        /// call this before initializing the rendering
        /// @param serviceDirectory the DIRECTORY containing the G3DHTService executable
        /// @return success
        public static bool ht_init_from_dir(string serviceDirectory)
        {
            if (!loaded())
                return false;
            return ft_g3d_sdk.ht_init(serviceDirectory);
        }

        /// @brief initializes the headtracking
        ///
        /// call this before initializing the rendering
        /// the G3DHTService will be started here and run as a service in the background until the last client disconnects
        ///
        /// this expects the G3DHTService executable to be in <currentdir>/G3DHTService
        /// @return success
        public static bool ht_init()
        {
            return ht_init_from_dir(Application.streamingAssetsPath + "/G3DHTService");
        }

        /// @brief finalizes the headtracking and disconnects from G3DHTService
        public static void ht_fin()
        {
            if (!loaded())
                return; // call load() first!
            ft_g3d_sdk.ht_fin();
        }

        /// @brief starts tracking
        ///
        /// this is only required if the render mode is not set to automatic
        public static void ht_start()
        {
            if (!loaded())
                return; // call load() first!
            ft_g3d_sdk.ht_start();
        }

        /// @brief stops tracking, but the service remains available
        public static void ht_stop()
        {
            if (!loaded())
                return; // call load() first!
            ft_g3d_sdk.ht_stop();
        }

        /// @return headtracking is initialized
        public static bool ht_is_initialized()
        {
            if (!loaded())
                return false; // call load() first!
            return ft_g3d_sdk.ht_is_initialized();
        }

        /// @return headtracking is connected to the service
        public static bool ht_is_connected()
        {
            if (!loaded())
                return false; // call load() first!
            return ft_g3d_sdk.ht_is_connected();
        }

        /// @return headtracking found a device
        public static bool ht_has_device()
        {
            if (!loaded())
                return false; // call load() first!
            return ft_g3d_sdk.ht_has_device();
        }

        /// @brief get the last user position relative to the camera
        ///
        /// if you are right in front of the camera, you are at x = 0
        /// @return
        public static HeadPosition ht_get_head_world_pos()
        {
            if (!loaded())
                return new HeadPosition(); // call load() first!
            return ft_g3d_sdk.ht_get_head_world_pos();
        }

        /// @brief for debugging purposes, the G3DHTService may have some useful information in the logs
        /// @return path to the log file
        public static string ht_get_log_file_path()
        {
            if (!loaded())
                return "sdk library not loaded"; // call load() first!
            IntPtr cStr = ft_g3d_sdk.ht_get_log_file_path();
            return PtrToStringUTF8(cStr);
        }

        /// @brief the headtracking may provide the current monitor parameters
        /// @return monitor parameters
        public static MonitorParameters ht_get_monitor_parameters()
        {
            if (!loaded())
                return new MonitorParameters(); // call load() first!
            return ft_g3d_sdk.ht_get_monitor_parameters();
        }

        /// @brief the headtracking may provide the current render parameters
        /// @return monitor parameters
        public static RenderParameters ht_get_render_parameters()
        {
            if (!loaded())
                return new RenderParameters(); // call load() first!
            return ft_g3d_sdk.ht_get_render_parameters();
        }

        /// @brief get the available predictions
        public static List<string> ht_get_available_predictions()
        {
            List<string> result = new List<string>();
            if (!loaded())
                return result;

            IntPtr arrayPtr;
            int length;
            ft_g3d_sdk.ht_get_available_predictions(out arrayPtr, out length);
            if (arrayPtr == IntPtr.Zero || length <= 0)
                return result;
            for (int i = 0; i < length; ++i)
            {
                IntPtr cStr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                string str = PtrToStringUTF8(cStr);
                if (str.Length > 0)
                    result.Add(str);
            }

            return result;
        }

        /// @brief set the prediction to be used
        ///
        /// get a list of available predictions by calling ht_get_available_predictions
        ///
        /// you can expect at least "NONE" and "default" to be available at all times
        /// @param prediction prediction name
        /// @return success
        public static bool ht_set_prediction(string prediction)
        {
            if (!loaded())
                return false;
            return ft_g3d_sdk.ht_set_prediction(prediction);
        }

        /// @brief set the prediction latency to be used
        ///
        /// the latency defines the point in the future for the prediction calculations.
        /// use this only if you expect a massive delay between drawing and actually display.
        /// @param latencyMs latency in ms
        /// @return success
        public static bool ht_set_prediction_latency(double latencyMs)
        {
            if (!loaded())
                return false;
            return ft_g3d_sdk.ht_set_prediction_latency(latencyMs);
        }

        /// @brief get the available filters
        public static List<string> ht_get_available_filters()
        {
            List<string> result = new List<string>();
            if (!loaded())
                return result;

            IntPtr arrayPtr;
            int length;
            ft_g3d_sdk.ht_get_available_filters(out arrayPtr, out length);
            if (arrayPtr == IntPtr.Zero || length <= 0)
                return result;
            for (int i = 0; i < length; ++i)
            {
                IntPtr cStr = Marshal.ReadIntPtr(arrayPtr, i * IntPtr.Size);
                string str = PtrToStringUTF8(cStr);
                if (str.Length > 0)
                    result.Add(str);
            }

            return result;
        }

        /// @brief set the filter to be used
        ///
        /// get a list of available filters by calling ht_get_available_filters
        ///
        /// you can expect at least "NONE" and "default" to be available at all times
        /// @param filter filter name
        /// @return success
        public static bool ht_set_filter(string filter)
        {
            if (!loaded())
                return false;
            return ft_g3d_sdk.ht_set_filter(filter);
        }

        /// @brief set the filter size to be used
        ///
        /// the filter size defines the number of prior samples used for filtering
        /// the filter will be applied on (predicted - if enabled) headtracking results
        /// @param size number of prior samples used for filtering
        /// @return success
        public static bool ht_set_filter_size(int size)
        {
            if (!loaded())
                return false;
            return ft_g3d_sdk.ht_set_filter_size(size);
        }

        /// @brief get the currently used device name (this also correlates to the name of the folder for calibration files)
        /// @return currently used headtracking device
        public static string ht_get_device_name()
        {
            if (!loaded())
                return "sdk library not loaded"; // call load() first!

            IntPtr cStr = ft_g3d_sdk.ht_get_device_name();
            return PtrToStringUTF8(cStr);
        }

        /// @brief user guidance helper
        /// @return value to guide the user into the optimal headtracking area
        public static HeadTrackingUserPosCodes ht_get_user_guidance()
        {
            if (!loaded())
                return HeadTrackingUserPosCodes.USER_POS_NO;
            return ft_g3d_sdk.ht_get_user_guidance();
        }

        //-----------------------------------------------------------------------------------------
        #endregion
    }

    //-----------------------------------------------------------------------------------------

    #region aux

    public static class HeadTrackingUserPosCodesExtensions
    {
        public static string ToDisplayString(this HeadTrackingSDK.HeadTrackingUserPosCodes code)
        {
            switch (code)
            {
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_NO:
                    return "USER_POS_NO";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_VALID:
                    return "USER_POS_VALID";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_LEFT:
                    return "USER_POS_WARN_LEFT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_RIGHT:
                    return "USER_POS_WARN_RIGHT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_FRONT:
                    return "USER_POS_WARN_FRONT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_BACK:
                    return "USER_POS_WARN_BACK";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_UP:
                    return "USER_POS_WARN_UP";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_WARN_DOWN:
                    return "USER_POS_WARN_DOWN";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_LEFT:
                    return "USER_POS_ERR_LEFT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_RIGHT:
                    return "USER_POS_ERR_RIGHT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_FRONT:
                    return "USER_POS_ERR_FRONT";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_BACK:
                    return "USER_POS_ERR_BACK";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_UP:
                    return "USER_POS_ERR_UP";
                case HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_ERR_DOWN:
                    return "USER_POS_ERR_DOWN";
                default:
                    return "WHAT?";
            }
        }
    }

    #endregion
}
