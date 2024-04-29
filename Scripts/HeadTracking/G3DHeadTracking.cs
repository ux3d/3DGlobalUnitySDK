using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class G3DHeadTracking : MonoBehaviour
{
    public string calibrationPath = "";
    public string configPath = "";
    public string configFileName = "";

    private IntPtr headTrackingDevice = new IntPtr();

    private LibInterface libInterface;

    // Start is called before the first frame update
    void Start()
    {
        libInterface = new LibInterface(calibrationPath, configPath, configFileName, true);
    }

    // Update is called once per frame
    void Update() { }

    void OnDestroy() { }
}
