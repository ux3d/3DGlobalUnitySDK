using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(G3DHeadTracking))]
public class G3DCamera : MonoBehaviour
{
    #region Properties

    [Header("Camera")]
    public bool testviews_active = false;
    public bool testcolors_active = false;

    [Range(1.0f, 100.0f)]
    public float resolution = 100.0f;

    [Range(1, 16)]
    public int cameracount = 2;
    public bool camera_position_circular = false;

    [Range(0.00001f, 1.0f)]
    public float eyedistance = 0.65f;

    [Range(0f, 1f)]
    public float stereo_depth = 0.0f;

    [Range(-5f, 5f)]
    public float stereo_plane = 5f;

    [Range(0.00001f, 1.0f)]
    public float angle = 0.05f;

    [Range(1f, 200f)]
    public float distance = 30.0f;

    [Header("Modes")]
    public int rendermode = 1; //algo, viewmap, vector, ztracking

    [Range(0, 48)]
    public int viewshift = 0;
    public bool invert_headtracking = false;
    public bool enable_headtracking = true;

    [Range(0f, 3f)]
    public float stereo_delimiter_space = 0.1f;

    [Range(0f, 5f)]
    public float stereo_eyearea_space = 0.4f;

    [Range(0f, 100f)]
    public float stereo_zone_distance = 1f;

    [Range(0f, 500f)]
    public float stereo_zone_width = 1f;

    [Range(1, 20)]
    public int algo_angle_counter = 1;

    [Range(1, 20)]
    public int algo_angle_denominator = 1;

    [Range(2, 100)]
    public int algo_hqviews = 10;
    public bool algo_direction = false;
    public int blur_factor = 200;

    #endregion


    #region Views

    public const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public const string CAMERA_NAME_PREFIX = "g3dcam_";

    private Camera maincamera = null;
    private List<Camera> cameras = null;
    private GameObject cameraParent = null;

    private G3DHeadTracking headTracking = null;
    private Vector3 startCameraPos = Vector3.zero;

    public void setCameras()
    {
        //initialize cameras
        if (cameras == null)
        {
            maincamera = GetComponent<Camera>();

            cameraParent = new GameObject("g3dcams");
            cameraParent.transform.parent = transform;

            cameras = new List<Camera>();
            for (int i = 0; i < MAX_CAMERAS; i++)
            {
                cameras.Add(new GameObject(CAMERA_NAME_PREFIX + i).AddComponent<Camera>());
                cameras[i].transform.SetParent(cameraParent.transform, true);
                cameras[i].gameObject.SetActive(false);
            }
        }

        // var mi = apiGetMonitorInfo();

        //put camera host gameobject in easy-to-handle situation and save its position/rotation for resetting after "parenting" the child cameras
        Vector3 savedCameraPosition = transform.position;
        Quaternion savedCameraRotation = transform.rotation;
        cameraParent.transform.position = new Vector3(0, 0, 0);
        cameraParent.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

        //calculate camera positions and matrices
        for (int i = 0; i < cameracount; i++)
        {
            var camera = cameras[i];
            float currentView = -cameracount / 2 + i;

            //copy any changes to the main camera
            camera.fieldOfView = maincamera.fieldOfView;
            camera.farClipPlane = maincamera.farClipPlane;
            camera.nearClipPlane = maincamera.nearClipPlane;
            camera.projectionMatrix = maincamera.projectionMatrix;
            camera.transform.position = cameraParent.transform.position;
            camera.transform.rotation = cameraParent.transform.rotation;

            //adjust camera
            if (camera_position_circular)
            {
                //camera is placed according to an angle
                camera.transform.eulerAngles = new Vector3(
                    cameraParent.transform.rotation.eulerAngles.x,
                    cameraParent.transform.rotation.eulerAngles.y + currentView * angle,
                    cameraParent.transform.rotation.eulerAngles.z
                );

                camera.transform.localPosition = new Vector3(
                    cameraParent.transform.position.x
                        - ((float)(distance * Math.Sin(currentView * angle * (Math.PI / 180)))),
                    cameraParent.transform.position.y,
                    cameraParent.transform.position.z
                        + (
                            (float)(
                                distance
                                - distance * Math.Cos(currentView * angle * (Math.PI / 180))
                            )
                        )
                );
            }
            else
            {
                int ScreenWidth = Screen.currentResolution.width;

                // eye distance
                float EyeDistance = eyedistance * 100;

                // calculate eye distance in pixel
                // TODO this was the original code here. Handle correctly (see mi.MonitorWidth in the original code)
                // int StereoViewIPDOffset =
                //     (int)currentView * (int)(EyeDistance / mi.MonitorWidth * ScreenWidth / 2); // offset for left/right eye in pixel (eye distance (in mm) / monitor width (in mm) * monitor width (in pixel) / 2)
                int StereoViewIPDOffset = (int)currentView * (int)(EyeDistance / ScreenWidth / 2); // offset for left/right eye in pixel (eye distance (in mm) / monitor width (in mm) * monitor width (in pixel) / 2)

                // get view size
                int ViewWidth = camera.pixelWidth;

                // calculate offset for projection matrix
                float ProjOffset = StereoViewIPDOffset * stereo_depth / ViewWidth; // real offset (pixel offset * factor / view size (fullscreen here))

                // calculate adjusted projection matrix
                Matrix4x4 tempMatrix = camera.projectionMatrix; // original matrix
                tempMatrix[0, 2] = tempMatrix[0, 2] + ProjOffset; // apply offset

                // calculate offset for view matrix
                float ViewOffset = 0.0f;
                float FC = tempMatrix[2, 2];
                float FD = tempMatrix[2, 3];
                if ((Math.Abs(tempMatrix[0, 0]) > 1E-3) && (Math.Abs(FC - 1) > 1E-4)) // projection matrix is valid and calculation possible
                {
                    float Near = ((FC + 1) / (FC - 1) - 1) / 2 * FD; // near of current projection matrix
                    float DataWidth = 2 * Near / tempMatrix[0, 0]; // width
                    ViewOffset =
                        (float)StereoViewIPDOffset
                        / (float)ViewWidth
                        * DataWidth
                        * (float)(stereo_depth - (stereo_plane));
                }

                // apply new projection matrix
                camera.projectionMatrix = tempMatrix;

                camera.transform.localPosition = new Vector3(ViewOffset, 0, 0);
            }

            camera.gameObject.SetActive(true);
        }

        //reset parent position/rotation
        cameraParent.transform.position = savedCameraPosition;
        cameraParent.transform.rotation = savedCameraRotation;

        //disable all the other cameras, we are not using them with this cameracount
        for (int i = cameracount; i < MAX_CAMERAS; i++)
            cameras[i].gameObject.SetActive(false);

        updateShaderViews();
    }

    #endregion


    #region Shader

    private const int SUPPORTED_REPITITIONS = 2;

    private static Material material;

    private Texture[] testviews;
    private Texture[] testcolors;

    private Texture2D cache_loadedViewmap;
    private Texture2D cache_positionMap;
    private Texture2D cache_vectorMap;
    private Texture2D[] cache_vectorIndexMaps;

    private int cache_views_monitor = 1;

    #region shader_ids

    private int id_windowPosition;
    private int id_indexMap;
    private int id_view_count;
    private int id_algo_angle_counter;
    private int id_algo_angle_denominator;
    private int id_algo_direction;
    private int id_blur_factor;
    private int id_view_count_monitor_hq;
    private int id_view_offset;
    private int id_view_offset_headtracking;
    private int id_userPosition;
    private int id_viewPositions;
    private int id_ViewMap;
    private int id_PositionMap;
    private int id_VectorMap;
    private int[] id_View = new int[MAX_CAMERAS];
    private int[] id_VectorIndexMap = new int[SUPPORTED_REPITITIONS * 2]; //2 for each rep
    #endregion

    private void updateVectorRenderingZones()
    {
        if (material == null)
            return;

        // TODO: This was the original code here. Handle correctly
        // var mi = apiGetMonitorInfo();
        // material.SetVector(
        //     id_viewPositions,
        //     new Vector4(
        //         0
        //             - mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2
        //             - mi.VectorMapStereoZoneWidthHeadTracking * stereo_zone_width,
        //         0 - mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2,
        //         0 + mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2,
        //         0
        //             + mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2
        //             + mi.VectorMapStereoZoneWidthHeadTracking * stereo_zone_width
        //     )
        // );
        material.SetVector(id_viewPositions, new Vector4(0, 0, 0, 0));
    }

    private void reinitializeShader()
    {
        material = null;
        // TODO Handle this case
        // if (apiGetMonitorInfo().ViewCount < 2)
        //     return; //null material will be handled by the features as blitting

        switch (rendermode)
        {
            case 0: //algo
#if HDRP
                material = new Material(Shader.Find("G3D/AlgoShaderHDRP"));
#elif URP
                material = new Material(Shader.Find("G3D/AlgoShaderURP"));
#else
                material = new Material(Shader.Find("G3D/AlgoShader"));
#endif
                setMonitorViewcount();
                setAngleCounter();
                setAngleDenominator();
                setAlgoDirection();
                setViewOffset();
                setIndexMap();

                break;
            case 1: //viewmap
#if HDRP
                material = new Material(Shader.Find("G3D/ViewmapShaderHDRP"));
#elif URP
                material = new Material(Shader.Find("G3D/ViewmapShaderURP"));
#else
                material = new Material(Shader.Find("G3D/ViewmapShader"));
#endif
                if (cache_loadedViewmap != null)
                    material.SetTexture(id_ViewMap, cache_loadedViewmap);
                setMonitorViewcount();
                setViewOffset();
                setViewCount();
                setIndexMap();
                break;
            case 2: //vector
#if HDRP
                material = new Material(Shader.Find("G3D/VectorShaderHDRP"));
#elif URP
                material = new Material(Shader.Find("G3D/VectorShaderURP"));
#else
                material = new Material(Shader.Find("G3D/VectorShader"));
#endif

                material.SetTexture(id_PositionMap, cache_positionMap);
                material.SetTexture(id_VectorMap, cache_vectorMap);

                for (
                    int i = 0;
                    i < cache_vectorIndexMaps.Length && i < SUPPORTED_REPITITIONS * 2;
                    i++
                )
                    material.SetTexture(id_VectorIndexMap[i], cache_vectorIndexMaps[i]);

                updateVectorRenderingZones();
                break;
            case 3: //ztracking
#if HDRP
                material = new Material(Shader.Find("G3D/ZTrackingShaderHDRP"));
#elif URP
                material = new Material(Shader.Find("G3D/ZTrackingShaderURP"));
#else
                material = new Material(Shader.Find("G3D/ZTrackingShader"));
#endif
                setMonitorViewcount();
                setAngleCounter();
                setAngleDenominator();
                setAlgoDirection();
                setViewOffset();
                setIndexMap();
                break;
        }

        updateShaderViews();
        setWindowPosition();
    }

    private void setIndexMap()
    {
        if (rendermode == 2)
            return; //vectorrendering does not use the indexmap
        if (material == null)
            return;

        float[] indexMap = new float[cache_views_monitor];

        if (cache_views_monitor == 1)
        {
            //no 3d
            indexMap[0] = 0;
        }
        else if (cameracount == 2)
        {
            //stereo is optimized

            //value sanity check
            float sds = stereo_delimiter_space;
            float eas = stereo_eyearea_space;
            if (sds + 2 * eas > 1.0)
            {
                float diff = (sds + 2 * eas) - 1.0f;
                sds -= diff / 3;
                eas -= (diff / 3) * 2;
            }

            int delimViews = (int)(cache_views_monitor * sds);
            int eyeViews = (int)(cache_views_monitor * eas);
            int startDelimViews = (cache_views_monitor - delimViews - eyeViews * 2) / 2;
            int endDelimViews = cache_views_monitor - delimViews - eyeViews * 2 - startDelimViews;

            int i = 0;
            for (; startDelimViews > 0; startDelimViews--)
                indexMap[i++] = 255f;
            for (int j = 0; j < eyeViews; j++)
                indexMap[i++] = 0f;
            for (; delimViews > 0; delimViews--)
                indexMap[i++] = 255f;
            for (int j = 0; j < eyeViews; j++)
                indexMap[i++] = 1f;
            for (; endDelimViews > 0; endDelimViews--)
                indexMap[i++] = 255f;
        }
        else
        {
            //multiview is spaced as evenly as possible, starting and ending with a delimiter view
            int spacePerView = (cache_views_monitor - 2) / cameracount;
            int spaceLeftover = (cache_views_monitor - 2) % cameracount;

            int i = 0;
            indexMap[i++] = 255f;
            for (int viewIndex = 0; viewIndex < cameracount; viewIndex++)
            {
                int offset = 0;
                if (spaceLeftover > 0)
                {
                    offset = 1;
                    spaceLeftover--;
                }

                for (int currentSpace = spacePerView + offset; currentSpace > 0; currentSpace--)
                {
                    indexMap[i++] = viewIndex;
                }
            }
            indexMap[i++] = 255f;
        }

        material.SetFloatArray(id_indexMap, indexMap);
    }

    private void setViewCount()
    {
        if (rendermode == 2)
            return; //vectorrendering does not use a viewcount
        material?.SetFloat(id_view_count, cameracount);
    }

    private void setWindowPosition()
    {
        material?.SetVector(
            id_windowPosition,
            new Vector4(
                Screen.mainWindowPosition.x,
                Screen.mainWindowPosition.y,
                Screen.width,
                Screen.height
            )
        );
    }

    private void setViewOffset()
    {
        if (rendermode == 2)
            return; //vectorrendering does not use a viewcount
        material?.SetFloat(id_view_offset, viewshift);
    }

    private void setAngleCounter()
    {
        if (rendermode == 1 || rendermode == 2)
            return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_angle_counter, algo_angle_counter);
    }

    private void setAngleDenominator()
    {
        if (rendermode == 1 || rendermode == 2)
            return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_angle_denominator, algo_angle_denominator);
    }

    private void setAlgoDirection()
    {
        if (rendermode == 1 || rendermode == 2)
            return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_direction, algo_direction ? 1 : -1);
    }

    private void setBlurFactor()
    {
        if (rendermode != 3)
            return;
        material?.SetInt(id_blur_factor, blur_factor);
    }

    private void setMonitorViewcount()
    {
        if (rendermode == 1 || rendermode == 2)
        {
            material?.SetInt(id_view_count_monitor_hq, cache_views_monitor);
        }
        else
        {
            material?.SetInt(id_view_count_monitor_hq, algo_hqviews);
        }
    }

    public void updateShaderViews()
    {
        if (material == null)
            return;
        if (cameras == null)
            return;

        //prevent any memory leaks
        for (int i = 0; i < MAX_CAMERAS; i++)
            cameras[i].targetTexture?.Release();

        //set only those we need
        for (int i = 0; i < cameracount; i++)
        {
            Texture tex;
            if (testviews_active)
                tex = testviews[i % 16];
            else if (testcolors_active)
                tex = testcolors[i % 4];
            else
                tex = cameras[i].targetTexture = new RenderTexture(
                    (int)(Screen.width * resolution / 100),
                    (int)(Screen.height * resolution / 100),
                    0
                );

            material.SetTexture(id_View[i], tex);
        }
    }

    public static Material GetMaterial()
    {
        return material;
    }

    #endregion


    #region Monobehaviour

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        //legacy support (no URP or HDRP)
        if (material == null)
            Graphics.Blit(source, destination);
        else
            Graphics.Blit(source, destination, material);
    }

    private void Awake()
    {
        //shader id caching
        id_windowPosition = Shader.PropertyToID("windowPosition");
        id_indexMap = Shader.PropertyToID("indexMap");
        id_view_count = Shader.PropertyToID("view_count");
        id_algo_angle_counter = Shader.PropertyToID("angle_counter");
        id_algo_angle_denominator = Shader.PropertyToID("angle_denominator");
        id_algo_direction = Shader.PropertyToID("direction");
        id_blur_factor = Shader.PropertyToID("blur_factor");
        id_view_count_monitor_hq = Shader.PropertyToID("view_count_monitor_hq");
        id_view_offset = Shader.PropertyToID("view_offset");
        id_view_offset_headtracking = Shader.PropertyToID("view_offset_headtracking");
        id_userPosition = Shader.PropertyToID("userPosition");
        id_viewPositions = Shader.PropertyToID("viewPositions");
        id_ViewMap = Shader.PropertyToID("_ViewMap");
        id_PositionMap = Shader.PropertyToID("_PositionMap");
        id_VectorMap = Shader.PropertyToID("_VectorMap");

        for (int i = 0; i < MAX_CAMERAS; i++)
            id_View[i] = Shader.PropertyToID("_View_" + i);

        for (int i = 0; i < SUPPORTED_REPITITIONS * 2; i++)
            id_VectorIndexMap[i] = Shader.PropertyToID("_VectorIndexMap" + i);

        //load test textures only once
        testviews = new Texture[16];
        for (int i = 0; i < 16; i++)
            testviews[i] = Resources.Load<Texture>("testviews/" + i);

        testcolors = new Texture[4];
        for (int i = 0; i < 4; i++)
            testcolors[i] = Resources.Load<Texture>("testcolors/" + i);

        //reset test toggles
        XmlSettings.Instance.SetValue(XmlSettingsKey.TESTVIEWS, false.ToString());
        XmlSettings.Instance.SetValue(XmlSettingsKey.TESTCOLORS, false.ToString());

        //rendering
        reinitializeShader();
    }

    private void Start()
    {
        //headtracking
        headTracking = GetComponent<G3DHeadTracking>();
        startCameraPos = transform.position;

        StartCoroutine(LateStart());
    }

    private IEnumerator LateStart()
    {
        yield return new WaitForEndOfFrame();
        //LateStart

        setCameras(); //fov bug fix
    }

    private float time_passed_since_last_update = 0;

    private void Update()
    {
        if (enable_headtracking)
        {
            // handle camera position change via hadtracking
            //headtracking values
            var trackingData = apiGetHeadtrackingData();
            Vector3 headPos;
            if (invert_headtracking)
            {
                // TODO check if this simple inversion is correct
                headPos = new Vector3(-trackingData.x, -trackingData.y, -trackingData.z);
            }
            else
            {
                headPos = new Vector3(trackingData.x, trackingData.y, trackingData.z);
            }
            transform.position = startCameraPos + headPos;

            switch (rendermode)
            {
                case 0: //algo
                case 1: //viewmap
                    material?.SetFloat(
                        id_view_offset_headtracking,
                        invert_headtracking
                            ? (cache_views_monitor - trackingData.w)
                            : trackingData.w
                    );
                    break;
                case 2: //vectorrendering
                    material?.SetVector(
                        id_userPosition,
                        new Vector3(
                            trackingData.x + (invert_headtracking ? viewshift : -viewshift) * 10, //this might be far from mature, but it does the trick
                            trackingData.y,
                            trackingData.z
                        )
                    );
                    break;
                case 3: //ztracking
                    material?.SetFloat(
                        id_view_offset_headtracking,
                        invert_headtracking
                            ? (cache_views_monitor - trackingData.w)
                            : trackingData.w
                    );
                    material?.SetVector(
                        id_userPosition,
                        new Vector3(trackingData.x, trackingData.y, trackingData.z)
                    );
                    break;
            }
        }

        //window pos
        setWindowPosition();

        if (!Screen.fullScreen)
        {
            time_passed_since_last_update += Time.unscaledDeltaTime;
            if (time_passed_since_last_update > 1) //just once a second
            {
                time_passed_since_last_update -= 1;

                //window handling
                if (windowResized())
                    setCameras();

                //monitor handling
                // TODO handle this case
                // if (apiMonitorChanged())
                //      reinitializeShader();
            }
        }
    }

    private Vector2 cache_window_dim;

    private bool windowResized()
    {
        var window_dim = new Vector2(Screen.width, Screen.height);
        if (cache_window_dim != window_dim)
        {
            cache_window_dim = window_dim;
            return true;
        }
        return false;
    }

    #endregion




    #region API
    private Vector4 apiGetHeadtrackingData()
    {
        HeadPosition headPosition = headTracking.getHeadPosition();
        return new Vector4(
            (float)headPosition.worldPosX,
            (float)headPosition.worldPosY,
            (float)headPosition.worldPosZ,
            1
        );
    }
    #endregion
}
