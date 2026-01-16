int  nativeViewCount;      // Anzahl nativer Views
int  zwinkel;        // Winkelzähler
int  nwinkel;        // Winkelnenner
int  isleft;         // links(1) oder rechts(0) geneigtes Lentikular
int  test;           // Rot/Schwarz (1)ein, (0)aus
int  stest;          // Streifen Rot/Schwarz (1)ein, (0)aus
int  testgap;        // Breite der Lücke im Testbild
int  track;          // Trackingshift
int  mstart;         // Viewshift permanent Offset
int  hqview;         // hqViewCount
int  hviews1;          // hqview - 1
int  hviews2;       // hqview / 2

int  bls;            // black left start (start and end points of left and right "eye" window)
int  ble;         // black left end 
int  brs;          // black right start
int  bre;      // black right end 

int  s_height;       // screen height
int  v_pos_x;        // horizontal viewport position
int  v_pos_y;        // vertical viewport position
int  tvx;            // zCorrectionValue
int  zkom;           // zCompensationValue, kompensiert den Shift der durch die Z-Korrektur entsteht

// This shader was originally implemented for OpenGL, so we need to invert the y axis to make it work in Unity.
// to do this we need the actual viewport height
int viewportHeight;

int mirror; // 1: mirror from left to right, 0: no mirror

// unused parameters -> only here for so that this shader overlaps with the multiview shader
// amount of render targets
int cameraCount;
int isBGR; // 0 = RGB, 1 = BGR


// unused parameters
int  bborder;        // blackBorder schwarz verblendung zwischen den views?
int  bspace;         // blackSpace
int  s_width;        // screen width
int  blur;           // je größer der Wert umso mehr wird verwischt 0-1000 sinnvoll

float index_map[256];
int indexMapLength;

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 screenPos : SV_POSITION;
};

int viewOffset = 0;

int finalizeViewIndex(int viewIndex)
{
    int result = viewIndex + viewOffset;
    result = result % nativeViewCount;

    // apply index map
    result = clamp(result, 0, indexMapLength - 1);
    result = index_map[result];

    return result;
}

int3 getSubPixelViewIndices(float2 screenPos)
{
    int direction = isleft == 1 ? 1 : -1;
    uint view = uint(screenPos.x * 3.f + ((screenPos.y * (float(zwinkel) / float(nwinkel))) % float(nativeViewCount) * direction) + float(nativeViewCount)) + mstart;
    int3 viewIndices = int3(view, view, view);

    viewIndices += uint3(0 + (isBGR * 2), 1, 2 - (isBGR * 2));

    viewIndices.x = finalizeViewIndex(viewIndices.x);
    viewIndices.y = finalizeViewIndex(viewIndices.y);
    viewIndices.z = finalizeViewIndex(viewIndices.z);

    return viewIndices;
}