#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace G3D
{
    [CustomEditor(typeof(G3DCamera))]
    public class InspectorG3DCamera : UnityEditor.Editor
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

        private DropdownField predictionModelDropdown;

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

        // We use this to check if the headtracking connection is active
        // in order to display certain UI elements in the inspector
        private HeadtrackingConnection headtrackingConnection;

        public override VisualElement CreateInspectorGUI()
        {
            G3DCamera camera = (G3DCamera)target;
            // Create a new VisualElement to be the root of our Inspector UI.
            VisualElement mainInspector = new VisualElement();
            // Load the UXML file.
            inspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.3dglobal.core/Resources/G3DCameraInspector.uxml"
            );

            // Instantiate the UXML.
            mainInspector = inspectorXML.Instantiate();

            // Find the PropertyField in the Inspector XML.
            modeField = mainInspector.Q<PropertyField>("mode");

            advancedSettingsFoldout = mainInspector.Q<Foldout>("AdvancedSettings");
            advancedSettingsFoldout.value = isAdvancedSettingsVisible;

            IndexMap = mainInspector.Q<Label>("IndexMap");
            updateIndexMap();

            headtrackingScaleField = mainInspector.Q<PropertyField>("headtrackingSensitivity");

            viewOffsetField = mainInspector.Q<PropertyField>("viewOffset");

            focusDistanceField = mainInspector.Q<PropertyField>("focusDistance");
            dollyZoomField = mainInspector.Q<PropertyField>("dollyZoom");

            // MSAA only affects the Built-in Render Pipeline; SRP-based projects control MSAA via
            // the pipeline asset. Hide the field (and its backing logic in G3DCamera is compiled out)
            // when URP or HDRP is present so it can't be set to a value that has no effect.
#if G3D_URP || G3D_HDRP
            PropertyField msaaSampleCountField = mainInspector.Q<PropertyField>("msaaSampleCount");
            if (msaaSampleCountField != null)
                msaaSampleCountField.style.display = DisplayStyle.None;
#endif

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

            Button setFocusDistanceButton = mainInspector.Q<Button>("setFocusDistance");
            setFocusDistanceButton.tooltip =
                "Set the focus distance to the native focus distance of the display. At native focus distance the camera to focus plane distance is the same as the ideal real world viewing distance of the display.";
            setFocusDistanceButton.clicked += () =>
            {
                camera.setFocusDistanceToDisplay();
            };

            Button setCameraFOVButton = mainInspector.Q<Button>("setCameraFOV");
            setCameraFOVButton.tooltip =
                "Set the main camera's field of view to the real world angle the display actually covers in the user's vision when viewed from the ideal distance.";
            setCameraFOVButton.clicked += () =>
            {
                camera.setCameraFOVToDisplayFOV();
            };

            Button visitOnlineDocumentationButton = mainInspector.Q<Button>(
                "visitOnlineDocumentation"
            );
            visitOnlineDocumentationButton.clicked += () =>
            {
                Application.OpenURL("https://3d-global-docs.vercel.app/docs/category/unity");
            };

            predictionModelDropdown = mainInspector.Q<DropdownField>("predictionModel");
            predictionModelDropdown.choices = HeadTrackingSDK.ht_get_available_predictions();
            if (predictionModelDropdown.choices.Count == 0)
            {
                predictionModelDropdown.value = "No prediction models available";
                predictionModelDropdown.SetEnabled(false);
            }
            else
            {
                predictionModelDropdown.value = "kalman_9dv2";
            }
            HeadTrackingSDK.ht_set_prediction(predictionModelDropdown.value);
            predictionModelDropdown.RegisterValueChangedCallback(
                (evt) =>
                {
                    Debug.Log($"Selected prediction model: {evt.newValue}");
                    HeadTrackingSDK.ht_set_prediction(evt.newValue);
                }
            );

            return mainInspector;
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
                    }
                    else // Holobox and Multiview mode
                    {
                        calibFolderLabel.style.display = DisplayStyle.None;
                        headtrackingScaleField.style.display = DisplayStyle.None;
                        HeadtrackingCalibFileInfo.style.display = DisplayStyle.None;
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
