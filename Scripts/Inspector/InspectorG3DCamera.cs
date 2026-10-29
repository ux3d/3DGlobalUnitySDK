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
        private PropertyField configCodeField;
        private PropertyField indexMapYoyoStartField;
        private PropertyField invertIndexMapField;
        private PropertyField invertIndexMapIndicesField;

        private PropertyField headtrackingScaleField;

        private PropertyField focusDistanceField;
        private PropertyField dollyZoomField;

        private DropdownField predictionModelDropdown;

        private Label calibFolderLabel;
        private Label HeadtrackingCalibFileInfo;

        private static bool isAdvancedSettingsVisible = false;
        private Foldout advancedSettingsFoldout;

        private Label IndexMap;

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

            configCodeField = mainInspector.Q<PropertyField>("configCode");

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

            indexMapYoyoStartField = mainInspector.Q<PropertyField>("indexMapYoyoStart");
            invertIndexMapField = mainInspector.Q<PropertyField>("invertIndexMap");
            invertIndexMapIndicesField = mainInspector.Q<PropertyField>("invertIndexMapIndices");
            setupValueChangeInteractions();

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
                    // update UI
                    G3DCameraMode newMode = Misc.CameraModeFromString((string) evt.changedProperty.boxedValue.ToString());
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

                    // update camera mode in the backend
                    ((G3DCamera)target).updateMode();
                    updateIndexMap();
                }
            );

            advancedSettingsFoldout.RegisterValueChangedCallback(
                (evt) =>
                {
                    isAdvancedSettingsVisible = evt.newValue;
                }
            );

            G3DCamera camera = (G3DCamera)target;
            configCodeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.setupCameras();
                    updateIndexMap();

                    // force update the scene view to force gizmo update
                    SceneView.RepaintAll();
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
