using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DefaultCalibrationProvider
{
    public string CalibrationFile = "DefaultCalibration.ini";

    public DefaultCalibrationProvider(string calibrationFile)
    {
        CalibrationFile = calibrationFile;
    }

    public G3DShaderParameters getDefaultShaderParameters()
    {
        INIReader reader = new INIReader(CalibrationFile);

        G3DShaderParameters parameters = new G3DShaderParameters();

        // display parameters
        DisplayInfo mainDisplayInfo = Screen.mainWindowDisplayInfo;
        parameters.screenWidth = mainDisplayInfo.width;
        parameters.screenHeight = mainDisplayInfo.height;
        parameters.leftViewportPosition = Screen.mainWindowPosition.x; //< The left position of the viewport in screen coordinates
        parameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height; //< The bottom position of the viewport in screen coordinates

        // default values are those i got from the head tracking library when no camera was connected
        parameters.nativeViewCount = readOrDefault(reader, "NativeViewcount", 7);
        parameters.angleRatioNumerator = readOrDefault(reader, "AngleRatioNumerator", 4);
        parameters.angleRatioDenominator = readOrDefault(reader, "AngleRatioDenominator", 5);
        parameters.leftLensOrientation = readOrDefault(reader, "LeftLensOrientation", 1);
        parameters.BGRPixelLayout = readOrDefault(reader, "isBGR", 0);
        parameters.blackBorder = readOrDefault(reader, "BlackBorderDefault", 0);
        parameters.blackSpace = readOrDefault(reader, "BlackSpaceDefault", 0);

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
    private int readOrDefault(in INIReader reader, string key, int defaultValue)
    {
        try
        {
            return int.Parse(reader.Read(key));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
            return defaultValue;
        }
    }
}
