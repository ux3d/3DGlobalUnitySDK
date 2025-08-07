#ifndef G3DMONITORTYPES_H
#define G3DMONITORTYPES_H

#include <stdbool.h>
#include <stdint.h>

// alignment of view map
typedef enum G3DViewMapAlignment {
    vma_Default,  // use calculated view indizes (first left sub pixel use last view index (or 0, if view order is inverted)
    vma_DefinedZero,  // defined sub pixel will get view index 0
    vma_CompatibleDeprecated,  // compatible to deprecated algorithm (if screen is left, left pixel of last row has highest view number; if screen is right, left pixel of first row has highest view number)
    vma_CompatibleMPV  // compatible to mpv (left red subpixel in second row from top will use view 0; since there are multiple implementations, this may not work as used version of mpv; mpv use different calculation, if map is calculated shader!)
} G3DViewMapAlignment;

// zero sub pixel
typedef struct G3DViewMapZeroPixel {
    uint32_t x;
    uint32_t y;
    uint32_t z;
} G3DViewMapZeroPixel;

// pixel colors
typedef enum G3DPixelColorChannel {
    pc_Red,
    pc_Green,
    pc_Blue,
    pc_NumColors
} G3DPixelColorChannel;

// 2d point
typedef struct G3DPoint2DF {
    float x;
    float y;
    
    G3DPoint2DF()
    {
        x = 0; 
        y = 0;
    }

    G3DPoint2DF(float ax, float ay)
    {
        x = ax;
        y = ay;
    }
} G3DPoint2DF;

// 2d point for pixel
typedef struct G3DPixelPointF {
    G3DPoint2DF& operator[](G3DPixelColorChannel i) { return data[i]; }
    G3DPoint2DF data[pc_NumColors];
} G3DPixelPointF;

#endif // G3DMONITORTYPES_H
