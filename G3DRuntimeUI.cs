using UnityEngine;

public class G3DRuntimeUI : MonoBehaviour
{
    [Tooltip("The G3DCamera to control.")]
    public G3DCamera g3dCamera;

    // UI state
    private bool showUI = true;

    // Cached values for sliders/toggles
    private float viewOffsetScale;
    private float dollyZoom;
    private bool showTestFrame;
    private bool showTestStripe;
    private int testGapWidth;
    private int blackBorder;
    private int blackSpace;
    private int renderResolutionScale;

    void Start()
    {
        if (g3dCamera == null)
        {
            g3dCamera = FindObjectOfType<G3DCamera>();
        }
        if (g3dCamera != null)
        {
            // Initialize cached values from camera
            viewOffsetScale = g3dCamera.viewOffsetScale;
            dollyZoom = g3dCamera.dollyZoom;
            showTestFrame = g3dCamera.showTestFrame;
            showTestStripe = g3dCamera.showTestStripe;
            testGapWidth = g3dCamera.testGapWidth;
            blackBorder = g3dCamera.blackBorder;
            blackSpace = g3dCamera.blackSpace;
            renderResolutionScale = g3dCamera.renderResolutionScale;
        }
    }

    void Update()
    {
    }

    void OnGUI()
    {
        if (!showUI || g3dCamera == null)
            return;

        GUILayout.BeginArea(new Rect(20, 20, 350, 420), "G3D Camera Controls", GUI.skin.window);

        GUILayout.Label("View Controls");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Shift View Left"))
        {
            g3dCamera.shiftViewToLeft();
        }
        if (GUILayout.Button("Shift View Right"))
        {
            g3dCamera.shiftViewToRight();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("View Offset Scale: " + viewOffsetScale.ToString("F2"));
        float newViewOffsetScale = GUILayout.HorizontalSlider(viewOffsetScale, 0.0f, 5.0f);
        if (Mathf.Abs(newViewOffsetScale - viewOffsetScale) > 0.0001f)
        {
            viewOffsetScale = newViewOffsetScale;
            g3dCamera.viewOffsetScale = viewOffsetScale;
        }

        GUILayout.Label("Dolly Zoom: " + dollyZoom.ToString("F2"));
        float newDollyZoom = GUILayout.HorizontalSlider(dollyZoom, 0.001f, 3.0f);
        if (Mathf.Abs(newDollyZoom - dollyZoom) > 0.0001f)
        {
            dollyZoom = newDollyZoom;
            g3dCamera.dollyZoom = dollyZoom;
        }

        GUILayout.Space(10);
        GUILayout.Label("Shader/Test Parameters");

        bool newShowTestFrame = GUILayout.Toggle(showTestFrame, "Show Test Frame");
        if (newShowTestFrame != showTestFrame)
        {
            showTestFrame = newShowTestFrame;
            g3dCamera.showTestFrame = showTestFrame;
        }


        bool newShowTestStripe = GUILayout.Toggle(showTestStripe, "Show Test Stripe");
        if (newShowTestStripe != showTestStripe)
        {
            showTestStripe = newShowTestStripe;
            g3dCamera.showTestStripe = showTestStripe;
        }

        GUILayout.Label("Test Gap Width: " + testGapWidth);
        int newTestGapWidth = (int)GUILayout.HorizontalSlider(testGapWidth, 0, 100);
        if (newTestGapWidth != testGapWidth)
        {
            testGapWidth = newTestGapWidth;
            g3dCamera.testGapWidth = testGapWidth;
        }

        GUILayout.Label("Black Border: " + blackBorder);
        int newBlackBorder = (int)GUILayout.HorizontalSlider(blackBorder, 0, 100);
        if (newBlackBorder != blackBorder)
        {
            blackBorder = newBlackBorder;
            g3dCamera.blackBorder = blackBorder;
        }

        GUILayout.Label("Black Space: " + blackSpace);
        int newBlackSpace = (int)GUILayout.HorizontalSlider(blackSpace, 0, 100);
        if (newBlackSpace != blackSpace)
        {
            blackSpace = newBlackSpace;
            g3dCamera.blackSpace = blackSpace;
        }

        GUILayout.Label("Render Resolution Scale: " + renderResolutionScale + "%");
        int newRenderResolutionScale = (int)GUILayout.HorizontalSlider(renderResolutionScale, 1, 100);
        if (newRenderResolutionScale != renderResolutionScale)
        {
            renderResolutionScale = newRenderResolutionScale;
            g3dCamera.renderResolutionScale = renderResolutionScale;
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close UI (F10)"))
        {
            showUI = false;
        }

        GUILayout.EndArea();
    }

    // Reflection helpers removed; all fields are now public and accessed directly.
}
