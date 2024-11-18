using System.Collections;
using System.Collections.Generic;
using System.IO;
using Salaros.Configuration;
using UnityEngine;

public class DefaultCalibrationProvider
{
    private ConfigParser configParser;

    private DefaultCalibrationProvider() { }

    public static DefaultCalibrationProvider getFromConfigFile(string calibrationFile)
    {
        DefaultCalibrationProvider provider = new DefaultCalibrationProvider();
        if (calibrationFile == null || !File.Exists(calibrationFile))
        {
            return provider;
        }
        provider.configParser = new ConfigParser(calibrationFile);
        return provider;
    }

    public static DefaultCalibrationProvider getFromString(string calibrationData)
    {
        DefaultCalibrationProvider provider = new DefaultCalibrationProvider();
        if (calibrationData == null || calibrationData.Length == 0)
        {
            return provider;
        }
        provider.configParser = new ConfigParser(
            calibrationData,
            new ConfigParserSettings
            {
                MultiLineValues =
                    MultiLineValues.Simple
                    | MultiLineValues.AllowValuelessKeys
                    | MultiLineValues.QuoteDelimitedValues,
                Culture = new System.Globalization.CultureInfo("de-DE")
            }
        );
        return provider;
    }

    public G3DShaderParameters getDefaultShaderParameters()
    {
        G3DShaderParameters parameters = new G3DShaderParameters();

        // display parameters
#if UNITY_IOS
        parameters.screenWidth = Screen.width;
        parameters.screenHeight = Screen.height;
        parameters.leftViewportPosition = 0; //< The left position of the viewport in screen coordinates
        parameters.bottomViewportPosition = 0; //< The bottom position of the viewport in screen coordinates
#else
        DisplayInfo mainDisplayInfo = Screen.mainWindowDisplayInfo;
        parameters.screenWidth = mainDisplayInfo.width;
        parameters.screenHeight = mainDisplayInfo.height;
        parameters.leftViewportPosition = Screen.mainWindowPosition.x; //< The left position of the viewport in screen coordinates
        parameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height; //< The bottom position of the viewport in screen coordinates
#endif

        // default values are those i got from the head tracking library when no camera was connected
        parameters.nativeViewCount = readOrDefault("NativeViewcount", 7);
        parameters.angleRatioNumerator = readOrDefault("AngleRatioNumerator", 4);
        parameters.angleRatioDenominator = readOrDefault("AngleRatioDenominator", 5);
        parameters.leftLensOrientation = readOrDefault("LeftLensOrientation", 1);
        parameters.BGRPixelLayout = readOrDefault("isBGR", 0);
        parameters.blackBorder = readOrDefault("BlackBorderDefault", 0);
        parameters.blackSpace = readOrDefault("BlackSpaceDefault", 0);

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

    /// <summary>
    /// Returns the value of key from the ini file, or defaultValue if the key is not found or an error occurs.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="key"></param>
    /// <param name="section"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private int readOrDefault(string key, int defaultValue)
    {
        if (configParser == null)
        {
            return defaultValue;
        }

        try
        {
            string value = configParser.GetValue("MonitorConfiguration", key);

            if (value == null)
            {
                return defaultValue;
            }
            int number;
            if (int.TryParse(value, out number))
            {
                return number;
            }

            float floatNumber;
            if (float.TryParse(value, out floatNumber))
            {
                return (int)floatNumber;
            }

            bool boolValue;
            if (bool.TryParse(value, out boolValue))
            {
                return boolValue ? 1 : 0;
            }

            return defaultValue;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
            return defaultValue;
        }
    }
}
