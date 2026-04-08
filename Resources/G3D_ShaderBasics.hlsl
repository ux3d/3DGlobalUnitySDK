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