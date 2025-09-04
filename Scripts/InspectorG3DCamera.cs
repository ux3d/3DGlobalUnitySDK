#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(G3DCamera))]
public class InspectorG3DCamera : Editor
{
    public VisualTreeAsset inspectorXML;

    private PropertyField modeField;
    private PropertyField generateViewsField;

    private VisualElement dioramaInspector;
    private VisualElement multiviewInspector;

    private VisualElement viewGenerationContainer;

    public override VisualElement CreateInspectorGUI()
    {
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
                setDisplayMode(newMode);
            }
        );

        dioramaInspector = mainInspector.Q<VisualElement>("Diorama");
        multiviewInspector = mainInspector.Q<VisualElement>("Multiview");

        viewGenerationContainer = mainInspector.Q<VisualElement>("viewGenerationContainer");
        generateViewsField = mainInspector.Q<PropertyField>("generateViews");
        generateViewsField.RegisterValueChangeCallback(
            (evt) =>
            {
                bool newMode = evt.changedProperty.boolValue;
                setViewgenerationDisplay(newMode);
            }
        );

        // setup UI
        setDisplayMode((target as G3DCamera).mode);
        setViewgenerationDisplay((target as G3DCamera).generateViews);

        return mainInspector;
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

    private void setDisplayMode(G3DCameraMode mode)
    {
        if (mode == G3DCameraMode.DIORAMA)
        {
            dioramaInspector.style.display = DisplayStyle.Flex;
            multiviewInspector.style.display = DisplayStyle.None;
        }
        else if (mode == G3DCameraMode.MULTIVIEW)
        {
            dioramaInspector.style.display = DisplayStyle.None;
            multiviewInspector.style.display = DisplayStyle.Flex;
        }
        else
        {
            dioramaInspector.style.display = DisplayStyle.None;
            multiviewInspector.style.display = DisplayStyle.None;
        }
    }
}
#endif
