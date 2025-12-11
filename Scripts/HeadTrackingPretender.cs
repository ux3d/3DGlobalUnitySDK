using UnityEngine;

/// <summary>
/// This class can be used to simulate head tracking if no head tracking device is available.
/// Simply attach it to a GameObject and move the GameObject around to simulate head tracking.
/// The game objects transform will be used to simulate the head position.
/// (In meter -> 0.7 z equals 70 centimiter from the head tracking camera)
/// </summary>
public class HeadTrackingPretender : MonoBehaviour
{
    public G3DCamera g3dCamera;
    public bool headDetected = true;

    public float initialOffsetZ = 0.7f;

    // Start is called before the first frame update
    void Start() { }

    // Update is called once per frame
    void Update()
    {
        Vector3 headPosition = transform.localPosition;
        headPosition = -headPosition;
        headPosition = headPosition * 1000;
        headPosition.z += initialOffsetZ * 1000;
        ((ITNewHeadPositionCallback)g3dCamera).NewHeadPositionCallback(
            headDetected,
            true,
            (int)headPosition.x,
            (int)headPosition.y,
            headPosition.x,
            headPosition.y,
            headPosition.z
        );
    }
}
