#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[CustomEditor(typeof(G3DCameraMosaicMultiview))]
public class InspectorG3DMosaicCamera1 : Editor
{
    public VisualTreeAsset inspectorXML;

    private PropertyField dimensionsFromFilename;
    private PropertyField modeField;

    private PropertyField renderTexture;
    private PropertyField image;
    private PropertyField videoClip;

    private static bool isAdvancedSettingsVisible = false;
    private Foldout advancedSettingsFoldout;

    public override VisualElement CreateInspectorGUI()
    {
        G3DCameraMosaicMultiview camera = (G3DCameraMosaicMultiview)target;

        // Create a new VisualElement to be the root of our Inspector UI.
        VisualElement mainInspector = new VisualElement();

        // Load the UXML file.
        inspectorXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
            "Packages/com.3dglobal.core/Resources/G3DMosaicCameraInspector.uxml"
        );

        // Instantiate the UXML.
        mainInspector = inspectorXML.Instantiate();

        dimensionsFromFilename = mainInspector.Q<PropertyField>("dimensionsFromFilename");

        modeField = mainInspector.Q<PropertyField>("mosaicMode");
        renderTexture = mainInspector.Q<PropertyField>("renderTexture");
        image = mainInspector.Q<PropertyField>("image");
        videoClip = mainInspector.Q<PropertyField>("videoClip");

        // Find the PropertyField in the Inspector XML.
        modeField = mainInspector.Q<PropertyField>("mosaicMode");
        modeField.RegisterValueChangeCallback(
            (evt) =>
            {
                MosaicMode newMode = (MosaicMode)evt.changedProperty.enumValueIndex;
                switch (newMode)
                {
                    case MosaicMode.Image:
                        renderTexture.style.display = DisplayStyle.None;
                        image.style.display = DisplayStyle.Flex;
                        videoClip.style.display = DisplayStyle.None;

                        dimensionsFromFilename.SetEnabled(true);
                        break;
                    case MosaicMode.RenderTexture:
                        renderTexture.style.display = DisplayStyle.Flex;
                        image.style.display = DisplayStyle.None;
                        videoClip.style.display = DisplayStyle.None;
                        dimensionsFromFilename.SetEnabled(false);
                        break;
                    case MosaicMode.Video:
                        renderTexture.style.display = DisplayStyle.None;
                        image.style.display = DisplayStyle.None;
                        videoClip.style.display = DisplayStyle.Flex;
                        dimensionsFromFilename.SetEnabled(true);
                        break;
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

        // setup UI

        return mainInspector;
    }
}
#endif
