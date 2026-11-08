using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Small start overlay for the first forest sandbox build.
/// A/X or the on-screen button starts movement. It auto-starts if no controller is available.
/// </summary>
public sealed class GlimpTagStartMenu : MonoBehaviour
{
    public GlimpTagLocomotion locomotion;
    [Min(1f)] public float autoStartDelay = 8f;

    private bool started;
    private float autoStartAt;

    private void Start()
    {
        if (locomotion != null)
            locomotion.enabled = false;

        autoStartAt = Time.unscaledTime + autoStartDelay;
    }

    private void Update()
    {
        if (started)
            return;

        if (ControllerStartPressed() || Time.unscaledTime >= autoStartAt)
            StartRun();
    }

    private bool ControllerStartPressed()
    {
        InputDevice left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        bool pressed;
        if (left.isValid &&
            (left.TryGetFeatureValue(CommonUsages.primaryButton, out pressed) && pressed ||
             left.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed))
            return true;

        if (right.isValid &&
            (right.TryGetFeatureValue(CommonUsages.primaryButton, out pressed) && pressed ||
             right.TryGetFeatureValue(CommonUsages.secondaryButton, out pressed) && pressed))
            return true;

        return false;
    }

    private void StartRun()
    {
        started = true;
        if (locomotion != null)
        {
            locomotion.enabled = true;
            locomotion.RecenterHandHistory();
        }
    }

    private void OnGUI()
    {
        if (started)
        {
            GUI.Label(new Rect(18, 18, 620, 34),
                "GLIMP TAG  |  Push off the ground, trees, and platforms to move");
            return;
        }

        float width = 360f;
        float height = 230f;
        Rect panel = new Rect((Screen.width - width) * 0.5f,
            (Screen.height - height) * 0.5f, width, height);

        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Space(12);
        GUILayout.Label("G L I M P   T A G", CenterStyle(28));
        GUILayout.Label("FOREST MOVEMENT TEST", CenterStyle(14));
        GUILayout.Space(16);
        GUILayout.Label("Press A / X or tap START RUN");
        GUILayout.Label("Movement starts automatically after a few seconds.");
        GUILayout.Space(10);

        if (GUILayout.Button("START RUN", GUILayout.Height(52)))
            StartRun();

        GUILayout.EndArea();
    }

    private static GUIStyle CenterStyle(int size)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = FontStyle.Bold
        };
    }
}
