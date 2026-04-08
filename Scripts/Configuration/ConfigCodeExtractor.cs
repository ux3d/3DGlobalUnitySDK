using System;

namespace G3D
{
    public class ConfigCode
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
    }
}
