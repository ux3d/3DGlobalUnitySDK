uint  nativeViewCount;      // Anzahl nativer Views
uint  zwinkel;        // Winkelzähler
uint  nwinkel;        // Winkelnenner
int  isleft;         // links(1) oder rechts(0) geneigtes Lentikular
uint  test;           // Rot/Schwarz (1)ein, (0)aus
uint  stest;          // Streifen Rot/Schwarz (1)ein, (0)aus
uint  testgap;        // Breite der Lücke im Testbild
uint  track;          // Trackingshift
uint  mstart;         // Viewshift permanent Offset
uint  hqview;         // hqViewCount
uint  hviews1;          // hqview - 1
uint  hviews2;       // hqview / 2

uint  bls;            // black left start (start and end points of left and right "eye" window)
uint  ble;         // black left end 
uint  brs;          // black right start
uint  bre;      // black right end 

uint  s_height;       // screen height
uint  v_pos_x;        // horizontal viewport position
uint  v_pos_y;        // vertical viewport position
uint  tvx;            // zCorrectionValue
uint  zkom;           // zCompensationValue, kompensiert den Shift der durch die Z-Korrektur entsteht

// This shader was originally implemented for OpenGL, so we need to invert the y axis to make it work in Unity.
// to do this we need the actual viewport height
uint viewportHeight;

int mirror; // 1: mirror from left to right, 0: no mirror

// unused parameters -> only here for so that this shader overlaps with the multiview shader
// amount of render targets
uint cameraCount;
uint isBGR; // 0 = RGB, 1 = BGR


// unused parameters
uint  bborder;        // blackBorder schwarz verblendung zwischen den views?
uint  bspace;         // blackSpace
uint  s_width;        // screen width
uint  blur;           // je größer der Wert umso mehr wird verwischt 0-1000 sinnvoll

uint use_hq_views; // 1: use hq views, 0: use native views

float index_map[256];
uint indexMapLength;

// used for debug grid rendering
int shouldRenderMosaic;
// these two variaboles (mosaic_columns and mosaic_rows) can be used for rendering a mosaic grid for debugging purposes, and rendering mosaic videos.
uint mosaic_rows = 1; // number of rows in the mosaic
uint mosaic_columns = 1; // number of columns in the mosaic

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 screenPos : SV_POSITION;
};

uint viewOffset = 0;

uint finalizeViewIndex(uint viewIndex)
{
    uint result = viewIndex + viewOffset;

    if(use_hq_views == 1)
    {
        // for hq views we need to wrap around the number of hq views
        result = result % hqview;
    }
    else
    {
        // for native views we need to wrap around the number of native views
        result = result % nativeViewCount;
    }

    // apply index map
    result = clamp(result, 0, indexMapLength - 1);
    result = index_map[result];

    return result;
}

int3 getSubPixelViewIndices(float2 screenPos)
{
    int vc = nativeViewCount;
    float angle = float(zwinkel) / float(nwinkel);
    int direction = isleft == 1 ? 1 : -1;

    uint view = uint(screenPos.x * 3.f + ((screenPos.y * angle) % float(nativeViewCount) * direction) + float(nativeViewCount)) + mstart;
    int3 viewIndices = int3(view, view, view);

    viewIndices += uint3(0 + (isBGR * 2), 1, 2 - (isBGR * 2));

    viewIndices.x = finalizeViewIndex(viewIndices.x);
    viewIndices.y = finalizeViewIndex(viewIndices.y);
    viewIndices.z = finalizeViewIndex(viewIndices.z);

    return viewIndices;
}

float customMod(float x, float y)
{
    return x - y * floor(x / y);
}

int3 getHQViewIndices(float2 screenPos)
{
    float numHQViews = float(hqview);

    int direction = isleft == 1 ? 1 : -1;
    uint vc = nativeViewCount * nwinkel;
    int stride = nwinkel;
    float angle = float(zwinkel);

    int startIndex = int(float(screenPos.y) * angle + 0.5) * direction + screenPos.y * vc + screenPos.x * 3 * stride;
    int3 viewIndices = int3(startIndex, startIndex, startIndex);
    viewIndices += int3(0, stride, stride + stride);
    viewIndices = (vc-1) - (viewIndices % vc);

    viewIndices.x = finalizeViewIndex(viewIndices.x);
    viewIndices.y = finalizeViewIndex(viewIndices.y);
    viewIndices.z = finalizeViewIndex(viewIndices.z);

    return viewIndices;
}

int map(int x, int in_min, int in_max, int out_min, int out_max)
{
    if (in_max == in_min)
    {
        return out_min;
    }
    // use rounded float division instead of truncating integer division so small
    // output ranges (e.g. a 2x1 mosaic grid) still distribute across all buckets
    // instead of everything truncating down to the same bucket.
    float t = float(x - in_min) / float(in_max - in_min);
    return int(round(out_min + t * float(out_max - out_min)));
}

// the text coords of the original left and right view are from 0 - 1
// the tex coords of the texel we are currently rendering are also from 0 - 1
// but we want to create a grid of views, so we need to transform the tex coords
// to the grid size.
// basically we want to figure out in which grid cell the current texel is, then convert the texel coords to the grid cell coords.
// example assuming a grid size of 3x3:
// original tex coords: 0.8, 0.5
// step 1: transform the tex coords to the grid size by multiplying with grid size
//    -> e.g. original x coord 0.8 turns to 0.8 * 3 = 2.4
// step 2: figure out the grid cell by taking the integer part of the transformed tex coords
//    -> e.g. 2.4 turns to 2
// step 3: subtract the integer part from the transformed tex coords to get the texel coords in the grid cell
//   -> e.g. 2.4 - 2 = 0.4 -> final texel coords in the grid cell are 0.4, 0.5
float2 getCellRelativeUVCoords(float2 uv, int2 gridSize) {
    float x = uv.x * float(gridSize.x);
    float y = uv.y * float(gridSize.y);

    int cellX = int(x);
    int cellY = int(y);

    x = x - cellX;
    y = y - cellY;

    return float2(x, y);
}

float getCellIndex(float2 uv, int2 gridSize) {
    uv.y = 1.0 - uv.y; // invert y axis to have coordinate cell coords start from top left corner
    float x = uv.x * float(gridSize.x);
    float y = uv.y * float(gridSize.y);

    uint cellX = int(x);
    uint cellY = int(y);

    uint cellIndex = cellX + cellY * gridSize.x;
    uint totalCells = gridSize.x * gridSize.y;

    return float(cellIndex);
}