using UnityEngine;

using System.IO;
using System;

public class G3DRuntimeUI : MonoBehaviour
{
    [Tooltip("The G3DCamera to control.")]
    public G3DCamera g3dCamera;

    // UI state
    private bool showUI = false;

    // Cached values for sliders/toggles
    private float viewOffsetScale;
    private float dollyZoom;
    private bool showTestFrame;
    private bool showTestStripe;
    private int testGapWidth;
    private int blackBorder;
    private int blackSpace;
    private int renderResolutionScale;

    private string configPath;

    [Serializable]
    private class Config
    {
        public float viewOffsetScale;
        public float dollyZoom;
        public bool showTestFrame;
        public bool showTestStripe;
        public int testGapWidth;
        public int blackBorder;
        public int blackSpace;
        public int renderResolutionScale;
    }

    private float saveFeedbackTimer = 0f;
    private const float saveFeedbackDuration = 2.0f;

    void Start()
    {
        configPath = Path.Combine(Application.persistentDataPath, "G3DRuntimeUIConfig.json");
        if (g3dCamera == null)
        {
            g3dCamera = FindObjectOfType<G3DCamera>();
        }
        if (g3dCamera != null)
        {
            if (!LoadConfig())
            {
                // Initialize cached values from camera if no config loaded
                viewOffsetScale = g3dCamera.viewOffsetScale;
                dollyZoom = g3dCamera.dollyZoom;
                showTestFrame = g3dCamera.showTestFrame;
                showTestStripe = g3dCamera.showTestStripe;
                testGapWidth = g3dCamera.testGapWidth;
                blackBorder = g3dCamera.blackBorder;
                blackSpace = g3dCamera.blackSpace;
                renderResolutionScale = g3dCamera.renderResolutionScale;
            }
            ApplyConfigToCamera();
        }
    }

    // Hybrid input system detection for Ctrl+Alt+Shift+D held for 2 seconds
    private float debugComboHeldTime = 0f;
    private const float debugComboRequiredTime = 2.0f;
    void Update()
    {
        bool comboPressed = false;

        // Try new Input System (if available)
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        try
        {
            var keyboardType = System.Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
            if (keyboardType != null)
            {
                var keyboard = keyboardType.GetProperty("current").GetValue(null, null);
                if (keyboard != null)
                {
                    bool ctrl = (bool)keyboardType.GetProperty("ctrlKey").GetValue(keyboard, null).GetType().GetProperty("isPressed").GetValue(keyboardType.GetProperty("ctrlKey").GetValue(keyboard, null), null);
                    bool alt = (bool)keyboardType.GetProperty("altKey").GetValue(keyboard, null).GetType().GetProperty("isPressed").GetValue(keyboardType.GetProperty("altKey").GetValue(keyboard, null), null);
                    bool shift = (bool)keyboardType.GetProperty("shiftKey").GetValue(keyboard, null).GetType().GetProperty("isPressed").GetValue(keyboardType.GetProperty("shiftKey").GetValue(keyboard, null), null);
                    var dKey = keyboardType.GetProperty("dKey").GetValue(keyboard, null);
                    bool d = (bool)dKey.GetType().GetProperty("isPressed").GetValue(dKey, null);
                    comboPressed = ctrl && alt && shift && d;
                }
            }
        }
        catch { /* fallback to old system if reflection fails */ }
#else
        // Fallback: Old Input System
        if (!comboPressed)
        {
            comboPressed = Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.D);
        }
#endif
        if (comboPressed)
        {
            debugComboHeldTime += Time.unscaledDeltaTime;
            if (debugComboHeldTime >= debugComboRequiredTime && !showUI)
            {
                showUI = true;
            }
        }
        else
        {
            debugComboHeldTime = 0f;
        }

        // Save feedback timer
        if (saveFeedbackTimer > 0f)
        {
            saveFeedbackTimer -= Time.unscaledDeltaTime;
            if (saveFeedbackTimer < 0f) saveFeedbackTimer = 0f;
        }
    }

    void OnGUI()
    {
        if (!showUI || g3dCamera == null)
            return;

        GUILayout.BeginArea(new Rect(20, 20, 370, 500), "G3D Camera Controls", GUI.skin.window);

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
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Config"))
        {
            SaveConfig();
            saveFeedbackTimer = saveFeedbackDuration;
        }
        if (GUILayout.Button("Reset to Default"))
        {
            ResetConfig();
        }
        GUILayout.EndHorizontal();

        if (saveFeedbackTimer > 0f)
        {
            GUILayout.Space(5);
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.green;
            style.fontStyle = FontStyle.Bold;
            GUILayout.Label("Configuration saved!", style);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Close UI"))
        {
            showUI = false;
        }

        GUILayout.EndArea();
    }

    private void ApplyConfigToCamera()
    {
        if (g3dCamera == null) return;
        g3dCamera.viewOffsetScale = viewOffsetScale;
        g3dCamera.dollyZoom = dollyZoom;
        g3dCamera.showTestFrame = showTestFrame;
        g3dCamera.showTestStripe = showTestStripe;
        g3dCamera.testGapWidth = testGapWidth;
        g3dCamera.blackBorder = blackBorder;
        g3dCamera.blackSpace = blackSpace;
        g3dCamera.renderResolutionScale = renderResolutionScale;
    }

    private bool LoadConfig()
    {
        if (!File.Exists(configPath))
            return false;
        try
        {
            string json = File.ReadAllText(configPath);
            Config cfg = JsonUtility.FromJson<Config>(json);
            if (cfg == null) return false;
            viewOffsetScale = cfg.viewOffsetScale;
            dollyZoom = cfg.dollyZoom;
            showTestFrame = cfg.showTestFrame;
            showTestStripe = cfg.showTestStripe;
            testGapWidth = cfg.testGapWidth;
            blackBorder = cfg.blackBorder;
            blackSpace = cfg.blackSpace;
            renderResolutionScale = cfg.renderResolutionScale;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to load G3DRuntimeUI config: " + e.Message);
            return false;
        }
    }

    private void SaveConfig()
    {
        try
        {
            Config cfg = new Config
            {
                viewOffsetScale = viewOffsetScale,
                dollyZoom = dollyZoom,
                showTestFrame = showTestFrame,
                showTestStripe = showTestStripe,
                testGapWidth = testGapWidth,
                blackBorder = blackBorder,
                blackSpace = blackSpace,
                renderResolutionScale = renderResolutionScale
            };
            string json = JsonUtility.ToJson(cfg, true);
            File.WriteAllText(configPath, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to save G3DRuntimeUI config: " + e.Message);
        }
    }

    private void ResetConfig()
    {
        try
        {
            if (File.Exists(configPath))
                File.Delete(configPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed to delete G3DRuntimeUI config: " + e.Message);
        }
        // Reload the scene to reset everything
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    // Reflection helpers removed; all fields are now public and accessed directly.
}
