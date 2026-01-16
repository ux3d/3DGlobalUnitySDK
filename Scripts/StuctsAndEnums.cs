using UnityEngine;

public struct HeadPosition
{
    public bool headDetected;
    public bool imagePosIsValid;
    public int imagePosX;
    public int imagePosY;
    public double worldPosX;
    public double worldPosY;
    public double worldPosZ;
}

public enum G3DCameraMode
{
    DIORAMA,
    MULTIVIEW
}

/// <summary>
/// This struct is used to store the shader parameter handles for the individual shader parameters.
/// Its members should always be updated when the G3DShaderParameters struct changes.
/// </summary>
struct ShaderHandles
{
    // Viewport properties
    public int leftViewportPosition; //< The left   position of the viewport in screen coordinates
    public int bottomViewportPosition; //< The bottom position of the viewport in screen coordinates

    // Monitor properties
    public int screenWidth; //< The screen width in pixels
    public int screenHeight; //< The screen height in pixels

    public int nativeViewCount;
    public int angleRatioNumerator;
    public int angleRatioDenominator;
    public int leftLensOrientation;
    public int BGRPixelLayout;

    public int mstart;
    public int showTestFrame;
    public int showTestStripe;
    public int testGapWidth;
    public int track;
    public int hqViewCount;
    public int hviews1;
    public int hviews2;
    public int blur;
    public int blackBorder;
    public int blackSpace;
    public int bls;
    public int ble;
    public int brs;
    public int bre;

    public int zCorrectionValue;
    public int zCompensationValue;
}

public struct PreviousValues
{
    public TextAsset calibrationFile;
    public G3DCameraMode mode;
    public float sceneScaleFactor;

    public float indexMapYoyoStart;
    public bool invertIndexMap;
    public bool invertIndexMapIndices;

    public void init()
    {
        calibrationFile = null;
        mode = G3DCameraMode.DIORAMA;
        sceneScaleFactor = 1.0f;

        indexMapYoyoStart = 0.0f;
        invertIndexMap = false;
        invertIndexMapIndices = false;
    }
}
