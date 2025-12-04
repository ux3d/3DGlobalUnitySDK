#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(G3DCamera))]
public class InspectorG3DCamera : Editor
{
    public VisualTreeAsset inspectorXML;

    private PropertyField modeField;

    private PropertyField calibrationFileField;
    private PropertyField headtrackingScaleField;

    private PropertyField viewOffsetField;

    private Label calibFolderLabel;
    private Label DioramaCalibFileInfo;

    private static bool isAdvancedSettingsVisible = false;
    private Foldout advancedSettingsFoldout;

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

        // setup UI

        return mainInspector;
    }
}
#endif
