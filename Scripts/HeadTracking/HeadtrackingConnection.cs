using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace G3D
{
    public class HeadtrackingConnection
    {
        public float headLostTimeoutInSec = 3.0f;

        public float transitionDuration = 0.5f;

        public Vector3Int headPositionFilter = new Vector3Int(5, 5, 5);
        public float basicWorkingDistance = 1.0f;
        public float headTrackingSensitivity = 1.0f;
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

        private string calibrationPath;

        /// <summary>
        /// This struct is used to store the current head position.
        /// It is updated in a different thread, so always use getHeadPosition() to get the current head position.
        /// NEVER use headPosition directly.
        /// </summary>
        private HeadPosition headPosition;

        private static object headPosLock = new object();

        private Queue<string> headPositionLog;

        private G3DCamera g3dCamera;

        public HeadtrackingConnection(
            float basicWorkingDistance,
            float headTrackingSensitivity,
            string configurationPathOverwrite,
            G3DCamera g3dCamera,
            bool debugMessages = false,
            Vector3Int headPositionFilter = new Vector3Int()
        )
        {
            lastHeadPosition = new Vector3(0, 0, -basicWorkingDistance);
            this.basicWorkingDistance = basicWorkingDistance;
            this.headTrackingSensitivity = headTrackingSensitivity;

            calibrationPath = System.Environment.GetFolderPath(
                Environment.SpecialFolder.CommonDocuments
            );
            calibrationPath = Path.Combine(calibrationPath, "3D Global", "calibrations");
            if (!string.IsNullOrEmpty(configurationPathOverwrite))
            {
                calibrationPath = configurationPathOverwrite;
            }

            this.debugMessages = debugMessages;
            this.headPositionFilter = headPositionFilter;

            this.g3dCamera = g3dCamera;

            headPositionLog = new Queue<string>(10000);
        }

        public void startHeadTracking()
        {
            try
            {
                HeadTrackingSDK.ht_start();
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to start head tracking: " + e.Message);
            }
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

            if (HeadTrackingSDK.load())
            {
                if (HeadTrackingSDK.ht_init()) { }
                else
                {
                    Debug.LogError("Failed to initialize headtracking library");
                    return;
                }
            }
            else
            {
                Debug.LogError("Failed to load headtracking library");
                return;
            }

            // set initial values
            // intialize head position at focus distance from focus plane
            headPosition = new HeadPosition
            {
                headDetected = false,
                worldPosX = 0.0,
                worldPosY = 0.0,
                worldPosZ = -basicWorkingDistance
            };
        }

        public void deinitLibrary()
        {
            if (!HeadTrackingSDK.ht_is_initialized())
            {
                return;
            }

            try
            {
                HeadTrackingSDK.ht_stop();
                HeadTrackingSDK.ht_fin();
                HeadTrackingSDK.unload();
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        /// <summary>
        /// Calls the sdk to update the head position. This should be called in the Update() method of the camera.
        /// </summary>
        public void updateHeadPosition()
        {
            if (!HeadTrackingSDK.ht_is_initialized())
            {
                return;
            }

            HeadTrackingSDK.HeadPosition trackedHeadPos = HeadTrackingSDK.ht_get_head_world_pos();
            bool headDetected = isHeadDetected();

            updateHeadPosition(trackedHeadPos, headDetected);
        }

        public void updateHeadPosition(
            HeadTrackingSDK.HeadPosition trackedHeadPos,
            bool headDetected
        )
        {
            lock (headPosLock)
            {
                string logEntry =
                    DateTime.Now.ToString("HH:mm::ss.fff")
                    + ";"
                    + trackedHeadPos.world_x
                    + ";"
                    + trackedHeadPos.world_y
                    + ";"
                    + trackedHeadPos.world_z
                    + ";"
                    + headDetected
                    + ";";

                headPosition.headDetected = headDetected;

                int millimeterToMeter = 1000;

                Vector3 headPos = new Vector3(
                    -trackedHeadPos.world_x / millimeterToMeter,
                    trackedHeadPos.world_y / millimeterToMeter,
                    -trackedHeadPos.world_z / millimeterToMeter
                );

                int scaleFactorInt = (int)headTrackingSensitivity;

                headPosition.worldPosX = headPos.x * headTrackingSensitivity;
                headPosition.worldPosY = headPos.y * headTrackingSensitivity;
                headPosition.worldPosZ = headPos.z * headTrackingSensitivity;

                headPositionLog.Enqueue(logEntry);
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
                currentHeadPosition = headPosition;
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
            float currentFocusDistance
        )
        {
            HeadPosition headPos = getHeadPosition();
            headPos.worldPosZ = convertWorldPosZToTrackedPosition(
                (float)headPos.worldPosZ,
                currentFocusDistance
            );
            // get new state
            HeadTrackingState newState = getNewTrackingState(prevHeadTrackingState, ref headPos);

            prevHeadTrackingState = newState;

            // handle lost state
            if (newState == HeadTrackingState.LOST)
            {
                targetPosition = new Vector3(0, 0, -currentFocusDistance);
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
                Vector3 transitionTargetPosition = new Vector3(0, 0, -currentFocusDistance);
                float transitionViewSeparation = 0.0f;
                float originSeparation = viewSeparation;

                if (newState == HeadTrackingState.TRANSITIONTOTRACKING)
                {
                    originPosition = new Vector3(0, 0, -basicWorkingDistance);
                    transitionViewSeparation = viewSeparation;
                    originSeparation = 0.0f;

                    if (headPos.headDetected)
                    {
                        Vector3 headPositionWorld = new Vector3(
                            (float)headPos.worldPosX,
                            (float)headPos.worldPosY,
                            (float)headPos.worldPosZ
                        );
                        headPositionWorld.z = convertWorldPosZToTrackedPosition(
                            headPositionWorld.z,
                            currentFocusDistance
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
            HeadTrackingSDK.RenderParameters renderParameters =
                HeadTrackingSDK.ht_get_render_parameters();
            HeadTrackingSDK.MonitorParameters monitorParameters =
                HeadTrackingSDK.ht_get_monitor_parameters();
            G3DShaderParameters shaderParameters = convertToShaderParameters(
                renderParameters,
                monitorParameters
            );
            g3dCamera.setShaderParameters(shaderParameters);
        }

        public void setBasicWorkingDistance(float distance)
        {
            basicWorkingDistance = distance;
        }

        private bool isHeadDetected()
        {
            if (!HeadTrackingSDK.ht_is_initialized() && !HeadTrackingSDK.ht_is_connected())
            {
                return true;
            }

            HeadTrackingSDK.HeadTrackingUserPosCodes headTrackingUserPosCodes =
                HeadTrackingSDK.ht_get_user_guidance();
            return headTrackingUserPosCodes != HeadTrackingSDK.HeadTrackingUserPosCodes.USER_POS_NO;
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

        /// <summary>
        /// Converts the input position (in real world head space) to a position relative to the input focus distance.
        /// </summary>
        /// <param name="worldPosZ"></param>
        /// <param name="focusDistance"></param>
        /// <returns></returns>
        private float convertWorldPosZToTrackedPosition(float worldPosZ, float focusDistance)
        {
            // convert from mm to m and apply scale factor
            float zOffset = basicWorkingDistance + worldPosZ; // worldposZ is negative when in front of the camera, so we add it to the basic working distance
            float convertedZ = focusDistance - zOffset;
            return -convertedZ; // invert to be in right coordinate system for camera position
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

        private G3DShaderParameters convertToShaderParameters(
            HeadTrackingSDK.RenderParameters renderParameters,
            HeadTrackingSDK.MonitorParameters monitorParameters
        )
        {
            G3DShaderParameters shaderParameters = new G3DShaderParameters
            {
                leftViewportPosition = renderParameters.leftViewportPosition,
                bottomViewportPosition = renderParameters.bottomViewportPosition,
                screenWidth = renderParameters.screenWidth,
                screenHeight = renderParameters.screenHeight,
                nativeViewCount = monitorParameters.viewcount,
                angleRatioNumerator = monitorParameters.angleNumerator,
                angleRatioDenominator = monitorParameters.angleDenominator,
                leftLensOrientation = monitorParameters.left ? 1 : 0,
                BGRPixelLayout = monitorParameters.bgr,
                mstart = renderParameters.viewOffset,
                showTestFrame = renderParameters.showTestFrame,
                showTestStripe = renderParameters.showTestStripe,
                testGapWidth = renderParameters.testGapWidth,
                track = renderParameters.track,
                hqViewCount = renderParameters.hviews1 + 1,
                hviews1 = renderParameters.hviews1,
                hviews2 = renderParameters.hviews2,
                blur = renderParameters.blur,
                blackBorder = renderParameters.blackBorder,
                blackSpace = renderParameters.blackSpace,
                bls = renderParameters.bls,
                ble = renderParameters.ble,
                brs = renderParameters.brs,
                bre = renderParameters.bre,
                zCorrectionValue = renderParameters.zCorrectionValue,
                zCompensationValue = renderParameters.zCompensationValue
            };
            return shaderParameters;
        }
    }
}
