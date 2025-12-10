using UnityEngine;

public class HeadtrackingState
{
    private float prevEyeSeparation = 0.0f;

    [Tooltip("Time it takes till the reset animation starts in seconds.")]
    public float headLostTimeoutInSec = 3.0f;

    [Tooltip("Reset animation duratuion in seconds.")]
    public float transitionDuration = 1.5f;

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

    public HeadtrackingState(float focusDistance)
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
        ref float targetEyeSeparation,
        float eyeSeparation,
        float focusDistance,
        Vector3 cameraParentLocalPosition
    )
    {
        HeadTrackingState headTrackingState = HeadTrackingState.LOST;

        if (
            prevHeadTrackingState == HeadTrackingState.LOSTGRACEPERIOD
            || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
            || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            headTrackingState = prevHeadTrackingState;
        }
        // head detected
        if (headPosition.headDetected)
        {
            Vector3 headPositionWorld = new Vector3(
                (float)headPosition.worldPosX,
                (float)headPosition.worldPosY,
                (float)headPosition.worldPosZ
            );

            headTrackingState = HeadTrackingState.TRACKING;
            targetPosition = headPositionWorld;
            targetEyeSeparation = eyeSeparation;
        }

        // if reaquired and we were in lost state or transitioning to lost, start transition to tracking
        if (
            headTrackingState == HeadTrackingState.TRACKING
            && (
                prevHeadTrackingState == HeadTrackingState.LOST
                || prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
            )
        )
        {
            headTrackingState = HeadTrackingState.TRANSITIONTOTRACKING;
            transitionTime = 0.0f;
        }

        if (
            headTrackingState == HeadTrackingState.TRACKING
            && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            headTrackingState = HeadTrackingState.TRANSITIONTOTRACKING;
        }

        // if lost, start grace period
        if (
            headTrackingState == HeadTrackingState.LOST
            && prevHeadTrackingState == HeadTrackingState.TRACKING
        )
        {
            headTrackingState = HeadTrackingState.LOSTGRACEPERIOD;
            targetPosition = lastHeadPosition;
        }

        if (headTrackingState == HeadTrackingState.LOSTGRACEPERIOD)
        {
            // if we have waited for the timeout
            if (Time.time - headLostTimer > headLostTimeoutInSec)
            {
                headTrackingState = HeadTrackingState.TRANSITIONTOLOST;
                headLostTimer = Time.time;
                transitionTime = 0.0f;
            }
            else
            {
                targetPosition = lastHeadPosition;
                targetEyeSeparation = prevEyeSeparation;
            }
        }

        // if we are in a transition when the transition flips reset the transition time
        if (
            headTrackingState == HeadTrackingState.TRANSITIONTOLOST
                && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
            || headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
                && prevHeadTrackingState == HeadTrackingState.TRANSITIONTOLOST
        )
        {
            transitionTime = 0.0f;
        }

        if (
            headTrackingState == HeadTrackingState.TRANSITIONTOLOST
            || headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING
        )
        {
            // interpolate values
            float transitionPercentage = transitionTime / transitionDuration;
            transitionTime += Time.deltaTime;

            // set to default position
            Vector3 transitionTargetPosition = new Vector3(0, 0, -focusDistance);
            if (headTrackingState == HeadTrackingState.TRANSITIONTOTRACKING)
            {
                transitionTargetPosition = targetPosition;
            }

            Vector3 interpolatedPosition = Vector3.Lerp(
                cameraParentLocalPosition,
                transitionTargetPosition,
                transitionPercentage
            );
            float interpolatedEyeSeparation = Mathf.Lerp(
                prevEyeSeparation,
                targetEyeSeparation,
                transitionPercentage
            );

            // apply values
            float distance = Vector3.Distance(interpolatedPosition, transitionTargetPosition);
            // only use interpolated position if we are not close enough to the target position
            if (distance > 0.0001f)
            {
                targetPosition = interpolatedPosition;
                targetEyeSeparation = interpolatedEyeSeparation;
            }
            else
            {
                // if we have reached the target position, we are no longer in transition
                if (headTrackingState == HeadTrackingState.TRANSITIONTOLOST)
                {
                    headTrackingState = HeadTrackingState.LOST;
                }
                else
                {
                    headTrackingState = HeadTrackingState.TRACKING;
                }
            }
        }

        // store last known position data for tracking loss case
        if (headTrackingState == HeadTrackingState.TRACKING)
        {
            lastHeadPosition = targetPosition;
            prevEyeSeparation = targetEyeSeparation;
        }

        prevHeadTrackingState = headTrackingState;
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
