using System;
using System.IO;
using IniParser;
using IniParser.Model;
using IniParser.Parser;
using UnityEngine;
using UnityEngine.Networking;

namespace G3D
{
    public class ConfigurationProvider
    {
        private IniData iniData;

        private ConfigurationProvider() { }

        /// <summary>
        ///
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="callback">int return parameter can be ignored</param>
        /// <returns></returns>
        public static ConfigurationProvider getFromURI(
            string uri,
            Func<ConfigurationProvider, int> callback
        )
        {
            ConfigurationProvider provider = new ConfigurationProvider();
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

                string configurationData = webRequest.downloadHandler.text;

                IniDataParser parser = new IniDataParser();
                provider.iniData = parser.Parse(configurationData);

                callback(provider);
            };

            return provider;
        }

        public static ConfigurationProvider getFromConfigFile(string configurationFile)
        {
            ConfigurationProvider provider = new ConfigurationProvider();
            if (configurationFile == null || !File.Exists(configurationFile))
            {
                return provider;
            }
            FileIniDataParser parser = new FileIniDataParser();
            provider.iniData = parser.ReadFile(configurationFile);
            return provider;
        }

        public static ConfigurationProvider getFromString(string configurationData)
        {
            ConfigurationProvider provider = new ConfigurationProvider();
            if (configurationData == null || configurationData.Length == 0)
            {
                return provider;
            }
            IniDataParser parser = new IniDataParser();
            provider.iniData = parser.Parse(configurationData);
            return provider;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public G3DShaderParameters getShaderParameters()
        {
            int width;
            int height;
#if UNITY_IOS
            width = readOrDefault("HorizontalResolution", Screen.width);
            height = readOrDefault("VerticalResolution", Screen.height);
#else

            DisplayInfo mainDisplayInfo = Screen.mainWindowDisplayInfo;
            width = mainDisplayInfo.width;
            height = mainDisplayInfo.height;
#endif

            return fillShaderParameters(
                getInt("NativeViewcount"),
                getInt("AngleRatioNumerator"),
                getInt("AngleRatioDenominator"),
                getInt("LeftLensOrientation"),
                getBool("isBGR"),
                getInt("BlackBorderDefault"),
                getInt("BlackSpaceDefault"),
                readOrDefault("HorizontalResolution", width),
                readOrDefault("VerticalResolution", height)
            );
        }

        public ConfigCode.DisplayConfig getDisplayConfig()
        {
            string configCode = getString("ConfigCode");
            return ConfigCode.ParseConfigCode(configCode);
        }

        public static G3DShaderParameters getParametersFromDisplayConfig(
            ConfigCode.DisplayConfig displayConfig
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
            ConfigCode.DisplayConfig displayConfig = ConfigCode.ParseConfigCode(configCode);
            return getParametersFromDisplayConfig(displayConfig);
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
            if (
                float.TryParse(
                    value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out number
                )
            )
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
}
