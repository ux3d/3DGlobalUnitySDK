using UnityEngine;

namespace G3D
{
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

    public enum G3DPlacementMode
    {
        FOVBased,
        VirtualWindow
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

        public void init()
        {
            leftViewportPosition = Shader.PropertyToID("viewport_pos_x");
            bottomViewportPosition = Shader.PropertyToID("viewport_pos_y");
            screenWidth = Shader.PropertyToID("screen_width");
            screenHeight = Shader.PropertyToID("screen_height");
            nativeViewCount = Shader.PropertyToID("nativeViewCount");
            angleRatioNumerator = Shader.PropertyToID("zwinkel");
            angleRatioDenominator = Shader.PropertyToID("nwinkel");
            leftLensOrientation = Shader.PropertyToID("isleft");
            BGRPixelLayout = Shader.PropertyToID("isBGR");
            mstart = Shader.PropertyToID("mstart");
            showTestFrame = Shader.PropertyToID("test");
            showTestStripe = Shader.PropertyToID("stest");
            testGapWidth = Shader.PropertyToID("testgap");
            track = Shader.PropertyToID("track");
            hqViewCount = Shader.PropertyToID("hqview");
            hviews1 = Shader.PropertyToID("hviews1");
            hviews2 = Shader.PropertyToID("hviews2");
            blur = Shader.PropertyToID("blur");
            blackBorder = Shader.PropertyToID("bborder");
            blackSpace = Shader.PropertyToID("bspace");
            bls = Shader.PropertyToID("bls");
            ble = Shader.PropertyToID("ble");
            brs = Shader.PropertyToID("brs");
            bre = Shader.PropertyToID("bre");
            zCorrectionValue = Shader.PropertyToID("tvx");
            zCompensationValue = Shader.PropertyToID("zkom");
        }
    }
}
