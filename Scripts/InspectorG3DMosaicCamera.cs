#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace G3D
{
    [CustomEditor(typeof(G3DCameraMosaicMultiview))]
    public class InspectorG3DMosaicCamera : UnityEditor.Editor
    {
        public VisualTreeAsset inspectorXML;

        private PropertyField modeField;
        private PropertyField dimensionsFromFilename;
        private PropertyField dataTypeField;
        private PropertyField configurationFileField;
        private PropertyField indexMapYoyoStartField;
        private PropertyField invertIndexMapField;
        private PropertyField invertIndexMapIndicesField;

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

            // Find the PropertyField in the Inspector XML.
            modeField = mainInspector.Q<PropertyField>("mode");
            dataTypeField = mainInspector.Q<PropertyField>("dataType");
            renderTexture = mainInspector.Q<PropertyField>("renderTexture");
            image = mainInspector.Q<PropertyField>("image");
            videoClip = mainInspector.Q<PropertyField>("videoClip");

            dataTypeField.RegisterValueChangeCallback(
                (evt) =>
                {
                    DataType newMode = (DataType)evt.changedProperty.enumValueIndex;
                    switch (newMode)
                    {
                        case DataType.Image:
                            renderTexture.style.display = DisplayStyle.None;
                            image.style.display = DisplayStyle.Flex;
                            videoClip.style.display = DisplayStyle.None;
                            dimensionsFromFilename.SetEnabled(true);
                            break;
                        case DataType.RenderTexture:
                            renderTexture.style.display = DisplayStyle.Flex;
                            image.style.display = DisplayStyle.None;
                            videoClip.style.display = DisplayStyle.None;
                            dimensionsFromFilename.SetEnabled(false);
                            break;
                        case DataType.Video:
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

            configurationFileField = mainInspector.Q<PropertyField>("configurationFile");
            indexMapYoyoStartField = mainInspector.Q<PropertyField>("indexMapYoyoStart");
            invertIndexMapField = mainInspector.Q<PropertyField>("invertIndexMap");
            invertIndexMapIndicesField = mainInspector.Q<PropertyField>("invertIndexMapIndices");
            setupValueChangeInteractions();

            IndexMap = mainInspector.Q<Label>("IndexMap");
            updateIndexMapDisplay();

            Button visitOnlineDocumentationButton = mainInspector.Q<Button>(
                "visitOnlineDocumentation"
            );
            visitOnlineDocumentationButton.clicked += () =>
            {
                Application.OpenURL("https://3d-global-docs.vercel.app/docs/category/unity");
            };

            return mainInspector;
        }

        private void setupValueChangeInteractions()
        {
            G3DCameraMosaicMultiview camera = (G3DCameraMosaicMultiview)target;
            configurationFileField.RegisterValueChangeCallback(
                (evt) =>
                {
                    camera.updateShaderFromConfigurationFile();
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
        }

        private void updateIndexMapDisplay()
        {
            G3DCameraMosaicMultiview camera = (G3DCameraMosaicMultiview)target;
            IndexMap.text = camera.indexMapToString();
        }
    }
}
#endif
