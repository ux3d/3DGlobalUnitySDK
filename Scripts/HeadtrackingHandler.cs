using UnityEngine;

public class HeadtrackingHandler
{
    [Tooltip("Time it takes till the reset animation starts in seconds.")]
    public float headLostTimeoutInSec = 3.0f;

    [Tooltip("Reset animation duratuion in seconds.")]
    public float transitionDuration = 0.5f;

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

    public HeadtrackingHandler(float focusDistance)
    {
        lastHeadPosition = new Vector3(0, 0, -focusDistance);
    }

    /**
    * Calculate the new camera center position based on the head tracking.
    * If head tracking is lost, or the head moves to far away from the tracking camera a grace periope is started.
    * Afterwards the camera center will be animated back towards the default position.
    */
    public void handleHeadTrackingState(
        ref HeadPosition headPosition,
        ref Vector3 targetPosition,
        ref float targetViewSeparation,
        float viewSeparation,
        float focusDistance,
        Vector3 cameraParentLocalPosition
    )
    {
        // get new state
        HeadTrackingState newState = getNewStateState(prevHeadTrackingState, ref headPosition);

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
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
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

                if (headPosition.headDetected)
                {
                    Vector3 headPositionWorld = new Vector3(
                        (float)headPosition.worldPosX,
                        (float)headPosition.worldPosY,
                        (float)headPosition.worldPosZ
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

    private HeadTrackingState getNewStateState(
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
}
