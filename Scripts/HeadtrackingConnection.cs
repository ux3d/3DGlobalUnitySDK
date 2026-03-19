using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HeadtrackingConnection
    : ITNewHeadPositionCallback,
        ITNewShaderParametersCallback,
        ITNewErrorMessageCallback
{
    [Tooltip("Time it takes till the reset animation starts in seconds.")]
    public float headLostTimeoutInSec = 3.0f;

    [Tooltip("Reset animation duratuion in seconds.")]
    public float transitionDuration = 0.5f;

    public Vector3Int headPositionFilter = new Vector3Int(5, 5, 5);
    public LatencyCorrectionMode latencyCorrectionMode = LatencyCorrectionMode.LCM_SIMPLE;

    public float focusDistance = 1.0f;
    public float headTrackingScale = 1.0f;
    public float sceneScaleFactor = 1.0f;

    private bool debugMessages;

    private Vector3 lastHeadPosition = new Vector3(0, 0, 0);

    private float headLostTimer = 0.0f;
    private float transitionTime = 0.0f;

    private enum HeadTrackingState
    {
        TRACKING,
        LOST,
        TRANSITIONTOLOST,
        TRANSITIONTOTRACKING,
        LOSTGRACEPERIOD
    }

    private HeadTrackingState prevHeadTrackingState = HeadTrackingState.LOST;

    private LibInterface libInterface;
    private string calibrationPath;

    /// <summary>
    /// This struct is used to store the current head position.
    /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
    /// NEVER use headPosition directly.
    /// </summary>
    private HeadPosition headPosition;
    private HeadPosition filteredHeadPosition;

    private static object headPosLock = new object();
    private static object shaderLock = new object();

    private Queue<string> headPositionLog;

    private G3DCamera g3dCamera;

    public HeadtrackingConnection(
        float focusDistance,
        float headTrackingScale,
        float sceneScaleFactor,
        string calibrationPathOverwrite,
        G3DCamera g3dCamera,
        bool debugMessages = false,
        Vector3Int headPositionFilter = new Vector3Int(),
        LatencyCorrectionMode latencyCorrectionMode = LatencyCorrectionMode.LCM_SIMPLE
    )
    {
        lastHeadPosition = new Vector3(0, 0, -focusDistance);
        this.focusDistance = focusDistance;
        this.headTrackingScale = headTrackingScale;
        this.sceneScaleFactor = sceneScaleFactor;

        calibrationPath = System.Environment.GetFolderPath(
            Environment.SpecialFolder.CommonDocuments
        );
        calibrationPath = Path.Combine(calibrationPath, "3D Global", "calibrations");
        if (!string.IsNullOrEmpty(calibrationPathOverwrite))
        {
            calibrationPath = calibrationPathOverwrite;
        }

        this.debugMessages = debugMessages;
        this.headPositionFilter = headPositionFilter;
        this.latencyCorrectionMode = latencyCorrectionMode;

        this.g3dCamera = g3dCamera;

        headPositionLog = new Queue<string>(10000);
    }

    public void startHeadTracking()
    {
        try
        {
            libInterface.startHeadTracking();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to start head tracking: " + e.Message);
        }
    }

    public void shiftViewToLeft()
    {
        try
        {
            libInterface.shiftViewToLeft();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to shift view to left: " + e.Message);
        }
    }

    public void shiftViewToRight()
    {
        try
        {
            libInterface.shiftViewToRight();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to shift view to right: " + e.Message);
        }
    }

    public void toggleHeadTracking()
    {
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        try
        {
            HeadTrackingStatus headtrackingConnection = libInterface.getHeadTrackingStatus();
            if (headtrackingConnection.hasTrackingDevice)
            {
                if (!headtrackingConnection.isTrackingActive)
                {
                    libInterface.startHeadTracking();
                }
                else
                {
                    libInterface.stopHeadTracking();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to toggle head tracking status: " + e.Message);
        }
    }

    /// <summary>
    /// always use this method to get the current head position.
    /// NEVER access headPosition directly, as it is updated in a different thread.
    ///
    /// </summary>
    /// <returns></returns>
    public HeadPosition getHeadPosition()
    {
        HeadPosition currentHeadPosition;
        lock (headPosLock)
        {
            if (usePositionFiltering())
            {
                currentHeadPosition = filteredHeadPosition;
            }
            else
            {
                currentHeadPosition = headPosition;
            }
        }
        return currentHeadPosition;
    }

    /**
    * Calculate the new camera center position based on the head tracking.
    * If head tracking is lost, or the head moves to far away from the tracking camera a grace periope is started.
    * Afterwards the camera center will be animated back towards the default position.
    */
    public void handleHeadTrackingState(
        ref Vector3 targetPosition,
        ref float targetViewSeparation,
        float viewSeparation,
        float focusDistance
    )
    {
        HeadPosition headPos = getHeadPosition();
        // get new state
        HeadTrackingState newState = getNewTrackingState(prevHeadTrackingState, ref headPos);

        prevHeadTrackingState = newState;

        // handle lost state
        if (newState == HeadTrackingState.LOST)
        {
            targetPosition = new Vector3(0, 0, -focusDistance);
            targetViewSeparation = 0.0f;
        }
        // handle tracking state
        else if (newState == HeadTrackingState.TRACKING)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPos.worldPosX,
                (float)headPos.worldPosY,
                (float)headPos.worldPosZ
            );

            targetPosition = headPositionWorld;
            targetViewSeparation = viewSeparation;
            lastHeadPosition = targetPosition;
        }
        // if lost, start grace period
        else if (newState == HeadTrackingState.LOSTGRACEPERIOD)
        {
            // if we have waited for the timeout
            if (headLostTimer > headLostTimeoutInSec)
            {
                newState = HeadTrackingState.TRANSITIONTOLOST;
                headLostTimer = 0.0f;
                transitionTime = 0.0f;
            }
            else
            {
                headLostTimer += Time.deltaTime;
                targetPosition = lastHeadPosition;
                targetViewSeparation = viewSeparation;
            }
        }
        // handle transitions
        else if (
            newState == HeadTrackingState.TRANSITIONTOLOST
            || newState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            // init with values for transition to lost
            Vector3 originPosition = lastHeadPosition;
            Vector3 transitionTargetPosition = new Vector3(0, 0, -focusDistance);
            float transitionViewSeparation = 0.0f;
            float originSeparation = viewSeparation;

            if (newState == HeadTrackingState.TRANSITIONTOTRACKING)
            {
                originPosition = new Vector3(0, 0, -focusDistance);
                transitionViewSeparation = viewSeparation;
                originSeparation = 0.0f;

                if (headPos.headDetected)
                {
                    Vector3 headPositionWorld = new Vector3(
                        (float)headPos.worldPosX,
                        (float)headPos.worldPosY,
                        (float)headPos.worldPosZ
                    );
                    transitionTargetPosition = headPositionWorld;
                }
                else
                {
                    // if no head is detected use last known head position
                    transitionTargetPosition = lastHeadPosition;
                }
            }

            bool isEndReached = handleTransition(
                originPosition,
                transitionTargetPosition,
                originSeparation,
                transitionViewSeparation,
                ref targetPosition,
                ref targetViewSeparation
            );
            if (isEndReached)
            {
                transitionTime = 0.0f;
                // if we have reached the target position, we are no longer in transition
                if (newState == HeadTrackingState.TRANSITIONTOLOST)
                {
                    newState = HeadTrackingState.LOST;
                }
                else
                {
                    newState = HeadTrackingState.TRACKING;
                }
            }
        }

        // reset lost timer if we are not in grace period
        if (newState != HeadTrackingState.LOSTGRACEPERIOD)
        {
            headLostTimer = 0.0f;
        }

        prevHeadTrackingState = newState;
    }

    public void initLibrary()
    {
        string applicationName = Application.productName;
        if (string.IsNullOrEmpty(applicationName))
        {
            applicationName = "Unity";
        }
        var invalids = System.IO.Path.GetInvalidFileNameChars();
        applicationName = String
            .Join("_", applicationName.Split(invalids, StringSplitOptions.RemoveEmptyEntries))
            .TrimEnd('.');
        applicationName = applicationName + "_G3D_Config.ini";

        try
        {
            bool useHimaxD2XXDevices = true;
            bool useHimaxRP2040Devices = true;
            bool usePmdFlexxDevices = true;

            libInterface = LibInterface.Instance;
            libInterface.init(
                calibrationPath,
                Application.persistentDataPath,
                applicationName,
                this,
                this,
                this,
                debugMessages,
                useHimaxD2XXDevices,
                useHimaxRP2040Devices,
                usePmdFlexxDevices
            );
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to initialize library: " + e.Message);
            return;
        }

        // set initial values
        // intialize head position at focus distance from focus plane
        headPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = -focusDistance
        };
        filteredHeadPosition = new HeadPosition
        {
            headDetected = false,
            imagePosIsValid = false,
            imagePosX = 0,
            imagePosY = 0,
            worldPosX = 0.0,
            worldPosY = 0.0,
            worldPosZ = -focusDistance
        };

        if (usePositionFiltering())
        {
            try
            {
                libInterface.initializePositionFilter(
                    headPositionFilter.x,
                    headPositionFilter.y,
                    headPositionFilter.z
                );
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to initialize position filter: " + e.Message);
            }
        }
    }

    public void deinitLibrary()
    {
        if (libInterface == null || !libInterface.isInitialized())
        {
            return;
        }

        try
        {
            libInterface.stopHeadTracking();
            libInterface.unregisterHeadPositionChangedCallback(this);
            libInterface.unregisterShaderParametersChangedCallback(this);
            libInterface.unregisterMessageCallback(this);
            libInterface.deinit();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void logCameraPositionsToFile()
    {
        StreamWriter writer = new StreamWriter(
            Application.dataPath + "/HeadPositionLog.csv",
            false
        );
        writer.WriteLine(
            "Camera update time; Camera X; Camera Y; Camera Z; Head detected; Image position valid; Filtered X; Filtered Y; Filtered Z"
        );
        string[] headPoitionLogArray = headPositionLog.ToArray();
        for (int i = 0; i < headPoitionLogArray.Length; i++)
        {
            writer.WriteLine(headPoitionLogArray[i]);
        }
        writer.Close();
    }

    public void calculateShaderParameters()
    {
        libInterface.calculateShaderParameters(latencyCorrectionMode);
        g3dCamera.setShaderParameters(libInterface.getCurrentShaderParameters());
    }

    public void updateScreenViewportProperties(Vector2Int displayResolution)
    {
        try
        {
            // This is the size of the entire monitor screen
            libInterface.setScreenSize(displayResolution.x, displayResolution.y);

            // this refers to the window in which the 3D effect is rendered (including eg windows top window menu)
            libInterface.setWindowSize(Screen.width, Screen.height);
            libInterface.setWindowPosition(
                Screen.mainWindowPosition.x,
                Screen.mainWindowPosition.y
            );

            // This refers to the actual viewport in which the 3D effect is rendered
            libInterface.setViewportSize(Screen.width, Screen.height);
            libInterface.setViewportOffset(0, 0);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to update screen viewport properties: " + e.Message);
        }
    }

    /// <summary>
    /// returns true if transition end is reached
    ///
    /// </summary>
    /// <param name="transitionTargetPosition"></param>
    /// <param name="targetPosition"></param>
    /// <param name="targetViewSeparation"></param>
    /// <param name=""></param>
    /// <returns>is transition end reached</returns>
    private bool handleTransition(
        Vector3 originPosition,
        Vector3 transitionTargetPosition,
        float originSeparation,
        float transitionViewSeparation,
        ref Vector3 targetPosition,
        ref float targetViewSeparation
    )
    {
        // interpolate values
        float transitionPercentage = transitionTime / transitionDuration;
        transitionTime += Time.deltaTime;

        // set to default position
        Vector3 interpolatedPosition = Vector3.Lerp(
            originPosition,
            transitionTargetPosition,
            transitionPercentage
        );
        float interpolatedEyeSeparation = Mathf.Lerp(
            originSeparation,
            transitionViewSeparation,
            transitionPercentage
        );

        // check if end is reached (with a small tolerance)
        if (transitionPercentage + 0.01f < 1.0f)
        {
            // apply values
            targetPosition = interpolatedPosition;
            targetViewSeparation = interpolatedEyeSeparation;
            return false;
        }
        else
        {
            return true;
        }
    }

    private HeadTrackingState getNewTrackingState(
        HeadTrackingState currentHeadTrackingState,
        ref HeadPosition headPosition
    )
    {
        HeadTrackingState newState;

        if (headPosition.headDetected)
        {
            // if head detected
            if (currentHeadTrackingState == HeadTrackingState.TRACKING)
            {
                newState = HeadTrackingState.TRACKING;
            }
            else if (currentHeadTrackingState == HeadTrackingState.LOSTGRACEPERIOD)
            {
                newState = HeadTrackingState.TRACKING;
            }
            else if (currentHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING)
            {
                newState = HeadTrackingState.TRANSITIONTOTRACKING;
            }
            else if (currentHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST)
            {
                newState = HeadTrackingState.TRANSITIONTOLOST;
            }
            else //(currentHeadTrackingState == HeadTrackingState.LOST)
            {
                newState = HeadTrackingState.TRANSITIONTOTRACKING;
            }
        }
        else
        {
            // if head not detected
            if (currentHeadTrackingState == HeadTrackingState.TRACKING)
            {
                newState = HeadTrackingState.LOSTGRACEPERIOD;
            }
            else if (currentHeadTrackingState == HeadTrackingState.LOSTGRACEPERIOD)
            {
                newState = HeadTrackingState.LOSTGRACEPERIOD;
            }
            else if (currentHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING)
            {
                newState = HeadTrackingState.TRANSITIONTOTRACKING;
            }
            else if (currentHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST)
            {
                newState = HeadTrackingState.TRANSITIONTOLOST;
            }
            else //(currentHeadTrackingState == HeadTrackingState.LOST)
            {
                newState = HeadTrackingState.LOST;
            }
        }
        return newState;
    }

    private string headTrackingStateToString()
    {
        switch (prevHeadTrackingState)
        {
            case HeadTrackingState.TRACKING:
                return "TRACKING";
            case HeadTrackingState.LOST:
                return "LOST";
            case HeadTrackingState.LOSTGRACEPERIOD:
                return "LOSTGRACEPERIOD";
            case HeadTrackingState.TRANSITIONTOLOST:
                return "TRANSITIONTOLOST";
            case HeadTrackingState.TRANSITIONTOTRACKING:
                return "TRANSITIONTOTRACKING";
            default:
                return "UNKNOWN";
        }
    }

    #region callback handling
    void ITNewHeadPositionCallback.NewHeadPositionCallback(
        bool headDetected,
        bool imagePosIsValid,
        int imagePosX,
        int imagePosY,
        double worldPosX,
        double worldPosY,
        double worldPosZ
    )
    {
        lock (headPosLock)
        {
            string logEntry =
                DateTime.Now.ToString("HH:mm::ss.fff")
                + ";"
                + worldPosX
                + ";"
                + worldPosY
                + ";"
                + worldPosZ
                + ";"
                + headDetected
                + ";"
                + imagePosIsValid
                + ";";

            headPosition.headDetected = headDetected;
            headPosition.imagePosIsValid = imagePosIsValid;

            int millimeterToMeter = 1000;

            Vector3 headPos = new Vector3(
                (float)-worldPosX / millimeterToMeter,
                (float)worldPosY / millimeterToMeter,
                (float)-worldPosZ / millimeterToMeter
            );

            int scaleFactorInt = (int)sceneScaleFactor * (int)headTrackingScale;
            float scaleFactor = sceneScaleFactor * headTrackingScale;

            headPosition.imagePosX = imagePosX / (int)millimeterToMeter * scaleFactorInt;
            headPosition.imagePosY = imagePosY / (int)millimeterToMeter * scaleFactorInt;
            headPosition.worldPosX = headPos.x * scaleFactor;
            headPosition.worldPosY = headPos.y * scaleFactor;
            headPosition.worldPosZ = headPos.z * sceneScaleFactor;

            if (usePositionFiltering())
            {
                double filteredPositionX;
                double filteredPositionY;
                double filteredPositionZ;

                if (headDetected)
                {
                    try
                    {
                        libInterface.applyPositionFilter(
                            worldPosX,
                            worldPosY,
                            worldPosZ,
                            out filteredPositionX,
                            out filteredPositionY,
                            out filteredPositionZ
                        );

                        filteredHeadPosition.worldPosX =
                            -filteredPositionX / millimeterToMeter * scaleFactor;
                        filteredHeadPosition.worldPosY =
                            filteredPositionY / millimeterToMeter * scaleFactor;
                        filteredHeadPosition.worldPosZ =
                            -filteredPositionZ / millimeterToMeter * sceneScaleFactor;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Failed to apply position filter: " + e.Message);
                    }
                }

                filteredHeadPosition.headDetected = headDetected;
                filteredHeadPosition.imagePosIsValid = imagePosIsValid;

                logEntry +=
                    filteredHeadPosition.worldPosX
                    + ";"
                    + filteredHeadPosition.worldPosY
                    + ";"
                    + filteredHeadPosition.worldPosZ
                    + ";";
            }

            headPositionLog.Enqueue(logEntry);
        }
    }

    void ITNewErrorMessageCallback.NewErrorMessageCallback(
        EMessageSeverity severity,
        string sender,
        string caption,
        string cause,
        string remedy
    )
    {
        string messageText = formatErrorMessage(caption, cause, remedy);
        switch (severity)
        {
            case EMessageSeverity.MS_EXCEPTION:
                Debug.LogError(messageText);
                break;
            case EMessageSeverity.MS_ERROR:
                Debug.LogError(messageText);
                break;
            case EMessageSeverity.MS_WARNING:
                Debug.LogWarning(messageText);
                break;
            case EMessageSeverity.MS_INFO:

                Debug.Log(messageText);
                break;
            default:
                Debug.Log(messageText);
                break;
        }
    }

    /// <summary>
    /// The shader parameters contain everything necessary for the shader to render the 3D effect.
    /// These are updated every time a new head position is received.
    /// They do not update the head position itself.
    /// </summary>
    /// <param name="shaderParameters"></param>
    void ITNewShaderParametersCallback.NewShaderParametersCallback(
        G3DShaderParameters shaderParameters
    )
    {
        g3dCamera.setShaderParameters(shaderParameters);
    }

    private string formatErrorMessage(string caption, string cause, string remedy)
    {
        string messageText = caption + ": " + cause;

        if (string.IsNullOrEmpty(remedy) == false)
        {
            messageText = messageText + "\n" + remedy;
        }

        return messageText;
    }
    #endregion

    /// <summary>
    /// Returns false if all values of the position filter are set to zero.
    /// </summary>
    /// <returns></returns>
    private bool usePositionFiltering()
    {
        return headPositionFilter.x != 0 || headPositionFilter.y != 0 || headPositionFilter.z != 0;
    }
}
