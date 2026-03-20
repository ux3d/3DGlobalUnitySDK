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
        private PropertyField calibrationFileField;
        private PropertyField sceneScaleFactorField;
        private PropertyField indexMapYoyoStartField;
        private PropertyField invertIndexMapField;
        private PropertyField invertIndexMapIndicesField;
        private PropertyField generateViewsField;

        private PropertyField headtrackingScaleField;

        private PropertyField viewOffsetField;

        private Label calibFolderLabel;
        private Label DioramaCalibFileInfo;

        private static bool isAdvancedSettingsVisible = false;
        private Foldout advancedSettingsFoldout;
        private VisualElement viewGenerationContainer;

        public override VisualElement CreateInspectorGUI()
        {
            G3DCamera camera = (G3DCamera)target;

            // Create a new VisualElement to be the root of our Inspector UI.
            VisualElement mainInspector = new VisualElement();

            // Add a simple label.
            mainInspector.Add(new Label("This is a custom Inspector"));

            // Load the UXML file.
            inspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Packages/com.3dglobal.core/Resources/G3DCameraInspector.uxml"
            );

            // Instantiate the UXML.
            mainInspector = inspectorXML.Instantiate();

            // Find the PropertyField in the Inspector XML.
            modeField = mainInspector.Q<PropertyField>("mode");
            modeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    G3DCameraMode newMode = (G3DCameraMode)evt.changedProperty.enumValueIndex;
                    if (newMode == G3DCameraMode.DIORAMA)
                    {
                        calibFolderLabel.style.display = DisplayStyle.Flex;
                        headtrackingScaleField.style.display = DisplayStyle.Flex;
                        DioramaCalibFileInfo.style.display = DisplayStyle.Flex;
                        viewOffsetField.style.display = DisplayStyle.None;
                    }
                    else
                    {
                        calibFolderLabel.style.display = DisplayStyle.None;
                        headtrackingScaleField.style.display = DisplayStyle.None;
                        DioramaCalibFileInfo.style.display = DisplayStyle.None;
                        viewOffsetField.style.display = DisplayStyle.Flex;
                    }
                }
            );

            advancedSettingsFoldout = mainInspector.Q<Foldout>("AdvancedSettings");
            advancedSettingsFoldout.value = isAdvancedSettingsVisible;
            advancedSettingsFoldout.RegisterValueChangedCallback(
                (evt) =>
                {
                    isAdvancedSettingsVisible = evt.newValue;
                }
            );

            headtrackingScaleField = mainInspector.Q<PropertyField>("headTrackingScale");

            viewOffsetField = mainInspector.Q<PropertyField>("viewOffset");

            string calibrationPath = System.Environment.GetFolderPath(
                Environment.SpecialFolder.CommonDocuments
            );
            calibrationPath = System.IO.Path.Combine(calibrationPath, "3D Global", "calibrations");
            calibFolderLabel = mainInspector.Q<Label>("DioramaCalibrationFolder");
            calibFolderLabel.text =
                "The headtracking library will search for display calibrations in this folder:\n"
                + calibrationPath;

            DioramaCalibFileInfo = mainInspector.Q<Label>("DioramaCalibFileInfo");

            viewGenerationContainer = mainInspector.Q<VisualElement>("viewGenerationContainer");
            generateViewsField = mainInspector.Q<PropertyField>("generateViews");
            generateViewsField.RegisterValueChangeCallback(
                (evt) =>
                {
                    bool newMode = evt.changedProperty.boolValue;
                    setViewgenerationDisplay(newMode);
                }
            );

#if G3D_URP
            // hide in URP
            viewGenerationContainer.style.display = DisplayStyle.None;
#elif G3D_HDRP
            // setup UI
            setViewgenerationDisplay((target as G3DCamera).generateViews);
#endif

            calibrationFileField = mainInspector.Q<PropertyField>("calibrationFile");
            sceneScaleFactorField = mainInspector.Q<PropertyField>("sceneScaleFactor");
            indexMapYoyoStartField = mainInspector.Q<PropertyField>("indexMapYoyoStart");
            invertIndexMapField = mainInspector.Q<PropertyField>("invertIndexMap");
            invertIndexMapIndicesField = mainInspector.Q<PropertyField>("invertIndexMapIndices");
            setupValueChangeInteractions();

            return mainInspector;
        }

        private void setupValueChangeInteractions()
        {
            G3DCamera camera = (G3DCamera)target;
            calibrationFileField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.setupCameras();
                }
            );
            sceneScaleFactorField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.setupCameras();
                    if (camera.headtrackingConnection != null)
                    {
                        camera.headtrackingConnection.sceneScaleFactor =
                            evt.changedProperty.floatValue;
                    }
                }
            );
            modeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateMode();
                }
            );

            indexMapYoyoStartField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                }
            );
            invertIndexMapField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                }
            );
            invertIndexMapIndicesField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                }
            );
        }

        private void setViewgenerationDisplay(bool enabled)
        {
            if (enabled)
            {
                viewGenerationContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                viewGenerationContainer.style.display = DisplayStyle.None;
            }
        }
    }
}
#endif
