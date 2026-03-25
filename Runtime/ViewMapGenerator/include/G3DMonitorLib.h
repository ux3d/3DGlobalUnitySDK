#ifndef VIEWMAP_GENERATOR_H
#define VIEWMAP_GENERATOR_H

#if defined(_WIN32) && defined(VIEWMAP_GENERATOR_DLL)
#ifdef viewmap_generator_EXPORTS
#define VIEWMAP_GENERATOR_API __declspec(dllexport)
#else
#define VIEWMAP_GENERATOR_API __declspec(dllimport)
#endif
#else
#define VIEWMAP_GENERATOR_API
#endif

#include "G3DMonitorTypes.h"
#include <stdint.h>
#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif
    typedef enum G3DMonitor_Error {
        G3DMONITOR_SUCCESS,  // no error
        G3DMONITOR_ERROR,  // general error, call Error_GetLastError for details
        G3DMONITOR_INVALID_PARAMETER,  // invalid parameter (e.g. invalid instance)
        G3DMONITOR_BUFFER_TOO_SMALL,  // buffer is too small
        G3DMONITOR_NOT_IMPLEMENTED,  // function is not implemented
    } G3DMonitor_Error;

#define ERRORINTRO \
	try	\
	{ 

#define ERROROUTRO \
		return G3DMONITOR_SUCCESS; \
	} \
	catch (const std::exception& e) \
	{ \
		Error_SetLastError(e.what()); \
	} \
	catch (...) \
	{ \
		Error_SetLastError("Unexpected exception"); \
	} \
	return G3DMONITOR_ERROR;

#define G3DMONITOR_CALLTYPE /* default */

    // error handling
    VIEWMAP_GENERATOR_API void G3DMONITOR_CALLTYPE Error_SetLastError(const char* ErrorText);
    VIEWMAP_GENERATOR_API bool G3DMONITOR_CALLTYPE Error_GetLastError(char* ErrorText, size_t* Size);

    // monitor instance handling
    VIEWMAP_GENERATOR_API G3DMonitor_Error G3DMONITOR_CALLTYPE G3DMonitor_Create(
        uint32_t PixelCountX,  // number of pixels in x (used for full view map dimension)
        uint32_t PixelCountY,  // number of pixels in y (used for full view map dimension)
        uint16_t ViewCount,  // view count (if 0, 3d is not supported)
        uint32_t LensWidth,  // lens width (in number of pixel)
        int32_t LensAngleCounter,  // angle counter (including sign)
        bool ViewOrderInverted,  // if view order is inverted (left and right mirrored)
        bool Rotated,  // if monitor is rotated by 90° (not supported by deprecated algorithm)
        bool FullPixel,  // if only full pixel available (lens is always over full pixel; no sub pixel addressable)
        bool BGRMode,  // if red and blue channel are exchanged (display rotated by 180°)
        void** Monitor);
    VIEWMAP_GENERATOR_API G3DMonitor_Error G3DMONITOR_CALLTYPE G3DMonitor_Destroy(void* Monitor);

    // calculate view map from given parameters
    // is result = G3DMONITOR_SUCCESS, view map is valid
    // do not forget to free memory of view map, if result is G3DMONITOR_SUCCESS (calling G3DMonitor_FreeViewMap)
    VIEWMAP_GENERATOR_API G3DMonitor_Error G3DMONITOR_CALLTYPE G3DMonitor_BuildViewMap(
        void* Monitor,  // monitor instance (created with G3DMonitor_Create)
        bool HQMode,  // hq mode (use all views up to 250; in non-hq mode, number of views is limited to number of base views)
        G3DViewMapAlignment ViewAlignment,  // view alignment
        G3DViewMapZeroPixel ViewZeroPixel,  // zero sub pixel, if view alignment is vma_DefinedZero; z is color to use (0=red, 1=green, 2=blue)
        bool EnlargeViewMapX,  // if to repeat shrinked viewmap to full monitor resolution
        bool EnlargeViewMapY,  // if to repeat shrinked viewmap to full monitor resolution
        bool AViewMapIsBGR,  // if colors aligned blue, green, red (instead red, green, blue; e.g. used for windows bitmap)
        bool AViewMapInvertY,  // if y order is inverted (bottom line is last in memory (like windows bitmap))
        int8_t AViewMapLinePadding,  // line padding (<0=automatic for windows bitmap, 0=off, 1..n = define number of fill bytes)
        uint8_t* ResultViewCount,  // number of views used in view map (will be <= 250 in any case since 251..255 are reserved)
        uint32_t* ResultViewMapWidth,  // dimension of viewmap in pixel in x 
        uint32_t* ResultViewMapHeight,  // dimension of viewmap in pixel in y
        uint32_t* ResultViewMapSize,  // size of view map (in bytes)
        uint32_t* ResultViewMapScanLineSize,  // size of single line of view map (in bytes)
        uint8_t** ResultViewMap  // result view map (memory is allocated and filled automatically; memory size of view map is ResultViewMapWidth*ResultViewMapHeight*3 (format is RGB))
    );

    // destroy view map memory and set pointer null
    VIEWMAP_GENERATOR_API G3DMonitor_Error G3DMONITOR_CALLTYPE G3DMonitor_FreeViewMap(uint8_t** ViewMap);

#ifdef __cplusplus
}
#endif

#endif // VIEWMAP_GENERATOR_H
