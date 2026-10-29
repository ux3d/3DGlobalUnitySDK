using System;
using UnityEngine;

namespace G3D
{
    public class ConfigCodeHandling
    {
        public struct DisplayConfig
        {
            public uint numberOfViews;
            public bool isBGR;
            public bool isLeft;
            public float lensOpeningAngleInDeg;
            public uint lensAngleNumerator;
            public uint lensAngleDenominator;
        }

        public static DisplayConfig ParseConfigCode(string configCode)
        {
            if (string.IsNullOrEmpty(configCode) || configCode.Length != 3)
            {
                throw new ArgumentException("Invalid config code format.");
            }

            // capitalize all letters to simplify parsing
            configCode = configCode.ToUpper();

            DisplayConfig config = new DisplayConfig();

            // The first two characters represent the RGB/BGR panel type and number of views
            string panelCode = configCode.Substring(0, 2);

            translateRGBPanelCode(panelCode, ref config);

            // The third character represents the lens angle and orientation
            string lensCode = configCode.Substring(2, 1);
            translateLensAngleCode(lensCode, ref config);

            return config;
        }

        private static void translateRGBPanelCode(string code, ref DisplayConfig config)
        {
            switch (code)
            {
                // rgb panels
                case "02":
                    config.isBGR = false;
                    config.numberOfViews = 2;
                    break;
                case "04":
                    config.isBGR = false;
                    config.numberOfViews = 4;
                    break;
                case "05":
                    config.isBGR = false;
                    config.numberOfViews = 5;
                    break;
                case "07":
                    config.isBGR = false;
                    config.numberOfViews = 7;
                    break;
                case "08":
                    config.isBGR = false;
                    config.numberOfViews = 8;
                    break;
                case "09":
                    config.isBGR = false;
                    config.numberOfViews = 9;
                    break;
                case "10":
                    config.isBGR = false;
                    config.numberOfViews = 10;
                    break;
                case "12":
                    config.isBGR = false;
                    config.numberOfViews = 12;
                    break;
                case "16":
                    config.isBGR = false;
                    config.numberOfViews = 16;
                    break;
                case "17":
                    config.isBGR = false;
                    config.numberOfViews = 17;
                    break;
                case "24":
                    config.isBGR = false;
                    config.numberOfViews = 24;
                    break;
                case "36":
                    config.isBGR = false;
                    config.numberOfViews = 36;
                    break;
                case "49":
                    config.isBGR = false;
                    config.numberOfViews = 49;
                    break;
                case "55":
                    config.isBGR = false;
                    config.numberOfViews = 55;
                    break;

                // bgr panels
                case "A2":
                    config.isBGR = true;
                    config.numberOfViews = 2;
                    break;
                case "A4":
                    config.isBGR = true;
                    config.numberOfViews = 4;
                    break;
                case "A5":
                    config.isBGR = true;
                    config.numberOfViews = 5;
                    break;
                case "A7":
                    config.isBGR = true;
                    config.numberOfViews = 7;
                    break;
                case "A8":
                    config.isBGR = true;
                    config.numberOfViews = 8;
                    break;
                case "A9":
                    config.isBGR = true;
                    config.numberOfViews = 9;
                    break;
                case "B0":
                    config.isBGR = true;
                    config.numberOfViews = 10;
                    break;
                case "B2":
                    config.isBGR = true;
                    config.numberOfViews = 12;
                    break;
                case "B6":
                    config.isBGR = true;
                    config.numberOfViews = 16;
                    break;
                case "B7":
                    config.isBGR = true;
                    config.numberOfViews = 17;
                    break;
                case "C4":
                    config.isBGR = true;
                    config.numberOfViews = 24;
                    break;
                case "D6":
                    config.isBGR = true;
                    config.numberOfViews = 36;
                    break;
                case "E9":
                    config.isBGR = true;
                    config.numberOfViews = 49;
                    break;
                case "F5":
                    config.isBGR = true;
                    config.numberOfViews = 55;
                    break;

                default:
                    var isNumeric = int.TryParse("123", out int number);
                    if (!isNumeric)
                    {
                        throw new ArgumentException("Invalid config code format.");
                    }

                    config.isBGR = false;
                    config.numberOfViews = uint.Parse(code);
                    break;
            }
        }

        private static void translateLensAngleCode(string code, ref DisplayConfig config)
        {
            switch (code)
            {
                // left tilted lenses
                case "A":
                    config.lensOpeningAngleInDeg = 12.5f;
                    config.lensAngleNumerator = 2;
                    config.lensAngleDenominator = 3;
                    config.isLeft = true;
                    break;
                case "B":
                    config.lensOpeningAngleInDeg = 14.6f;
                    config.lensAngleNumerator = 40;
                    config.lensAngleDenominator = 51;
                    config.isLeft = true;
                    break;
                case "C":
                    config.lensOpeningAngleInDeg = 18.4f;
                    config.lensAngleNumerator = 1;
                    config.lensAngleDenominator = 1;
                    config.isLeft = true;
                    break;
                case "D":
                    config.lensOpeningAngleInDeg = 14.9f;
                    config.lensAngleNumerator = 4;
                    config.lensAngleDenominator = 5;
                    config.isLeft = true;
                    break;
                case "E":
                    config.lensOpeningAngleInDeg = 16.0f;
                    config.lensAngleNumerator = 43;
                    config.lensAngleDenominator = 50;
                    config.isLeft = true;
                    break;
                case "F":
                    config.lensOpeningAngleInDeg = 15.0f;
                    config.lensAngleNumerator = 5;
                    config.lensAngleDenominator = 6;
                    config.isLeft = true;
                    break;
                case "G":
                    config.lensOpeningAngleInDeg = 13.4f;
                    config.lensAngleNumerator = 5;
                    config.lensAngleDenominator = 7;
                    config.isLeft = true;
                    break;
                case "H":
                    config.lensOpeningAngleInDeg = 13.4f;
                    config.lensAngleNumerator = 40;
                    config.lensAngleDenominator = 49;
                    config.isLeft = true;
                    break;

                // right tilted lenses
                case "N":
                    config.lensOpeningAngleInDeg = 12.5f;
                    config.lensAngleNumerator = 2;
                    config.lensAngleDenominator = 3;
                    config.isLeft = false;
                    break;
                case "O":
                    config.lensOpeningAngleInDeg = 14.6f;
                    config.lensAngleNumerator = 40;
                    config.lensAngleDenominator = 51;
                    config.isLeft = false;
                    break;
                case "P":
                    config.lensOpeningAngleInDeg = 18.4f;
                    config.lensAngleNumerator = 1;
                    config.lensAngleDenominator = 1;
                    config.isLeft = false;
                    break;
                case "Q":
                    config.lensOpeningAngleInDeg = 14.9f;
                    config.lensAngleNumerator = 4;
                    config.lensAngleDenominator = 5;
                    config.isLeft = false;
                    break;
                case "R":
                    config.lensOpeningAngleInDeg = 16.0f;
                    config.lensAngleNumerator = 43;
                    config.lensAngleDenominator = 50;
                    config.isLeft = false;
                    break;
                case "S":
                    config.lensOpeningAngleInDeg = 15.0f;
                    config.lensAngleNumerator = 5;
                    config.lensAngleDenominator = 6;
                    config.isLeft = false;
                    break;
                case "T":
                    config.lensOpeningAngleInDeg = 13.4f;
                    config.lensAngleNumerator = 5;
                    config.lensAngleDenominator = 7;
                    config.isLeft = false;
                    break;
                default:
                    throw new ArgumentException("Invalid config code.");
            }
        }

        public static G3DShaderParameters getParametersFromDisplayConfig(
            DisplayConfig displayConfig
        )
        {
            return fillShaderParameters(
                (int)displayConfig.numberOfViews,
                (int)displayConfig.lensAngleNumerator,
                (int)displayConfig.lensAngleDenominator,
                displayConfig.isLeft ? 1 : 0,
                displayConfig.isBGR,
                0,
                0
            );
        }

        public static G3DShaderParameters getParametersFromConfigCode(string configCode)
        {
            try
            {
                DisplayConfig displayConfig = ParseConfigCode(configCode);
                return getParametersFromDisplayConfig(displayConfig);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to get shader parameters from config code: " + e.Message);
                return fillShaderParameters();
            }
        }

        public static int getNumberOfViews(string configCode)
        {
            try
            {
                DisplayConfig displayConfig = ParseConfigCode(configCode);
                return (int)displayConfig.numberOfViews;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to get number of views from config code: " + e.Message);
                return 7; // default to 2 views if parsing fails
            }
        }

        private static G3DShaderParameters fillShaderParameters(
            int nativeViewCount = 2,
            int angleRatioNumerator = 1,
            int angleRatioDenominator = 1,
            int leftLensOrientation = 0,
            bool isBGR = false,
            int blackBorder = 0,
            int blackSpace = 0,
            int screenWidth = 0,
            int screenHeight = 0
        )
        {
            G3DShaderParameters parameters = new G3DShaderParameters();

            // display parameters
            parameters.screenWidth = screenWidth;
            parameters.screenHeight = screenHeight;
#if UNITY_IOS
            parameters.leftViewportPosition = 0; //< The left position of the viewport in screen coordinates
            parameters.bottomViewportPosition = 0; //< The bottom position of the viewport in screen coordinates
#else

            parameters.leftViewportPosition = Screen.mainWindowPosition.x; //< The left position of the viewport in screen coordinates
            parameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height; //< The bottom position of the viewport in screen coordinates
#endif

            // default values are those i got from the head tracking library when no camera was connected
            parameters.nativeViewCount = nativeViewCount;
            parameters.angleRatioNumerator = angleRatioNumerator;
            parameters.angleRatioDenominator = angleRatioDenominator;
            parameters.leftLensOrientation = leftLensOrientation;
            parameters.BGRPixelLayout = isBGR ? 1 : 0;
            parameters.blackBorder = blackBorder;
            parameters.blackSpace = blackSpace;

            parameters.showTestFrame = 0;
            parameters.showTestStripe = 0;
            parameters.testGapWidth = 0;

            parameters.blur = 0; // this is based on a guess

            parameters.mstart = 0; // set to zero for no viewOffset
            parameters.track = 0; // set to zero for no tracking shift

            // lens parameters
            // This is a total guess as to how this parameter is calculated.
            parameters.hqViewCount = parameters.nativeViewCount * parameters.angleRatioDenominator;
            parameters.hviews1 = parameters.hqViewCount - 1;
            parameters.hviews2 = parameters.hqViewCount / 2;

            // bls and ble correspond to the start and end of the left eye view "window"
            // brs and bre correspond to the start and end of the right eye view "window"
            // my guess: the left eye takes half the views, and the right eye takes the other half
            parameters.bls = 0;
            parameters.ble = parameters.hviews2;
            parameters.brs = parameters.hviews2;
            parameters.bre = parameters.hqViewCount - 1;

            parameters.zCompensationValue = 0; // i got this value from the library when no camera was connected
            parameters.zCorrectionValue = 13248; // i got this value from the library when no camera was connected

            return parameters;
        }
    }
}
