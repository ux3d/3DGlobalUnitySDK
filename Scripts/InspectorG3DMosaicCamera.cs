#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace G3D
{
    [CustomEditor(typeof(G3DCameraMosaicMultiview))]
    public class InspectorG3DMosaicCamera : Editor
    {
        public VisualTreeAsset inspectorXML;

        private PropertyField dimensionsFromFilename;
        private PropertyField modeField;
        private PropertyField configCodeField;
        private PropertyField indexMapYoyoStartField;
        private PropertyField invertIndexMapField;
        private PropertyField invertIndexMapIndicesField;
        private PropertyField useHQViewsField;

        private PropertyField renderTexture;
        private PropertyField image;
        private PropertyField videoClip;

        private static bool isAdvancedSettingsVisible = false;
        private Foldout advancedSettingsFoldout;

        private Label IndexMap;

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
            configCodeField = mainInspector.Q<PropertyField>("configCode");
            indexMapYoyoStartField = mainInspector.Q<PropertyField>("indexMapYoyoStart");
            invertIndexMapField = mainInspector.Q<PropertyField>("invertIndexMap");
            invertIndexMapIndicesField = mainInspector.Q<PropertyField>("invertIndexMapIndices");
            useHQViewsField = mainInspector.Q<PropertyField>("useHQViews");
            setupValueChangeInteractions();

            IndexMap = mainInspector.Q<Label>("IndexMap");
            updateIndexMapDisplay();

            return mainInspector;
        }

        private void setupValueChangeInteractions()
        {
            G3DCameraMosaicMultiview camera = (G3DCameraMosaicMultiview)target;
            configCodeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    string newConfigCode = evt.changedProperty.stringValue;
                    camera.updateConfigCode(newConfigCode);
                }
            );
            indexMapYoyoStartField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                    updateIndexMapDisplay();
                }
            );
            invertIndexMapField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                    updateIndexMapDisplay();
                }
            );
            invertIndexMapIndicesField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                    updateIndexMapDisplay();
                }
            );
            useHQViewsField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateIndexMap();
                    updateIndexMapDisplay();
                }
            );
        }

        private void updateIndexMapDisplay()
        {
            G3DCameraMosaicMultiview camera = (G3DCameraMosaicMultiview)target;
            IndexMap.text = camera.indexMapToString();
        }
    }
}
#endif
