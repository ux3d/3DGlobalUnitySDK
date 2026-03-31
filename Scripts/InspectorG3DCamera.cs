#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace G3D
{
    [CustomEditor(typeof(G3DCamera))]
    public class InspectorG3DCamera : Editor
    {
        public VisualTreeAsset inspectorXML;

        private PropertyField modeField;
        private PropertyField configurationFileField;
        private PropertyField indexMapYoyoStartField;
        private PropertyField invertIndexMapField;
        private PropertyField invertIndexMapIndicesField;

        private PropertyField headtrackingScaleField;

        private PropertyField viewOffsetField;
        private PropertyField focusDistanceField;
        private PropertyField dollyZoomField;
        private Button setCameraFOVButton;

        private Label calibFolderLabel;
        private Label HeadtrackingCalibFileInfo;

        private static bool isAdvancedSettingsVisible = false;
        private Foldout advancedSettingsFoldout;

        private Label IndexMap;

        /// <summary>
        /// This is used to hack the Unity Inspector.
        /// Unity triggers a change event when the Inspector is first displayed, but it doesn't provide the new value in that event, so we have to store the last config file to detect when it actually changes.
        /// </summary>
        private TextAsset lastConfigFile;

        public override VisualElement CreateInspectorGUI()
        {
            G3DCamera camera = (G3DCamera)target;
            // Create a new VisualElement to be the root of our Inspector UI.
            VisualElement mainInspector = new VisualElement();
            // Load the UXML file.
            inspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.3dglobal.core/Resources/G3DCameraInspector.uxml"
            );

            // Add a simple label.
            mainInspector.Add(new Label("This is a custom Inspector"));

            // Instantiate the UXML.
            mainInspector = inspectorXML.Instantiate();

            // Find the PropertyField in the Inspector XML.
            modeField = mainInspector.Q<PropertyField>("mode");

            advancedSettingsFoldout = mainInspector.Q<Foldout>("AdvancedSettings");
            advancedSettingsFoldout.value = isAdvancedSettingsVisible;

            IndexMap = mainInspector.Q<Label>("IndexMap");
            updateIndexMap();

            headtrackingScaleField = mainInspector.Q<PropertyField>("headTrackingSensitivity");

            viewOffsetField = mainInspector.Q<PropertyField>("viewOffset");

            focusDistanceField = mainInspector.Q<PropertyField>("focusDistance");
            dollyZoomField = mainInspector.Q<PropertyField>("dollyZoom");

            string calibrationPath = System.Environment.GetFolderPath(
                Environment.SpecialFolder.CommonDocuments
            );
            calibrationPath = System.IO.Path.Combine(calibrationPath, "3D Global", "calibrations");
            calibFolderLabel = mainInspector.Q<Label>("HeadtrackingCalibrationFolder");
            calibFolderLabel.text = calibrationPath;

            HeadtrackingCalibFileInfo = mainInspector.Q<Label>("HeadtrackingCalibFileInfo");

            configurationFileField = mainInspector.Q<PropertyField>("configurationFile");
            lastConfigFile = camera.configurationFile != null ? camera.configurationFile : null; // store the initial config file to detect changes later
            indexMapYoyoStartField = mainInspector.Q<PropertyField>("indexMapYoyoStart");
            invertIndexMapField = mainInspector.Q<PropertyField>("invertIndexMap");
            invertIndexMapIndicesField = mainInspector.Q<PropertyField>("invertIndexMapIndices");
            setupValueChangeInteractions();

            setCameraFOVButton = mainInspector.Q<Button>("setCameraFOV");
            setCameraFOVButton.tooltip =
                "Set the camera FOV to the natural field of view the display actually covers in your field of vision if you sit at the recommended distance.";
            setCameraFOVButton.clicked += () =>
            {
                camera.setCameraFOVToDisplayFOV();
            };

            Button setFocusDistanceButton = mainInspector.Q<Button>("setFocusDistance");
            setFocusDistanceButton.tooltip =
                "Set the focus distance to the native focus distance of the dispaly. Native focus distance is the distance the viewer has to be from the display for the 3d effect to look best.";
            setFocusDistanceButton.clicked += () =>
            {
                camera.setFocusDistanceToDisplay();
            };

            return mainInspector;
        }

        void Update()
        {
            G3DCamera camera = (G3DCamera)target;
            Debug.Log("Updating camera inspector, current mode: " + camera.mode);
        }

        private void setupValueChangeInteractions()
        {
            modeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    G3DCameraMode newMode = (G3DCameraMode)evt.changedProperty.enumValueIndex;
                    if (newMode == G3DCameraMode.HEADTRACKING)
                    {
                        calibFolderLabel.style.display = DisplayStyle.Flex;
                        headtrackingScaleField.style.display = DisplayStyle.Flex;
                        HeadtrackingCalibFileInfo.style.display = DisplayStyle.Flex;
                        viewOffsetField.style.display = DisplayStyle.None;
                    }
                    else // Holobox and Multiview mode
                    {
                        calibFolderLabel.style.display = DisplayStyle.None;
                        headtrackingScaleField.style.display = DisplayStyle.None;
                        HeadtrackingCalibFileInfo.style.display = DisplayStyle.None;
                        viewOffsetField.style.display = DisplayStyle.Flex;
                    }
                }
            );

            advancedSettingsFoldout.RegisterValueChangedCallback(
                (evt) =>
                {
                    isAdvancedSettingsVisible = evt.newValue;
                }
            );

            G3DCamera camera = (G3DCamera)target;
            configurationFileField.RegisterValueChangeCallback(
                (evt) =>
                {
                    if (camera.configurationFile != lastConfigFile)
                    {
                        lastConfigFile = camera.configurationFile;
                        camera.setupCameras();
                        updateIndexMap();

                        // force update the scene view to dorce gizmo update
                        SceneView.RepaintAll();
                    }
                }
            );
            modeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateMode();
                    updateIndexMap();
                }
            );

            indexMapYoyoStartField.RegisterValueChangeCallback(
                (evt) =>
                {
                    updateIndexMap();
                }
            );
            invertIndexMapField.RegisterValueChangeCallback(
                (evt) =>
                {
                    updateIndexMap();
                }
            );
            invertIndexMapIndicesField.RegisterValueChangeCallback(
                (evt) =>
                {
                    updateIndexMap();
                }
            );

            focusDistanceField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateFocusDistance();
                }
            );

            dollyZoomField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateFocusDistance();
                }
            );
        }

        private void updateIndexMap()
        {
            G3DCamera camera = (G3DCamera)target;
            camera.updateIndexMap();
            IndexMap.text = camera.indexMapToString();
        }
    }
}
#endif
