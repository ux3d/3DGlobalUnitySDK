using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class G3DCamera : MonoBehaviour, IXmlSettingsListener {

    #region Properties

    [Header("Camera")]
    public bool testviews_active = false;
    public bool testcolors_active = false;
    [Range(1.0f, 100.0f)] public float resolution = 100.0f;
    [Range(1, 16)] public int cameracount = 2;
    public bool camera_position_circular = false;
    [Range(0.00001f, 1.0f)] public float eyedistance = 0.65f;
    [Range(0f, 1f)] public float stereo_depth = 0.0f;
    [Range(-5f, 5f)] public float stereo_plane = 5f;
    [Range(0.00001f, 1.0f)] public float angle = 0.05f;
    [Range(1f, 200f)] public float distance = 30.0f;

    [Header("Modes")]
    public int rendermode = 1; //algo, viewmap, vector, ztracking
    [Range(0, 48)] public int viewshift = 0;
    public bool invert_headtracking = false;
    [Range(0f, 3f)] public float stereo_delimiter_space = 0.1f;
    [Range(0f, 5f)] public float stereo_eyearea_space = 0.4f;
    [Range(0f, 100f)] public float stereo_zone_distance = 1f;
    [Range(0f, 500f)] public float stereo_zone_width = 1f;
    [Range(1, 20)] public int algo_angle_counter = 1;
    [Range(1, 20)] public int algo_angle_denominator = 1;
    [Range(2, 100)] public int algo_hqviews = 10;
    public bool algo_direction = false;
    public int blur_factor = 200;

    #endregion


    #region Views

    public const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ..
    public const string CAMERA_NAME_PREFIX = "g3dcam_";

    private Camera maincamera = null;
    private List<Camera> cameras = null;
    private GameObject cameraParent = null;
    
    
    public void setCameras()
    {
        //initialize cameras
        if (cameras == null)
        {
            maincamera = GetComponent<Camera>();

            cameraParent = new GameObject("g3dcams");
            cameraParent.transform.parent = transform;

            cameras = new List<Camera>();
            for (int i = 0; i < MAX_CAMERAS; i++) {
                cameras.Add(new GameObject(CAMERA_NAME_PREFIX + i).AddComponent<Camera>());
                cameras[i].transform.SetParent(cameraParent.transform, true);
                cameras[i].gameObject.SetActive(false);
            }
        }

        var mi = apiGetMonitorInfo();


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
                    cameraParent.transform.position.x - ((float)(           distance * Math.Sin(currentView * angle * (Math.PI / 180)))),
                    cameraParent.transform.position.y,
                    cameraParent.transform.position.z + ((float)(distance - distance * Math.Cos(currentView * angle * (Math.PI / 180))))
                );
            }
            else
            {
                int ScreenWidth = Screen.currentResolution.width;

                // eye distance
                float EyeDistance = eyedistance * 100;

                // calculate eye distance in pixel
                int StereoViewIPDOffset = (int)currentView * (int)(EyeDistance / mi.MonitorWidth * ScreenWidth / 2);  // offset for left/right eye in pixel (eye distance (in mm) / monitor width (in mm) * monitor width (in pixel) / 2)

                // get view size               
                int ViewWidth = camera.pixelWidth;

                // calculate offset for projection matrix
                float ProjOffset = StereoViewIPDOffset * stereo_depth / ViewWidth;  // real offset (pixel offset * factor / view size (fullscreen here))

                // calculate adjusted projection matrix
                Matrix4x4 tempMatrix = camera.projectionMatrix;  // original matrix
                tempMatrix[0, 2] = tempMatrix[0, 2] + ProjOffset;  // apply offset

                // calculate offset for view matrix
                float ViewOffset = 0.0f;
                float FC = tempMatrix[2, 2];
                float FD = tempMatrix[2, 3];
                if ((Math.Abs(tempMatrix[0, 0]) > 1E-3) && (Math.Abs(FC - 1) > 1E-4))  // projection matrix is valid and calculation possible
                {
                    float Near = ((FC + 1) / (FC - 1) - 1) / 2 * FD;  // near of current projection matrix
                    float DataWidth = 2 * Near / tempMatrix[0, 0];  // width
                    ViewOffset = (float)StereoViewIPDOffset / (float)ViewWidth * DataWidth * (float)(stereo_depth - (stereo_plane));
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
        if (material == null) return;

        var mi = apiGetMonitorInfo();

        material.SetVector(id_viewPositions, new Vector4(
            0 - mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2 - mi.VectorMapStereoZoneWidthHeadTracking * stereo_zone_width,
            0 - mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2,
            0 + mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2,
            0 + mi.VectorMapStereoZoneDistanceHeadTracking * stereo_zone_distance / 2 + mi.VectorMapStereoZoneWidthHeadTracking * stereo_zone_width
        ));
    }

    private void reinitializeShader()
    {
        material = null;
        if (apiGetMonitorInfo().ViewCount < 2) return; //null material will be handled by the features as blitting

        switch(rendermode)
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

                for (int i = 0; i < cache_vectorIndexMaps.Length && i < SUPPORTED_REPITITIONS * 2; i++)
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
        if (rendermode == 2) return; //vectorrendering does not use the indexmap
        if (material == null) return;

        float[] indexMap = new float[cache_views_monitor];

        if(cache_views_monitor == 1)
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
            if(sds + 2 * eas > 1.0)
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
            for (; startDelimViews > 0; startDelimViews--)  indexMap[i++] = 255f;
            for (int j = 0; j < eyeViews; j++)              indexMap[i++] = 0f;
            for (; delimViews > 0; delimViews--)            indexMap[i++] = 255f;
            for (int j = 0; j < eyeViews; j++)              indexMap[i++] = 1f;
            for (; endDelimViews > 0; endDelimViews--)      indexMap[i++] = 255f;
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
                if(spaceLeftover > 0)
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
        if (rendermode == 2) return; //vectorrendering does not use a viewcount
        material?.SetFloat(id_view_count, cameracount);
    }

    private void setWindowPosition()
    {
        material?.SetVector(id_windowPosition, new Vector4(Screen.mainWindowPosition.x, Screen.mainWindowPosition.y, Screen.width, Screen.height));
    }

    private void setViewOffset()
    {
        if (rendermode == 2) return; //vectorrendering does not use a viewcount
        material?.SetFloat(id_view_offset, viewshift);
    }

    private void setAngleCounter()
    {
        if (rendermode == 1 || rendermode == 2) return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_angle_counter, algo_angle_counter);
    }

    private void setAngleDenominator()
    {
        if (rendermode == 1 || rendermode == 2) return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_angle_denominator, algo_angle_denominator);
    }

    private void setAlgoDirection()
    {
        if (rendermode == 1 || rendermode == 2) return; //vectorrendering does not use a viewcount
        material?.SetInt(id_algo_direction, algo_direction ? 1 : -1);
    }

    private void setBlurFactor()
    {
        if (rendermode != 3) return;
        material?.SetInt(id_blur_factor, blur_factor);
    }

    private void setMonitorViewcount()
    {
        if (rendermode == 1 || rendermode == 2)
        {
            material?.SetInt(id_view_count_monitor_hq, cache_views_monitor);
        } else
        {
            material?.SetInt(id_view_count_monitor_hq, algo_hqviews);
        }
    }

    public void updateShaderViews() {
        if (material == null) return;
        if (cameras == null) return;

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
                tex = cameras[i].targetTexture = new RenderTexture((int)(Screen.width * resolution / 100), (int)(Screen.height * resolution / 100), 0);

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
        if(material == null) 
            Graphics.Blit(source, destination);
        else 
            Graphics.Blit(source, destination, material);
    }

    private void Awake()
    {
        //shader id caching
        id_windowPosition               = Shader.PropertyToID("windowPosition");
        id_indexMap                     = Shader.PropertyToID("indexMap");
        id_view_count                   = Shader.PropertyToID("view_count");
        id_algo_angle_counter           = Shader.PropertyToID("angle_counter");
        id_algo_angle_denominator       = Shader.PropertyToID("angle_denominator");
        id_algo_direction               = Shader.PropertyToID("direction");
        id_blur_factor                  = Shader.PropertyToID("blur_factor");
        id_view_count_monitor_hq        = Shader.PropertyToID("view_count_monitor_hq");
        id_view_offset                  = Shader.PropertyToID("view_offset");
        id_view_offset_headtracking     = Shader.PropertyToID("view_offset_headtracking");
        id_userPosition                 = Shader.PropertyToID("userPosition");
        id_viewPositions                = Shader.PropertyToID("viewPositions");
        id_ViewMap                      = Shader.PropertyToID("_ViewMap");
        id_PositionMap                  = Shader.PropertyToID("_PositionMap");
        id_VectorMap                    = Shader.PropertyToID("_VectorMap");

        for (int i = 0; i < MAX_CAMERAS; i++)
            id_View[i] = Shader.PropertyToID("_View_" + i);

        for (int i = 0; i < SUPPORTED_REPITITIONS * 2; i++)
            id_VectorIndexMap[i] = Shader.PropertyToID("_VectorIndexMap" + i);


        //settings initialization from script on first creation
        settingsAwake();

        //load test textures only once
        testviews = new Texture[16];
        for (int i = 0; i < 16; i++) testviews[i] = Resources.Load<Texture>("testviews/" + i);

        testcolors = new Texture[4];
        for (int i = 0; i < 4; i++) testcolors[i] = Resources.Load<Texture>("testcolors/" + i);

        //reset test toggles
        XmlSettings.Instance.SetValue(XmlSettingsKey.TESTVIEWS, false.ToString());
        XmlSettings.Instance.SetValue(XmlSettingsKey.TESTCOLORS, false.ToString());

        //api
        apiInitialize();

        //rendering
        apiLoadData();
        reinitializeShader();
    }

    private void Start() {
        settingsStart();

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
        //headtracking values
        var trackingData = apiGetHeadtrackingData();

        switch(rendermode)
        {
            case 0: //algo
            case 1: //viewmap
                material?.SetFloat(id_view_offset_headtracking, invert_headtracking
                ? (cache_views_monitor - trackingData.w)
                : trackingData.w);
                break;
            case 2: //vectorrendering
                material?.SetVector(id_userPosition, new Vector3(
                    trackingData.x + (invert_headtracking ? viewshift : -viewshift) * 10, //this might be far from mature, but it does the trick
                    trackingData.y,
                    trackingData.z));
                break;
            case 3: //ztracking
                material?.SetFloat(id_view_offset_headtracking, invert_headtracking
                ? (cache_views_monitor - trackingData.w)
                : trackingData.w);
                material?.SetVector(id_userPosition, new Vector3(
                    trackingData.x,
                    trackingData.y,
                    trackingData.z));
                break;

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
                if (windowResized()) setCameras();

                //monitor handling
                if(apiMonitorChanged()) reinitializeShader();
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


    private void OnApplicationQuit()
    {
        apiFree();
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;

        apiOnValidate();
    }

    private void OnDestroy()
    {
        settingsDestroy();
    }



    #endregion


    #region Settings

    private void settingsAwake()
    {
        if (XmlSettings.Instance.isFreshlyInitialized())
        {
            XmlSettings.Instance.SetValue(XmlSettingsKey.CAMERACOUNT, cameracount.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.VIEWSHIFT, viewshift.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.RESOLUTION, resolution.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.EYEDISTANCE, eyedistance.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.TESTVIEWS, testviews_active.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.TESTCOLORS, testcolors_active.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREODELIMITERSPACE, stereo_delimiter_space.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.MODE_VIEWMAP, rendermode.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREOEYEAREASPACE, stereo_eyearea_space.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREODEPTH, stereo_depth.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREOPLANE, stereo_plane.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.CAMPOSITIONCIRCULAR, camera_position_circular.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.CIRCLE_ANGLE, angle.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.CIRCLE_DISTANCE, distance.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREO_ZONE_DISTANCE, stereo_zone_distance.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.STEREO_ZONE_WIDTH, stereo_zone_width.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.INVERT_HEADTRACKING, invert_headtracking.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.ALGO_ANGLE_COUNTER, algo_angle_counter.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.ALGO_ANGLE_DENOMINATOR, algo_angle_denominator.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.ALGO_HQVIEWS, algo_hqviews.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.ALGO_DIRECTION, algo_direction.ToString());
            XmlSettings.Instance.SetValue(XmlSettingsKey.BLUR_FACTOR, blur_factor.ToString());
        }
    }

    private void settingsStart()
    {
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.CAMERACOUNT, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.VIEWSHIFT, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.RESOLUTION, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.TESTVIEWS, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.TESTCOLORS, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.EYEDISTANCE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREODELIMITERSPACE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREOEYEAREASPACE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.MODE_VIEWMAP, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREODEPTH, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREOPLANE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.CAMPOSITIONCIRCULAR, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.CIRCLE_ANGLE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.CIRCLE_DISTANCE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREO_ZONE_DISTANCE, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.STEREO_ZONE_WIDTH, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.INVERT_HEADTRACKING, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.ALGO_ANGLE_COUNTER, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.ALGO_ANGLE_DENOMINATOR, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.ALGO_HQVIEWS, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.ALGO_DIRECTION, this);
        XmlSettings.Instance.RegisterListener(XmlSettingsKey.BLUR_FACTOR, this);
    }

    private void settingsDestroy()
    {
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.CAMERACOUNT, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.VIEWSHIFT, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.RESOLUTION, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.TESTVIEWS, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.TESTCOLORS, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.EYEDISTANCE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREODELIMITERSPACE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREOEYEAREASPACE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.MODE_VIEWMAP, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREODEPTH, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREOPLANE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.CAMPOSITIONCIRCULAR, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.CIRCLE_ANGLE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.CIRCLE_DISTANCE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREO_ZONE_DISTANCE, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.STEREO_ZONE_WIDTH, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.INVERT_HEADTRACKING, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.ALGO_ANGLE_COUNTER, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.ALGO_ANGLE_DENOMINATOR, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.ALGO_HQVIEWS, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.ALGO_DIRECTION, this);
        XmlSettings.Instance.UnregisterListener(XmlSettingsKey.BLUR_FACTOR, this);
    }

    public void OnXmlValueChanged(XmlSettingsKey key, string value)
    {
        bool bvalue;
        int ivalue;
        float fvalue;

        switch (key)
        {
            case XmlSettingsKey.CAMERACOUNT:
                if (int.TryParse(value, out ivalue))
                {
                    cameracount = ivalue;
                    setViewCount();
                    setCameras();
                    setIndexMap();
                }
                break;
            case XmlSettingsKey.VIEWSHIFT:
                if (int.TryParse(value, out ivalue))
                {
                    viewshift = ivalue;
                    setViewOffset();
                }
                break;
            case XmlSettingsKey.RESOLUTION:
                if (float.TryParse(value, out fvalue))
                {
                    resolution = fvalue;
                    updateShaderViews();
                }
                break;
            case XmlSettingsKey.TESTVIEWS:
                if (bool.TryParse(value, out bvalue))
                {
                    testviews_active = bvalue;
                    updateShaderViews();
                }
                break;
            case XmlSettingsKey.TESTCOLORS:
                if (bool.TryParse(value, out bvalue))
                {
                    testcolors_active = bvalue;
                    updateShaderViews();
                }
                break;
            case XmlSettingsKey.EYEDISTANCE:
                if (float.TryParse(value, out fvalue))
                {
                    eyedistance = fvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.STEREODELIMITERSPACE:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_delimiter_space = fvalue;
                    setIndexMap();
                }
                break;
            case XmlSettingsKey.STEREOEYEAREASPACE:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_eyearea_space = fvalue;
                    setIndexMap();
                }
                break;
            case XmlSettingsKey.MODE_VIEWMAP:
                if (int.TryParse(value, out ivalue))
                {
                    rendermode = ivalue;
                    reinitializeShader();
                }
                break;
            case XmlSettingsKey.STEREODEPTH:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_depth = fvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.STEREOPLANE:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_plane = fvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.CAMPOSITIONCIRCULAR:
                if (bool.TryParse(value, out bvalue))
                {
                    camera_position_circular = bvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.CIRCLE_ANGLE:
                if (float.TryParse(value, out fvalue))
                {
                    angle = fvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.CIRCLE_DISTANCE:
                if (float.TryParse(value, out fvalue))
                {
                    distance = fvalue;
                    setCameras();
                }
                break;
            case XmlSettingsKey.STEREO_ZONE_DISTANCE:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_zone_distance = fvalue;
                    updateVectorRenderingZones();
                }
                break;
            case XmlSettingsKey.STEREO_ZONE_WIDTH:
                if (float.TryParse(value, out fvalue))
                {
                    stereo_zone_width = fvalue;
                    updateVectorRenderingZones();
                }
                break;
            case XmlSettingsKey.INVERT_HEADTRACKING:
                if (bool.TryParse(value, out bvalue))
                {
                    invert_headtracking = bvalue;
                }
                break;
            case XmlSettingsKey.ALGO_ANGLE_COUNTER:
                if (int.TryParse(value, out ivalue))
                {
                    algo_angle_counter = ivalue;
                    setAngleCounter();
                }
                break;
            case XmlSettingsKey.ALGO_ANGLE_DENOMINATOR:
                if (int.TryParse(value, out ivalue))
                {
                    algo_angle_denominator = ivalue;
                    setAngleDenominator();
                }
                break;
            case XmlSettingsKey.ALGO_HQVIEWS:
                if (int.TryParse(value, out ivalue))
                {
                    algo_hqviews = ivalue;
                    setMonitorViewcount();
                }
                break;
            case XmlSettingsKey.ALGO_DIRECTION:
                if (bool.TryParse(value, out bvalue))
                {
                    algo_direction = bvalue;
                    setAlgoDirection();
                }
                break;
            case XmlSettingsKey.BLUR_FACTOR:
                if (int.TryParse(value, out ivalue))
                {
                    blur_factor = ivalue;
                    setBlurFactor();
                }
                break;
        }
    }


    #endregion


    #region API
    
    private void apiOnValidate()
    {
        G3DAPI.free(); //make sure we unload this if coming from the editor
    }

    private void apiInitialize()
    {
    #if UNITY_EDITOR
        G3DAPI.init($"{Application.productName} - {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} - Windows, Mac, Linux - Unity {Application.unityVersion} Personal <DX11>"); //todo: totally reconstruct unity editors name or find a better way
    #else
        G3DAPI.init(Application.productName);
    #endif

        G3DAPI.update();
    }

    private void apiLoadData()
    {
        loadViewmap(G3DAPI.GetCurrentViewmap());
        loadVectorMap(G3DAPI.GetCurrentVectormap());
    }

    private bool apiMonitorChanged()
    {
        if (G3DAPI.detectMonitorChange())
        {
            G3DAPI.update();
            if (G3DAPI.GetCurrentMonitorInfo().Value.ViewCount > 1) apiLoadData();
            return true;
        }
        return false;
    }

    private Vector4 apiGetHeadtrackingData()
    {
        var state = G3DAPI.G3DHeadTrackingGetStateA();
        if (state == null) return Vector4.zero;
        return new Vector4(state.Value.EyeCenterPosMMV1.X, state.Value.EyeCenterPosMMV1.Y, state.Value.EyeCenterPosMMV1.Z, state.Value.TrackingOffset);
    }

    private void loadViewmap(G3DAPI.Viewmap viewmap)
    {
        if (viewmap == null) return;

        Color32[] data = new Color32[viewmap.width * viewmap.height];

        //format in memory is bgr, but we need rgb
        for (int y = 0; y < viewmap.height; y++) for (int x = 0; x < viewmap.width; x++)
            {
                data[y * viewmap.width + x] = new Color32(
                    viewmap.data[y * viewmap.width * 3 + x * 3 + 2],
                    viewmap.data[y * viewmap.width * 3 + x * 3 + 1],
                    viewmap.data[y * viewmap.width * 3 + x * 3 + 0],
                    255
                );
            }

        Destroy(cache_loadedViewmap);
        cache_loadedViewmap = new Texture2D(viewmap.width, viewmap.height, GraphicsFormat.R8G8B8A8_UNorm, 0);
        cache_views_monitor = viewmap.viewcount;

        cache_loadedViewmap.filterMode = FilterMode.Point;
        cache_loadedViewmap.SetPixels32(data);
        cache_loadedViewmap.Apply();
    }

    private void loadVectorMap(G3DAPI.Vectormap vectormap)
    {
        if (vectormap == null) return;

        //position map
        Destroy(cache_positionMap);
        cache_positionMap = new Texture2D(vectormap.data.PositionMap.Length, 1, TextureFormat.RFloat, false);
        for (int x = 0; x < vectormap.data.PositionMap.Length; x++)
            cache_positionMap.SetPixel(x, 0, new Color(vectormap.data.PositionMap[x], 0, 0));
        cache_positionMap.filterMode = FilterMode.Point;
        cache_positionMap.Apply();

        //vectormap
        Destroy(cache_vectorMap);
        cache_vectorMap = new Texture2D(vectormap.data.VectorMap.Length, 1, TextureFormat.RFloat, false);
        for (int x = 0; x < vectormap.data.VectorMap.Length; x++)
            cache_vectorMap.SetPixel(x, 0, new Color(vectormap.data.VectorMap[x], 0, 0));
        cache_vectorMap.filterMode = FilterMode.Point;
        cache_vectorMap.Apply();

        //vector index maps
        for(int i = 0; cache_vectorIndexMaps != null && i < cache_vectorIndexMaps.Length; i++) 
            Destroy(cache_vectorIndexMaps[i]);
        cache_vectorIndexMaps = new Texture2D[vectormap.info.Repetition * 2];
        for (int currentRepitition = 0; currentRepitition < vectormap.info.Repetition; currentRepitition++)
        {
            var indexData = vectormap.indexMaps[currentRepitition];

            //unfortunately, no fitting and widely available 16bit format is available here.
            //values are split in high and low and reconstructed in the shader.
            //you know a better way? do it!
            cache_vectorIndexMaps[currentRepitition * 2 + 0] = new Texture2D((int)vectormap.info.Width, (int)vectormap.info.Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None); //L bits
            cache_vectorIndexMaps[currentRepitition * 2 + 1] = new Texture2D((int)vectormap.info.Width, (int)vectormap.info.Height, GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None); //H bits
            cache_vectorIndexMaps[currentRepitition * 2 + 0].filterMode = FilterMode.Point;
            cache_vectorIndexMaps[currentRepitition * 2 + 1].filterMode = FilterMode.Point;

            Color32[] L = new Color32[vectormap.info.Width * vectormap.info.Height];
            Color32[] H = new Color32[vectormap.info.Width * vectormap.info.Height];
            for (int i_src = 0, i_dst = 0; i_src < indexData.Indices.Length; i_src += 6, i_dst++)
            {
                //format in memory: bbggrr
                L[i_dst] = new Color32(indexData.Indices[i_src + 4], indexData.Indices[i_src + 2], indexData.Indices[i_src + 0], 255);
                H[i_dst] = new Color32(indexData.Indices[i_src + 5], indexData.Indices[i_src + 3], indexData.Indices[i_src + 1], 255);
            }
            cache_vectorIndexMaps[currentRepitition * 2 + 0].SetPixels32(L);
            cache_vectorIndexMaps[currentRepitition * 2 + 1].SetPixels32(H);

            cache_vectorIndexMaps[currentRepitition * 2 + 0].Apply();
            cache_vectorIndexMaps[currentRepitition * 2 + 1].Apply();
        }
    }

    private G3DAPI.TG3DMonitorInfoV1 apiGetMonitorInfo()
    {
        return G3DAPI.GetCurrentMonitorInfo() ?? new G3DAPI.TG3DMonitorInfoV1();
    }

    private void apiFree()
    {
        G3DAPI.free();
    }

    #endregion

}