using System;
using System.IO;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using UnityEngine;
using UnityEngine.Networking;

public class CalibrationProvider
{
    private IniData iniData;

    private CalibrationProvider() { }

    /// <summary>
    ///
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="callback">int return parameter can be ignored</param>
    /// <returns></returns>
    public static CalibrationProvider getFromURI(
        string uri,
        Func<CalibrationProvider, int> callback
    )
    {
        CalibrationProvider provider = new CalibrationProvider();
        if (uri == null || uri.Length == 0)
        {
            return provider;
        }

        UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        UnityWebRequestAsyncOperation asyncOperation = webRequest.SendWebRequest();

        asyncOperation.completed += (op) =>
        {
            if (!webRequest.isDone && webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(webRequest.error);
                return;
            }

            string calibrationData = webRequest.downloadHandler.text;

            IniDataParser parser = new IniDataParser();
            provider.iniData = parser.Parse(calibrationData);

            callback(provider);
        };

        return provider;
    }

    public static CalibrationProvider getFromConfigFile(string calibrationFile)
    {
        CalibrationProvider provider = new CalibrationProvider();
        if (calibrationFile == null || !File.Exists(calibrationFile))
        {
            return provider;
        }
        FileIniDataParser parser = new FileIniDataParser();
        provider.iniData = parser.ReadFile(calibrationFile);
        return provider;
    }

    public static CalibrationProvider getFromString(string calibrationData)
    {
        CalibrationProvider provider = new CalibrationProvider();
        if (calibrationData == null || calibrationData.Length == 0)
        {
            return provider;
        }
        IniDataParser parser = new IniDataParser();
        provider.iniData = parser.Parse(calibrationData);
        return provider;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public G3DShaderParameters getShaderParameters()
    {
        G3DShaderParameters parameters = new G3DShaderParameters();

        // display parameters
#if UNITY_IOS
        parameters.screenWidth = readOrDefault("HorizontalResolution", Screen.width);
        parameters.screenHeight = readOrDefault("VerticalResolution", Screen.height);
        parameters.leftViewportPosition = 0; //< The left position of the viewport in screen coordinates
        parameters.bottomViewportPosition = 0; //< The bottom position of the viewport in screen coordinates
#else
        DisplayInfo mainDisplayInfo = Screen.mainWindowDisplayInfo;
        parameters.screenWidth = readOrDefault("HorizontalResolution", mainDisplayInfo.width);
        parameters.screenHeight = readOrDefault("VerticalResolution", mainDisplayInfo.height);
        parameters.leftViewportPosition = Screen.mainWindowPosition.x; //< The left position of the viewport in screen coordinates
        parameters.bottomViewportPosition = Screen.mainWindowPosition.y + Screen.height; //< The bottom position of the viewport in screen coordinates
#endif

        // default values are those i got from the head tracking library when no camera was connected
        parameters.nativeViewCount = getInt("NativeViewcount");
        parameters.angleRatioNumerator = getInt("AngleRatioNumerator");
        parameters.angleRatioDenominator = getInt("AngleRatioDenominator");
        parameters.leftLensOrientation = getInt("LeftLensOrientation");
        parameters.BGRPixelLayout = getBool("isBGR") ? 1 : 0;
        parameters.blackBorder = getInt("BlackBorderDefault");
        parameters.blackSpace = getInt("BlackSpaceDefault");

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
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    private int readOrDefault(string key, int defaultValue)
    {
        if (iniData == null)
        {
            return defaultValue;
        }

        try
        {
            string value = null;
            iniData.TryGetKey("MonitorConfiguration." + key, out value);

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

    public int getInt(string key)
    {
        if (iniData == null)
        {
            throw new System.Exception("iniData is null");
        }

        string value;
        iniData.TryGetKey("MonitorConfiguration." + key, out value);

        if (value == null)
        {
            throw new System.Exception("Key not found: " + key);
        }
        int number;
        if (int.TryParse(value, out number))
        {
            return number;
        }
        throw new System.Exception("Error reading ini file. Value is not an int: " + value);
    }

    public float getFloat(string key)
    {
        if (iniData == null)
        {
            throw new System.Exception("iniData is null");
        }

        string value;
        iniData.TryGetKey("MonitorConfiguration." + key, out value);

        if (value == null)
        {
            throw new System.Exception("Key not found: " + key);
        }
        float number;
        if (float.TryParse(value, out number))
        {
            return number;
        }

        throw new System.Exception("Error reading ini file. Value is not a float: " + value);
    }

    public string getString(string key)
    {
        if (iniData == null)
        {
            throw new System.Exception("iniData is null");
        }

        string value;
        iniData.TryGetKey("MonitorConfiguration." + key, out value);

        if (value == null)
        {
            throw new System.Exception("Key not found: " + key);
        }

        return value;
    }

    public bool getBool(string key)
    {
        if (iniData == null)
        {
            throw new System.Exception("iniData is null");
        }

        string value = null;
        iniData.TryGetKey("MonitorConfiguration." + key, out value);

        if (value == null)
        {
            throw new System.Exception("Key not found: " + key);
        }
        bool boolValue;
        if (bool.TryParse(value, out boolValue))
        {
            return boolValue;
        }
        throw new System.Exception("Error reading ini file. Value is not a bool: " + value);
    }
}
