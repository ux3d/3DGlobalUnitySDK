using System;
using System.Collections.Generic;
using System.IO;
using G3D.RenderPipeline;
using UnityEngine;
using UnityEngine.Rendering;
#if G3D_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

#if G3D_URP
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#endif

namespace G3D
{
    /// <summary>
    /// IMPORTANT: This script must not be attached to a camera already using a G3D camera script.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class G3DCamera : MonoBehaviour
    {
        [Tooltip("Drop the configuration file for the display you want to use here.")]
        public TextAsset configurationFile;

        [Tooltip(
            "This path has to be set to the directory where the folder containing the configuration files for your monitor are located. The folder has to have the same name as your camera model."
        )]
        /// <summary>
        /// This path has to be set to the directory where the folder containing the configuration files for your monitor are located. The folder has to have the same name as your camera model.
        /// </summary>
        public string configurationPathOverwrite = "";

        public G3DCameraMode mode = G3DCameraMode.MULTIVIEW;

        /// <summary>
        /// prefix added to the cameras created by this script.
        /// </summary>
        public static string CAMERA_NAME_PREFIX = "g3dcam_";

        [Tooltip(
            "Set a percentage value to render only that percentage of the width and height per view. E.g. a reduction of 50% will reduce the rendered size by a factor of 4."
        )]
        [Range(1, 100)]
        /// <summary>
        /// "Set a percentage value to render only that percentage of the width and height per view.
        /// E.g. a reduction of 50% will reduce the rendered size by a factor of 4.

        /// </summary>
        public int renderResolutionScale = 100;

        [Tooltip(
            "Adjust the dolly zoom effect. 1.0 means no dolly zoom. 0.0 means large fov and minimum distance to the focus plane. 3.0 means small fov and maximum distance from the focus plane."
        )]
        [Range(0.001f, 3)]
        public float dollyZoom = 1;

        [Tooltip(
            "Scale the distance between the views (cameras). 1.0 is no scaling, 0.5 is half the distance, 2.0 is double the distance. Native distance depends on configuration file."
        )]
        [Range(0.0f, 20.0f)]
        /// <summary>
        /// Scale the distance between the views (cameras). 
        /// 1.0 is no scaling, 0.5 is half the distance, 2.0 is double the distance. 
        /// Native distance depends on configuration file.
        /// </summary>
        public float viewOffsetScale = 1.0f;

        /// <summary>
        /// Distance between the cameras and the focus plane in meters. Native value depends on configuration file.
        /// </summary>
        [Tooltip(
            "Distance between the cameras and the focus plane in meters. Native value depends on configuration file."
        )]
        [Min(0.0f)]
        public float focusDistance = 0.7f;

        /// <summary>
        /// Shifts the individual views to the left or right by the specified number of views.
        /// This does not shift the cameras. This shifts the views you see on the display.
        /// </summary>
        [Tooltip(
            "Shifts the individual views to the left or right by the specified number of views."
        )]
        public int viewOffset = 0;

        [Tooltip(
            "Scales the strength of the camera movement through headtracking. Below 1.0 camera movement is reduced compared to real world movement. Above 1.0 camera movement is increased compared to real world movement."
        )]
        [Min(0.0f)]
        /// <summary>
        /// Scale the headtracking effect. Dont set this lower than 0.0f.
        /// </summary>
        public float headtrackingSensitivity = 1.0f; // scale the headtracking effect

        #region Advanced settings
        /// <summary>
        /// Smoothes the head position (Size of the filter kernel). No filtering is applied, if set to all zeros. DO NOT CHANGE THIS WHILE GAME IS ALREADY RUNNING!
        /// </summary>
        public Vector3Int headPositionFilter = new Vector3Int(5, 5, 5);

        /// <summary>
        /// How latency correction is handled by the headtracking library.
        /// </summary>
        public LatencyCorrectionMode latencyCorrectionMode = LatencyCorrectionMode.LCM_SIMPLE;

        [Tooltip(
            "If set to true, the head tracking library will print debug messages to the console."
        )]
        /// <summary>
        /// If set to true, the head tracking library will print debug messages to the console.
        /// </summary>
        public bool debugMessages = false;

        [Tooltip(
            "Show the positions, frustums and focus plane of the generated cameras in the Scene view."
        )]
        public bool showGizmos = true;

        [Tooltip("Scale of the camera related gizmos in the Scene view.")]
        [Range(0.005f, 5.0f)]
        public float gizmoSize = 1.0f;

        [Tooltip("Switch position of the left and right view.")]
        public bool invertViewsInHeadtracking = false;

        [Tooltip(
            "Define the position where the views start to be ordered backwards (yoyo pattern) in the index map. Index map contains the order of views."
        )]
        [Range(0, 100)]
        /// <summary>
        /// Where the views start to yoyo in the index map in percent. Index map contains the order of views.
        /// [0, 1, 2, 3, 4, 5, 6, 7] with yoyo start at 50% would become [6, 5, 4, 3, 4, 5, 6]
        /// </summary>
        public int indexMapYoyoStart = 0;

        [Tooltip("Inverts the entire index map. Index map contains the order of views.")]
        /// <summary>
        /// Inverts the entire index map. Index map contains the order of views.
        /// [0, 1, 2, 3, 4, 5, 6, 7] would become [7, 6, 5, 4, 3, 2, 1, 0]
        /// </summary>
        public bool invertIndexMap = false;

        [Tooltip("Inverts the individual indices in the index map. Index map contains the order of views.")]
        /// <summary>
        /// Inverts the individual indices in the index map. Index map contains the order of views.
        /// [6, 5, 4, 3, 4, 5, 6] would become [0, 1, 2, 3, 2, 1, 0]
        /// </summary>
        public bool invertIndexMapIndices = false;

        public HeadtrackingConnection headtrackingConnection;

        #endregion

        #region Private variables
        private IndexMap indexMap = IndexMap.Instance;

        // distance between the two cameras for headtracking mode (in meters). DO NOT USE FOR MULTIVIEW MODE!
        private float viewSeparation = 0.065f;

        private const int MAX_CAMERAS = 16; //shaders dont have dynamic arrays and this is the max supported. change it here? change it in the shaders as well ...
        private int internalCameraCount = 2;
        private int oldRenderResolutionScale = 100;
        
        // mirrorViews is used to flip the views horizontally, this is required for Holoboxes
        private bool mirrorViews = false;

        private static object shaderLock = new object();

        private Camera mainCamera;
        private List<Camera> cameras = null;
        private GameObject focusPlaneObject = null;
        private GameObject cameraParent = null;

        private Material material;
#if G3D_HDRP
        private HDAdditionalCameraData.AntialiasingMode antialiasingMode = HDAdditionalCameraData
            .AntialiasingMode
            .None;
#endif
#if G3D_URP
        private G3D.RenderPipeline.URP.ScriptableRP customPass;
        private UnityEngine.Rendering.Universal.AntialiasingMode antialiasingMode = UnityEngine
            .Rendering
            .Universal
            .AntialiasingMode
            .None;
#endif

        private ShaderHandles shaderHandles;
        private G3DShaderParameters shaderParameters;

        private Vector2Int cachedWindowPosition;
        private Vector2Int cachedWindowSize;

        /// <summary>
        /// This value is calculated based on the configuration file
        /// </summary>
        private float displayFOV = 16.0f;

        private bool showTestFrame = false;

        private float scaledViewSeparation
        {
            get { return viewSeparation * viewOffsetScale; }
        }

        private float focusDistWithDollyZoom
        {
            get { return focusDistance * dollyZoom; }
        }

        #endregion


        private RenderTexture[] colorRenderTextures = null;
        private int mainCamCullingMask = -1;
        private CameraClearFlags originalMainClearFlags;

#if G3D_HDRP
        private G3D.RenderPipeline.HDRP.CustomPassController customPassController;
#endif

        private bool mainCamInactiveLastFrame = false;

        #region Initialization

        void Awake()
        {
            InitMainCamera();
        }

        void Start()
        {
            oldRenderResolutionScale = renderResolutionScale;
            setupCameras();

            reinitializeShader();

#if G3D_HDRP
            customPassController =
                gameObject.AddComponent<G3D.RenderPipeline.HDRP.CustomPassController>();
            customPassController.init(ref material);
#endif

#if G3D_URP
            customPass = new G3D.RenderPipeline.URP.ScriptableRP(material);
            antialiasingMode = mainCamera.GetUniversalAdditionalCameraData().antialiasing;
            mainCamera.GetUniversalAdditionalCameraData().antialiasing = UnityEngine
                .Rendering
                .Universal
                .AntialiasingMode
                .None;
#endif

            shaderHandles = new ShaderHandles();
            shaderHandles.init();

            headtrackingConnection = new HeadtrackingConnection(
                focusDistance,
                headtrackingSensitivity,
                configurationPathOverwrite,
                this,
                debugMessages,
                headPositionFilter,
                latencyCorrectionMode
            );
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                headtrackingConnection.initLibrary();
                headtrackingConnection.startHeadTracking();
            }

            updateScreenViewportProperties();

            loadShaderParametersFromConfigurationFile();
            updateShaderParameters();

            updateCameras();
            updateRenderTextures();

            // This has to be done after the cameras are updated
            cachedWindowPosition = new Vector2Int(
                Screen.mainWindowPosition.x,
                Screen.mainWindowPosition.y
            );
            cachedWindowSize = new Vector2Int(Screen.width, Screen.height);

            indexMap.UpdateIndexMap(
                getCameraCountFromConfigurationFile(),
                internalCameraCount,
                indexMapYoyoStart / 100.0f,
                invertIndexMap,
                invertIndexMapIndices
            );
        }

        void OnApplicationQuit()
        {
            headtrackingConnection.deinitLibrary();
        }

        private void OnEnable()
        {
            InitMainCamera();

            mainCamera.cullingMask = 0; //disable rendering of the main camera
            mainCamera.clearFlags = CameraClearFlags.Color;

#if G3D_URP
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
#endif
        }

        public void OnDisable()
        {
            if (cameras != null && cameras.Count > 0)
            {
                // disable all cameras when the script is disabled
                for (int i = 0; i < MAX_CAMERAS; i++)
                {
                    cameras[i]?.gameObject.SetActive(false);
                }
            }

            mainCamera.cullingMask = mainCamCullingMask;
            mainCamera.clearFlags = originalMainClearFlags;

#if G3D_URP
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
#endif
        }

#if G3D_URP
        private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
        {
            // Use the EnqueuePass method to inject a custom render pass
            cam.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(customPass);

            if (mainCamera.GetUniversalAdditionalCameraData().renderPostProcessing)
            {
                for (int i = 0; i < MAX_CAMERAS; i++)
                {
                    cameras[i].GetUniversalAdditionalCameraData().renderPostProcessing = true;
                }
            }
        }
#endif

        /// <summary>
        /// Call this function after the mode has been changed (e.g. multiview to headtracking)
        /// </summary>
        public void updateMode()
        {
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                viewSeparation = 0.065f;
            }
            else // Holobox and Multiview mode
            {
                ConfigurationProvider configuration = ConfigurationProvider.getFromString(
                    configurationFile.text
                );
                internalCameraCount = getCameraCountFromConfigurationFile(configuration);
                loadMultiviewViewSeparationFromConfiguration(configuration);
            }

            if (mode == G3DCameraMode.HOLOBOX)
            {
                mirrorViews = true;
            }
            else
            {
                mirrorViews = false;
            }

            updateCameraCountBasedOnMode();
        }

        public void updateIndexMap()
        {
            indexMap.UpdateIndexMap(
                getCameraCountFromConfigurationFile(),
                internalCameraCount,
                indexMapYoyoStart / 100.0f,
                invertIndexMap,
                invertIndexMapIndices
            );
        }

        public string indexMapToString()
        {
            return indexMap.currentMapToString();
        }

        /// <summary>
        /// Load shader parameters from current configuration file.
        /// </summary>
        public void loadShaderParametersFromConfigurationFile()
        {
            if (configurationFile == null)
            {
                Debug.LogError(
                    "No configuration file set. Please set a configuration file. Using default values."
                );
                return;
            }

            lock (shaderLock)
            {
                ConfigurationProvider configurationProvider = ConfigurationProvider.getFromString(
                    configurationFile.text
                );
                shaderParameters = configurationProvider.getShaderParameters();
            }
        }

        /// <summary>
        /// Updates all camera parameters based on the configuration file (i.e. focus distance, fov, etc.).
        /// Includes shader parameters (i.e. lense shear angle, camera count, etc.).
        ///
        /// Updates configuration file as well.
        /// </summary>
        public void setupCameras(TextAsset configurationFile)
        {
            this.configurationFile = configurationFile;
            setupCameras();
        }

        /// <summary>
        /// Sets up all camera parameters based on the configuration file (i.e. focus distance, fov, etc.).
        /// Includes shader parameters (i.e. lense shear angle, camera count, etc.).
        ///
        /// Does not update configuration file.
        /// </summary>
        public void setupCameras(bool updateFocusDist = false)
        {
            if (mainCamera == null)
            {
                InitMainCamera();
            }
            if (Application.isPlaying)
            {
                // only run this code if not in editor mode
                initCamerasAndParents();
            }
            if (configurationFile == null)
            {
                Debug.LogError(
                    "No configuration file set. Please set a configuration file. Using default values."
                );
                if (updateFocusDist)
                {
                    updateFocusDistance(0.7f);
                }
                headtrackingConnection?.setBasicWorkingDistance(focusDistance);

                if (mode == G3DCameraMode.HEADTRACKING)
                {
                    viewSeparation = 0.065f;
                }
                else // Holobox and Multiview mode
                {
                    viewSeparation = 0.031f;
                }

                return;
            }

            // load values from configuration file
            ConfigurationProvider configuration = ConfigurationProvider.getFromString(
                configurationFile.text
            );
            int BasicWorkingDistanceMM = configuration.getInt("BasicWorkingDistanceMM");
            float PhysicalSizeInch = configuration.getFloat("PhysicalSizeInch");
            int NativeViewcount = configuration.getInt("NativeViewcount");
            int HorizontalResolution = configuration.getInt("HorizontalResolution");
            int VerticalResolution = configuration.getInt("VerticalResolution");

            // calculate intermediate values
            float BasicWorkingDistanceMeter = BasicWorkingDistanceMM / 1000.0f;
            float physicalSizeInMeter = PhysicalSizeInch * 0.0254f;
            float aspectRatio = (float)HorizontalResolution / (float)VerticalResolution;
            float FOV =
                2
                * Mathf.Atan(physicalSizeInMeter / 2.0f / BasicWorkingDistanceMeter)
                * Mathf.Rad2Deg;
            // set camera fov
            displayFOV = Camera.HorizontalToVerticalFieldOfView(FOV, aspectRatio);

            // set focus distance
            if (updateFocusDist)
            {
                updateFocusDistance(BasicWorkingDistanceMeter);
            }
            headtrackingConnection?.setBasicWorkingDistance(BasicWorkingDistanceMeter);

            // calculate eye separation/ view separation
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                viewSeparation = 0.065f;
            }
            else // Holobox and Multiview mode
            {
                loadMultiviewViewSeparationFromConfiguration(configuration);
                internalCameraCount = NativeViewcount;
            }

            updateCameraCountBasedOnMode();

            loadShaderParametersFromConfigurationFile();
        }

        public void updateRenderTextures()
        {
            if (material == null)
                return;
            if (cameras == null)
                return;

            //prevent any memory leaks
            for (int i = 0; i < MAX_CAMERAS; i++)
                cameras[i].targetTexture?.Release();

            for (int i = 0; i < colorRenderTextures?.Length; i++)
            {
                if (colorRenderTextures[i] != null)
                    colorRenderTextures[i].Release();
            }

            colorRenderTextures = new RenderTexture[internalCameraCount];

            //set only those we need
            for (int i = 0; i < internalCameraCount; i++)
            {
                addRenderTextureToCamera(colorRenderTextures, i, i);
            }
        }

        public void logCameraPositionsToFile()
        {
            headtrackingConnection.logCameraPositionsToFile();
        }

        /// <summary>
        /// Shifts views to the left. This does not shift the cameras. This shifts the views you see on the display.
        /// Only works in headtracking mode, in multiview use viewOffset.
        /// </summary>
        public void shiftViewToLeft()
        {
            if (mode != G3DCameraMode.HEADTRACKING)
            {
                return;
            }

            headtrackingConnection.shiftViewToLeft();
        }

        /// <summary>
        /// Shifts views to the right. This does not shift the cameras. This shifts the views you see on the display.
        /// Only works in headtracking mode, in multiview use viewOffset.
        /// </summary>
        public void shiftViewToRight()
        {
            if (mode != G3DCameraMode.HEADTRACKING)
            {
                return;
            }

            headtrackingConnection.shiftViewToRight();
        }

        public void toggleTestFrame()
        {
            showTestFrame = !showTestFrame;
        }

        public void toggleHeadTracking()
        {
            if (mode != G3DCameraMode.HEADTRACKING)
            {
                return;
            }

            headtrackingConnection.toggleHeadTracking();
        }

        public G3DShaderParameters GetShaderParameters()
        {
            lock (shaderLock)
            {
                return shaderParameters;
            }
        }

        public void setShaderParameters(G3DShaderParameters parameters)
        {
            lock (shaderLock)
            {
                shaderParameters = parameters;
            }
        }

        /// <summary>
        /// Set the cameras FOV to the fov calculated from the configuration file.
        /// Sets the filed of view to the natural field of view the display actually covers in your field of vision if you sit at the recommended distance.
        /// </summary>
        public void setCameraFOVToDisplayFOV()
        {
            // set to display FOV from configuration file
            mainCamera.fieldOfView = displayFOV;
        }

        /// <summary>
        /// Set the focus distance to the native focus distance of the dispaly. Native focus distance is the distance the viewer has to be from the display for the 3d effect to look best.
        /// </summary>
        public void setFocusDistanceToDisplay()
        {
            if (configurationFile == null)
            {
                Debug.LogError(
                    "No configuration file set. Please set a configuration file. Using default values."
                );
                updateFocusDistance(0.7f);
                return;
            }

            ConfigurationProvider configuration = ConfigurationProvider.getFromString(
                configurationFile.text
            );
            int BasicWorkingDistanceMM = configuration.getInt("BasicWorkingDistanceMM");
            float BasicWorkingDistanceMeter = BasicWorkingDistanceMM / 1000.0f;
            updateFocusDistance(BasicWorkingDistanceMeter);
        }

        /// <summary>
        /// If no value or NAN is passed the focus distance will not be updated, but the focus plane object position will be updated
        /// </summary>
        /// <param name="newFocusDistance"></param>
        public void updateFocusDistance(float newFocusDistance = float.NaN)
        {
            if (!float.IsNaN(newFocusDistance))
            {
                focusDistance = newFocusDistance;
            }
            // only run this code if not in editor mode
            if (Application.isPlaying)
            {
                // update focus plane distance
                if (focusPlaneObject != null)
                {
                    focusPlaneObject.transform.localPosition = new Vector3(0, 0, focusDistance);
                }
                if (cameraParent != null)
                {
                    cameraParent.transform.localPosition = new Vector3(
                        0,
                        0,
                        -focusDistWithDollyZoom
                    );
                }
            }
        }

        private void InitMainCamera()
        {
            if (mainCamera != null)
            {
                return;
            }
            mainCamera = GetComponent<Camera>();
            mainCamCullingMask = mainCamera.cullingMask;
            originalMainClearFlags = mainCamera.clearFlags;
        }

        private void loadMultiviewViewSeparationFromConfiguration(
            ConfigurationProvider configuration
        )
        {
            if (mode != G3DCameraMode.MULTIVIEW && mode != G3DCameraMode.HOLOBOX)
            {
                return;
            }

            int BasicWorkingDistanceMM = configuration.getInt("BasicWorkingDistanceMM");
            int NativeViewcount = configuration.getInt("NativeViewcount");
            float ApertureAngle = 14.0f;
            try
            {
                ApertureAngle = configuration.getFloat("ApertureAngle");
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }

            float BasicWorkingDistanceMeter = BasicWorkingDistanceMM / 1000.0f;
            float halfZoneOpeningAngleRad = ApertureAngle * Mathf.Deg2Rad / 2.0f;
            float halfWidthZoneAtbasicDistance =
                Mathf.Tan(halfZoneOpeningAngleRad) * BasicWorkingDistanceMeter;

            // calculate eye separation/ view separation
            viewSeparation = halfWidthZoneAtbasicDistance * 2 / NativeViewcount;
        }

        /// <summary>
        /// Initializes the cameras and their parents.
        /// if cameras are already initialized, this function only returns the focus plane.
        /// </summary>
        /// <returns>
        /// The focus plane game object.
        /// </returns>
        private void initCamerasAndParents()
        {
            if (focusPlaneObject == null)
            {
                focusPlaneObject = new GameObject("focus plane center");
                focusPlaneObject.transform.parent = transform;
                focusPlaneObject.transform.localPosition = new Vector3(0, 0, focusDistance);
                focusPlaneObject.transform.localRotation = Quaternion.identity;
            }

            //initialize cameras
            if (cameraParent == null)
            {
                cameraParent = new GameObject("g3dcams");
                cameraParent.transform.parent = focusPlaneObject.transform;
                cameraParent.transform.localPosition = new Vector3(0, 0, -focusDistance);
                cameraParent.transform.localRotation = Quaternion.identity;
            }

            if (cameras == null)
            {
                cameras = new List<Camera>();
                for (int i = 0; i < MAX_CAMERAS; i++)
                {
                    cameras.Add(new GameObject(CAMERA_NAME_PREFIX + i).AddComponent<Camera>());

                    copyCameraSettings(mainCamera, cameras[i]);

                    cameras[i].transform.SetParent(cameraParent.transform, true);
                    cameras[i].gameObject.SetActive(false);
                    cameras[i].transform.localRotation = Quaternion.identity;
                }
            }
        }

        private int getCameraCountFromConfigurationFile()
        {
            if (configurationFile == null)
            {
                Debug.LogError(
                    "No configuration file set. Please set a configuration file. Using default values."
                );
                return 2;
            }

            // TODO do not recreate the configuration provider every time
            // This gets called every frame in UpdateCameraCountBasedOnMode
            ConfigurationProvider configuration = ConfigurationProvider.getFromString(
                configurationFile.text
            );
            return getCameraCountFromConfigurationFile(configuration);
        }

        private int getCameraCountFromConfigurationFile(ConfigurationProvider configuration)
        {
            int NativeViewcount = configuration.getInt("NativeViewcount");
            return NativeViewcount;
        }

        private Vector2Int getDisplayResolutionFromConfigurationFile()
        {
            if (configurationFile == null)
            {
                Debug.LogError(
                    "No configuration file set. Please set a configuration file. Using default values."
                );
                return new Vector2Int(1920, 1080);
            }

            ConfigurationProvider configuration = ConfigurationProvider.getFromString(
                configurationFile.text
            );
            int HorizontalResolution = configuration.getInt("HorizontalResolution");
            int VerticalResolution = configuration.getInt("VerticalResolution");
            return new Vector2Int(HorizontalResolution, VerticalResolution);
        }

        private void reinitializeShader()
        {
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                material = new Material(Shader.Find("G3D/Autostereo"));
            }
            else // Multiview and Holobox mode
            {
                material = new Material(Shader.Find("G3D/AutostereoMultiview"));
            }
        }
        #endregion

        #region Updates
        void Update()
        {
            bool windowMovedLastFrame = windowMoved();
            bool windowResizedLastFrame = windowResized();

            if (mainCamera.enabled == false)
            {
                // enable all cameras if main camera is disabled
                for (int i = 0; i < internalCameraCount; i++)
                {
                    cameras[i].gameObject.SetActive(false);
                }
                mainCamInactiveLastFrame = true;
                return;
            }

            bool recreatedRenderTextures = false;

            if (mainCamInactiveLastFrame)
            {
                // recreate shader render textures when main camera was inactive last frame
                recreatedRenderTextures = true;
            }

            mainCamInactiveLastFrame = false;

            // update the shader parameters (only in headtracking mode)
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                headtrackingConnection.calculateShaderParameters();
            }

            bool cameraCountChanged = updateCameraCountBasedOnMode();
            updateCameras();
            updateShaderParameters();

            if (windowResizedLastFrame || windowMovedLastFrame)
            {
                updateScreenViewportProperties();
            }

            if (windowResizedLastFrame)
            {
                recreatedRenderTextures = true;
            }

            if (cameraCountChanged || oldRenderResolutionScale != renderResolutionScale)
            {
                oldRenderResolutionScale = renderResolutionScale;
                recreatedRenderTextures = true;
            }

            if (recreatedRenderTextures)
            {
                updateRenderTextures();
            }
        }

        private void updateFocusPlane()
        {
            if (focusPlaneObject != null)
            {
                focusPlaneObject.transform.localPosition = new Vector3(0, 0, focusDistance);
            }
        }

        private void updateScreenViewportProperties()
        {
            Vector2Int displayResolution = getDisplayResolutionFromConfigurationFile();
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                headtrackingConnection.updateScreenViewportProperties(displayResolution);
            }
            else // Multiview and Holobox mode
            {
                shaderParameters.screenWidth = displayResolution.x;
                shaderParameters.screenHeight = displayResolution.y;
                shaderParameters.leftViewportPosition = Screen.mainWindowPosition.x;
                shaderParameters.bottomViewportPosition =
                    Screen.mainWindowPosition.y + Screen.height;
            }

            // this parameter is used in the shader to invert the y axis
            material?.SetInt(Shader.PropertyToID("viewportHeight"), Screen.height);
            material?.SetInt(Shader.PropertyToID("viewportWidth"), Screen.width);
        }

        private void updateShaderParameters()
        {
            lock (shaderLock)
            {
                material?.SetInt(
                    shaderHandles.leftViewportPosition,
                    shaderParameters.leftViewportPosition
                );
                material?.SetInt(
                    shaderHandles.bottomViewportPosition,
                    shaderParameters.bottomViewportPosition
                );
                material?.SetInt(shaderHandles.screenWidth, shaderParameters.screenWidth);
                material?.SetInt(shaderHandles.screenHeight, shaderParameters.screenHeight);
                material?.SetInt(shaderHandles.nativeViewCount, shaderParameters.nativeViewCount);
                material?.SetInt(
                    shaderHandles.angleRatioNumerator,
                    shaderParameters.angleRatioNumerator
                );
                material?.SetInt(
                    shaderHandles.angleRatioDenominator,
                    shaderParameters.angleRatioDenominator
                );
                material?.SetInt(
                    shaderHandles.leftLensOrientation,
                    shaderParameters.leftLensOrientation
                );
                material?.SetInt(shaderHandles.mstart, shaderParameters.mstart);

                // test frame and stripe
                material?.SetInt(shaderHandles.showTestFrame, showTestFrame ? 1 : 0);
                material?.SetInt(shaderHandles.showTestStripe, shaderParameters.showTestStripe);

                material?.SetInt(shaderHandles.testGapWidth, shaderParameters.testGapWidth);
                material?.SetInt(shaderHandles.track, shaderParameters.track);
                material?.SetInt(shaderHandles.hqViewCount, shaderParameters.hqViewCount);
                material?.SetInt(shaderHandles.hviews1, shaderParameters.hviews1);
                material?.SetInt(shaderHandles.hviews2, shaderParameters.hviews2);
                material?.SetInt(shaderHandles.blur, shaderParameters.blur);
                material?.SetInt(shaderHandles.blackBorder, shaderParameters.blackBorder);
                material?.SetInt(shaderHandles.blackSpace, shaderParameters.blackSpace);
                material?.SetInt(shaderHandles.bls, shaderParameters.bls);
                material?.SetInt(shaderHandles.ble, shaderParameters.ble);
                material?.SetInt(shaderHandles.brs, shaderParameters.brs);
                material?.SetInt(shaderHandles.bre, shaderParameters.bre);
                material?.SetInt(shaderHandles.zCorrectionValue, shaderParameters.zCorrectionValue);
                material?.SetInt(
                    shaderHandles.zCompensationValue,
                    shaderParameters.zCompensationValue
                );
                material?.SetInt(shaderHandles.BGRPixelLayout, shaderParameters.BGRPixelLayout);

                material?.SetInt(Shader.PropertyToID("cameraCount"), internalCameraCount);

                material?.SetInt(Shader.PropertyToID("mirror"), mirrorViews ? 1 : 0);

                if (mode == G3DCameraMode.HEADTRACKING)
                {
                    material.SetInt(
                        Shader.PropertyToID("invertViews"),
                        invertViewsInHeadtracking ? 1 : 0
                    );
                }
                else // Multiview and Holobox mode
                {
                    material.SetInt(
                        Shader.PropertyToID("indexMapLength"),
                        indexMap.currentMap.Length
                    );
                    material.SetFloatArray(
                        Shader.PropertyToID("index_map"),
                        indexMap.getPaddedIndexMapArray()
                    );

                    material?.SetInt(Shader.PropertyToID("viewOffset"), viewOffset);
                }

                material?.SetInt(Shader.PropertyToID("mosaic_rows"), 4);
                material?.SetInt(Shader.PropertyToID("mosaic_columns"), 4);
            }
        }

        private void updateCameras()
        {
            Vector3 targetPosition = new Vector3(0, 0, -focusDistWithDollyZoom); // position for the camera center (base position from which all other cameras are offset)
            float targetViewSeparation = 0.0f;

            // calculate the camera center position and eye separation if head tracking and the headtracking effect are enabled
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                headtrackingConnection.handleHeadTrackingState(
                    ref targetPosition,
                    ref targetViewSeparation,
                    scaledViewSeparation,
                    focusDistWithDollyZoom
                );
            }
            else // Multiview and Holobox mode
            {
                targetViewSeparation = scaledViewSeparation;
            }

            cameraParent.transform.localPosition = targetPosition;

            float horizontalOffset = targetPosition.x;
            float verticalOffset = targetPosition.y;

            float currentFocusDistance = -cameraParent.transform.localPosition.z;

            // set the camera parent position to the focus distance
            cameraParent.transform.localPosition = new Vector3(
                horizontalOffset,
                verticalOffset,
                -currentFocusDistance
            );

            //calculate camera positions and matrices
            for (int i = 0; i < internalCameraCount; i++)
            {
                var camera = cameras[i];
                copyCameraSettings(mainCamera, camera, cameraParent.transform.localRotation);
                camera.fieldOfView = calcNewFOV(currentFocusDistance);

                float localCameraOffset = calculateCameraOffset(
                    i,
                    targetViewSeparation,
                    internalCameraCount
                );

                // apply new projection matrix
                camera.projectionMatrix = calculateCameraProjectionMatrix(
                    localCameraOffset + horizontalOffset,
                    verticalOffset,
                    focusDistWithDollyZoom,
                    camera.projectionMatrix
                );

                camera.transform.localPosition = new Vector3(localCameraOffset, 0, 0);

                // enable all cameras
                camera.gameObject.SetActive(true);
            }

            //disable all the other cameras, we are not using
            for (int i = internalCameraCount; i < MAX_CAMERAS; i++)
            {
                cameras[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Sets the camera count to two if we are in headtracking mode. Sets it to the maximum amount of views the display is capable of if we are in multiview mode.
        /// </summary>
        /// <returns>true if camera count was changed.</returns>
        private bool updateCameraCountBasedOnMode()
        {
            int previousCameraCount = internalCameraCount;
            if (mode == G3DCameraMode.HEADTRACKING)
            {
                internalCameraCount = 2;
            }
            else // Multiview and Holobox mode
            {
                internalCameraCount = getCameraCountFromConfigurationFile();
                if (internalCameraCount > MAX_CAMERAS)
                {
                    internalCameraCount = MAX_CAMERAS;
                }
            }

            if (internalCameraCount != previousCameraCount)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// copies relevant camera settings from the source camera to the destination camera.
        /// Ensures that destination camera is setup correctly.
        /// </summary>
        /// <param name="source">The source camera from which to copy settings.</param>
        /// <param name="cameraParent">The parent transform of the destination camera.</param>
        /// <param name="destination">The destination camera to which settings will be copied.</param>
        private void copyCameraSettings(
            Camera source,
            Camera destination,
            Quaternion destLocalRotation = default(Quaternion)
        )
        {
            RenderTexture currentTargetTexture = destination.targetTexture;
            destination.CopyFrom(source);
            destination.targetTexture = currentTargetTexture; // ensure that the target texture is not overwritten
            destination.transform.localRotation = destLocalRotation;
            destination.cullingMask = mainCamCullingMask;
            destination.clearFlags = originalMainClearFlags;
            destination.depth = source.depth - 1; // make sure the main camera renders on top of the secondary cameras
#if G3D_HDRP
            HDAdditionalCameraData sourceHDData = source.GetComponent<HDAdditionalCameraData>();
            HDAdditionalCameraData destinationHDData =
                destination.GetComponent<HDAdditionalCameraData>();
            if (destinationHDData == null)
            {
                destinationHDData = destination.gameObject.AddComponent<HDAdditionalCameraData>();
            }
            if (sourceHDData != null)
            {
                sourceHDData.CopyTo(destinationHDData);
            }
#endif
#if G3D_URP
            UniversalAdditionalCameraData sourceURPData =
                source.GetComponent<UniversalAdditionalCameraData>();
            UniversalAdditionalCameraData destinationURPData =
                destination.GetComponent<UniversalAdditionalCameraData>();
            if (destinationURPData == null)
            {
                destinationURPData =
                    destination.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }

            if (sourceURPData != null)
            {
                Helpers.copyToCameraTarget(sourceURPData, destinationURPData);
            }

            destination.GetUniversalAdditionalCameraData().antialiasing = antialiasingMode;
#endif
        }

        /// <summary>
        /// adds rendertextres for the cameras to the shader and sets them as target textures for the cameras.
        /// </summary>
        /// <param name="renderTextures"></param>
        /// <param name="renderTextureIndex"></param>
        /// <param name="cameraIndex"></param>
        /// <param name="texNameInShader">only used if view generation is turned on. used to specify the texture name (left, right, middle)</param>
        private void addRenderTextureToCamera(
            RenderTexture[] renderTextures,
            int renderTextureIndex,
            int cameraIndex,
            string texNameInShader = "texture"
        )
        {
            int width = Screen.width;
            int height = Screen.height;

            width = (int)(width * (renderResolutionScale / 100f));
            height = (int)(height * (renderResolutionScale / 100f));

            renderTextures[renderTextureIndex] = new RenderTexture(width, height, 0)
            {
                format = RenderTextureFormat.ARGB32,
                depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm,
            };
            cameras[cameraIndex].targetTexture = renderTextures[renderTextureIndex];
            material.SetTexture(
                "texture" + renderTextureIndex,
                renderTextures[renderTextureIndex],
                RenderTextureSubElement.Color
            );
        }

        private bool windowResized()
        {
            var window_dim = new Vector2Int(Screen.width, Screen.height);
            if (cachedWindowSize != window_dim)
            {
                cachedWindowSize = window_dim;
                return true;
            }
            return false;
        }

        private bool windowMoved()
        {
            var window_pos = new Vector2Int(
                Screen.mainWindowPosition.x,
                Screen.mainWindowPosition.y
            );
            if (cachedWindowPosition != window_pos)
            {
                cachedWindowPosition = window_pos;
                return true;
            }
            return false;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localCameraOffset">Offset (along the x axis) of this camera compared to zero position.</param>
        /// <param name="horizontalOffset">general offset (x axis) of the "zero position" compared to start position due to head tracking.</param>
        /// <param name="verticalOffset">general offset (y axis) of the "zero position" compared to start position due to head tracking.</param>
        /// <param name="focusDistance">general offset (z axis) of the "zero position" compared to start position due to head tracking.</param>
        /// <param name="mainCamProjectionMatrix"></param>
        /// <returns></returns>
        private Matrix4x4 calculateCameraProjectionMatrix(
            float horizontalOffset,
            float verticalOffset,
            float focusDistance,
            Matrix4x4 mainCamProjectionMatrix
        )
        {
            // horizontal obliqueness
            float horizontalObl = -horizontalOffset / focusDistance;
            float vertObl = -verticalOffset / focusDistance;

            // focus distance is in view space. Writing directly into projection matrix would require focus distance to be in projection space
            Matrix4x4 shearMatrix = Matrix4x4.identity;
            shearMatrix[0, 2] = horizontalObl;
            shearMatrix[1, 2] = vertObl;
            // apply new projection matrix
            return mainCamProjectionMatrix * shearMatrix;
        }

        // This function only does something when you use the SRP render pipeline.
        // when using either URP or HRDP image combination is handled in the respective renderpasses.
        // URP -> G3D.RenderPipeline.URP.ScriptableRP.cs
        // HDRP -> G3D.RenderPipeline.HDRP.CustomPass.cs
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            // This is where the material and shader are applied to the camera image.
            //legacy support (no URP or HDRP)
#if G3D_HDRP || URP
#else
            if (material == null)
                Graphics.Blit(source, destination);
            else
                Graphics.Blit(source, destination, material);
#endif
        }
        #endregion

        #region Debugging
#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (!showGizmos)
            {
                return;
            }

            if (enabled == false)
            {
                // do not run this code if the script is not enabled
                return;
            }

            if (mainCamera == null)
            {
                InitMainCamera();
                if (mainCamera == null)
                {
                    Debug.LogError(
                        "No main camera found. Please add a camera to the G3DCamera object."
                    );
                    return;
                }
            }

            Vector3 basePosition = new Vector3(0, 0, focusDistance);
            float dollyDifference = focusDistance - focusDistWithDollyZoom;

            Vector3 position;
            Gizmos.color = new Color(0, 0, 1, 0.75F);
            Gizmos.matrix = transform.localToWorldMatrix;
            // draw camera position spheres
            for (int i = 0; i < internalCameraCount; i++)
            {
                float localCameraOffset = calculateCameraOffset(
                    i,
                    scaledViewSeparation,
                    internalCameraCount
                );
                Vector3 camPos = basePosition - new Vector3(0, 0, focusDistWithDollyZoom);
                camPos += new Vector3(1, 0, 0) * localCameraOffset;
                Gizmos.DrawSphere(camPos, 0.01f * gizmoSize);
            }

            // draw camera frustums
            Gizmos.color = new Color(0, 0, 1, 1); // set color to one wo improve visibility
            for (int i = 0; i < internalCameraCount; i++)
            {
                float localCameraOffset = calculateCameraOffset(
                    i,
                    scaledViewSeparation,
                    internalCameraCount
                );

                position = transform.position + transform.right * localCameraOffset;
                // apply dolly zoom to position
                position += transform.forward * dollyDifference;

                // apply new projection matrix
                Matrix4x4 localProjectionMatrix = Matrix4x4.TRS(
                    position,
                    transform.rotation,
                    Vector3.one
                );
                Matrix4x4 projMatrix = calculateCameraProjectionMatrix(
                    localCameraOffset,
                    0,
                    focusDistWithDollyZoom,
                    localProjectionMatrix
                );

                Gizmos.matrix = projMatrix;

                Gizmos.DrawFrustum(
                    Vector3.zero,
                    calcNewFOV(),
                    mainCamera.farClipPlane,
                    mainCamera.nearClipPlane,
                    mainCamera.aspect
                );
            }

            Gizmos.matrix = transform.localToWorldMatrix;
            drawFocusPlane(mainCamera.fieldOfView, focusDistance);
        }

        private void drawFocusPlane(float FOV, float focusDist)
        {
            Gizmos.color = new Color(0, 0, 1, 0.25F);
            Vector3 position = new Vector3(0, 0, focusDist);
            float frustumWidth = Mathf.Tan(FOV * Mathf.Deg2Rad / 2) * focusDist * 2;
            float frustumHeight =
                Mathf.Tan(
                    Camera.VerticalToHorizontalFieldOfView(FOV, mainCamera.aspect)
                        * Mathf.Deg2Rad
                        / 2
                )
                * focusDist
                * 2;
            Gizmos.DrawCube(position, new Vector3(frustumHeight, frustumWidth, 0.001f));
        }

        private float calcNewFOV()
        {
            return calcNewFOV(focusDistWithDollyZoom);
        }
#endif

        private float calcNewFOV(float actualFocusDistance)
        {
            float halfFOVRad = mainCamera.fieldOfView * Mathf.Deg2Rad / 2;
            float a = Mathf.Tan(halfFOVRad) * focusDistance;

            float newHalfFOVRad = Mathf.Atan(a / actualFocusDistance);
            return newHalfFOVRad * Mathf.Rad2Deg * 2;
        }
        #endregion

        private float calculateCameraOffset(
            int currentCamera,
            float targetEyeSeparation,
            int tmpCameraCount
        )
        {
            int currentView = -tmpCameraCount / 2 + currentCamera;
            if (tmpCameraCount % 2 == 0 && currentView >= 0)
            {
                currentView += 1;
            }

            // it is used to scale the offset to the ends for a mosaic texture where the middle textures are missing
            float offset = currentView * targetEyeSeparation;

            // when the camera count is even, one camera is placed half the eye separation to the right of the center
            // same for the other to the left
            // therefore we need to add the correction term to the offset to get the correct position
            if (tmpCameraCount % 2 == 0)
            {
                // subtract half of the eye separation to get the correct offset
                float correctionTerm = targetEyeSeparation / 2;
                if (currentView > 0)
                {
                    correctionTerm *= -1;
                }
                offset = offset + correctionTerm;
            }

            int flip = mirrorViews ? 1 : -1;

            return offset * flip;
        }
    }
}
