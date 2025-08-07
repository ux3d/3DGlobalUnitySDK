using System;
using System.Runtime.InteropServices;
using UnityEngine;

internal enum G3DViewMapAlignment
{
    vma_Default = 0, // use calculated view indizes (first left sub pixel use last view index (or 0, if view order is inverted)
    vma_DefinedZero = 1, // defined sub pixel will get view index 0
    vma_CompatibleDeprecated = 2, // compatible to deprecated algorithm (if screen is left, left pixel of last row has highest view number; if screen is right, left pixel of first row has highest view number)
    vma_CompatibleMPV = 3 // compatible to mpv (left red subpixel in second row from top will use view 0; since there are multiple implementations, this may not work as used version of mpv; mpv use different calculation, if map is calculated shader!)
}

internal struct G3DViewMapZeroPixel
{
    public uint x;
    public uint y;
    public uint z;
}

internal enum G3DPixelColorChannel
{
    pc_Red = 0,
    pc_Green = 1,
    pc_Blue = 2,
    pc_NumColors = 3
}

internal struct G3DPoint2DF
{
    public float x;
    public float y;

    public G3DPoint2DF(float ax, float ay)
    {
        x = ax;
        y = ay;
    }
}

internal struct G3DPixelPointF
{
    public G3DPoint2DF[] data;
}

internal enum G3DMonitor_Error
{
    G3DMONITOR_SUCCESS, // no error
    G3DMONITOR_ERROR, // general error, call Error_GetLastError for details
    G3DMONITOR_INVALID_PARAMETER, // invalid parameter (e.g. invalid instance)
    G3DMONITOR_BUFFER_TOO_SMALL, // buffer is too small
    G3DMONITOR_NOT_IMPLEMENTED, // function is not implemented
}

/// <summary>
/// does not upload the texture to the GPU.
/// </summary>
public sealed class ViewmapGeneratorInterface
{
    public static Texture2D getViewMap(
        uint PixelCountX, // number of pixels in x (used for full view map dimension)
        uint PixelCountY, // number of pixels in y (used for full view map dimension)
        uint ViewCount, // view count (if 0, 3d is not supported)
        uint LensWidth, // lens width (in number of pixel)
        int LensAngleCounter, // angle counter (including sign)
        bool ViewOrderInverted, // if view order is inverted (left and right mirrored)
        bool Rotated, // if monitor is rotated by 90° (not supported by deprecated algorithm)
        bool FullPixel, // if only full pixel available (lens is always over full pixel; no sub pixel addressable)
        bool BGRMode
    )
    {
        // first create the monitor instance
        IntPtr monitor;
        G3DMonitor_Error result = ViewmapGeneratorCpp.G3DMonitor_Create(
            PixelCountX,
            PixelCountY,
            ViewCount,
            LensWidth,
            LensAngleCounter,
            ViewOrderInverted,
            Rotated,
            FullPixel,
            BGRMode,
            out monitor
        );
        if (result != G3DMonitor_Error.G3DMONITOR_SUCCESS)
        {
            Debug.LogError("Failed to create monitor instance: " + result);
            return null;
        }

        // now build the view map
        IntPtr resultViewMapPtr;
        byte ResultViewCount; // number of views used in view map (will be <= 250 in any case since 251..255 are reserved)
        uint ResultViewMapWidth; // dimension of viewmap in pixel in x
        uint ResultViewMapHeight; // dimension of viewmap in pixel in y
        uint ResultViewMapSize; // size of view map (in bytes)
        uint ResultViewMapScanLineSize;
        G3DMonitor_Error buildViewmapResult = ViewmapGeneratorCpp.G3DMonitor_BuildViewMap(
            monitor,
            false,
            G3DViewMapAlignment.vma_Default,
            new G3DViewMapZeroPixel
            {
                x = 0,
                y = 0,
                z = 0
            },
            true,
            true,
            false,
            false,
            0,
            out ResultViewCount,
            out ResultViewMapWidth,
            out ResultViewMapHeight,
            out ResultViewMapSize,
            out ResultViewMapScanLineSize,
            out resultViewMapPtr
        );
        byte[] managedArray = new byte[ResultViewMapSize];
        Marshal.Copy(resultViewMapPtr, managedArray, 0, (int)ResultViewMapSize);

        G3DMonitor_Error freeMapResult = ViewmapGeneratorCpp.G3DMonitor_FreeViewMap(
            resultViewMapPtr
        );

        // copy the result view map to a byte array

        // free view map memory

        // free monitor instance
        G3DMonitor_Error destroyResult = ViewmapGeneratorCpp.G3DMonitor_Destroy(monitor);
        if (destroyResult != G3DMonitor_Error.G3DMONITOR_SUCCESS)
        {
            Debug.LogError("Failed to destroy monitor instance: " + destroyResult);
            return null;
        }

        Texture2D texture = new Texture2D(
            (int)ResultViewMapWidth,
            (int)ResultViewMapHeight,
            TextureFormat.RGB24,
            false
        );
        // for (int x = 0; x < ResultViewMapWidth; x++)
        // {
        //     for (int y = 0; y < ResultViewMapHeight; y++)
        //     {
        //         int index = (y * (int)ResultViewMapWidth + x) * 3;
        //         Color color = new Color(
        //             managedArray[index] / 255f,
        //             managedArray[index + 1] / 255f,
        //             managedArray[index + 2] / 255f
        //         );
        //         texture.SetPixel(x, y, color);
        //     }
        // }
        texture.LoadRawTextureData(managedArray);
        return texture;
    }
}

/// <summary>
/// This class provides the raw C interface to the G3D Universal Head Tracking Library.
/// </summary>
internal static class ViewmapGeneratorCpp
{
    //function definitions
    [DllImport(
        "viewmap_generator.dll",
        EntryPoint = "G3DMonitor_BuildViewMap",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern G3DMonitor_Error G3DMonitor_BuildViewMap(
        IntPtr Monitor, // monitor instance (created with G3DMonitor_Create)
        bool HQMode, // hq mode (use all views up to 250; in non-hq mode, number of views is limited to number of base views)
        G3DViewMapAlignment ViewAlignment, // view alignment
        G3DViewMapZeroPixel ViewZeroPixel, // zero sub pixel, if view alignment is vma_DefinedZero; z is color to use (0=red, 1=green, 2=blue)
        bool EnlargeViewMapX, // if to repeat shrinked viewmap to full monitor resolution
        bool EnlargeViewMapY, // if to repeat shrinked viewmap to full monitor resolution
        bool AViewMapIsBGR, // if colors aligned blue, green, red (instead red, green, blue; e.g. used for windows bitmap)
        bool AViewMapInvertY, // if y order is inverted (bottom line is last in memory (like windows bitmap))
        byte AViewMapLinePadding, // line padding (<0=automatic for windows bitmap, 0=off, 1..n = define number of fill bytes)
        out byte ResultViewCount, // number of views used in view map (will be <= 250 in any case since 251..255 are reserved)
        out uint ResultViewMapWidth, // dimension of viewmap in pixel in x
        out uint ResultViewMapHeight, // dimension of viewmap in pixel in y
        out uint ResultViewMapSize, // size of view map (in bytes)
        out uint ResultViewMapScanLineSize, // size of single line of view map (in bytes)
        out IntPtr ResultViewMap // result view map (memory is allocated and filled automatically; memory size of view map is ResultViewMapWidth*ResultViewMapHeight*3 (format is RGB))
    );

    [DllImport(
        "viewmap_generator.dll",
        EntryPoint = "G3DMonitor_FreeViewMap",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern G3DMonitor_Error G3DMonitor_FreeViewMap(in IntPtr ViewMap);

    [DllImport(
        "viewmap_generator.dll",
        EntryPoint = "G3DMonitor_Create",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern G3DMonitor_Error G3DMonitor_Create(
        uint PixelCountX, // number of pixels in x (used for full view map dimension)
        uint PixelCountY, // number of pixels in y (used for full view map dimension)
        uint ViewCount, // view count (if 0, 3d is not supported)
        uint LensWidth, // lens width (in number of pixel)
        int LensAngleCounter, // angle counter (including sign)
        [MarshalAs(UnmanagedType.U1)] bool ViewOrderInverted, // if view order is inverted (left and right mirrored)
        [MarshalAs(UnmanagedType.U1)] bool Rotated, // if monitor is rotated by 90° (not supported by deprecated algorithm)
        [MarshalAs(UnmanagedType.U1)] bool FullPixel, // if only full pixel available (lens is always over full pixel; no sub pixel addressable)
        [MarshalAs(UnmanagedType.U1)] bool BGRMode, // if red and blue channel are exchanged (display rotated by 180°)
        out IntPtr Monitor
    );

    [DllImport(
        "viewmap_generator.dll",
        EntryPoint = "G3DMonitor_Destroy",
        CallingConvention = CallingConvention.Cdecl
    )]
    public static extern G3DMonitor_Error G3DMonitor_Destroy(IntPtr Monitor);
}
