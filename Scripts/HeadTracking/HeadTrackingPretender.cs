using UnityEngine;

namespace G3D
{
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
            g3dCamera.headtrackingConnection.debugUpdateHeadPosition(
                new HeadTrackingSDK.HeadPosition
                {
                    world_x = (int)headPosition.x,
                    world_y = (int)headPosition.y,
                    world_z = (int)headPosition.z
                },
                headDetected
            );
        }
    }
}
