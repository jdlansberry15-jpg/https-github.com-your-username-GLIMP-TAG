using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple main menu prototype. Assign the name of the gameplay scene in the Inspector.
/// </summary>
public class GlimpTagMainMenu : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "ForestArena";
    [SerializeField] private GlimpTagPlayerCustomization customization;

    private bool showCustomization;

    private void OnGUI()
    {
        float width = 360f;
        float left = (Screen.width - width) * 0.5f;

        GUILayout.BeginArea(new Rect(left, 35f, width, Screen.height - 70f), GUI.skin.box);
        GUILayout.Space(18);
        GUILayout.Label("G L I M P   T A G", CenteredStyle(30));
        GUILayout.Label("RUN • CLIMB • TAG", CenteredStyle(14));
        GUILayout.Space(24);

        if (GUILayout.Button("PLAY", GUILayout.Height(58)))
        {
            if (!string.IsNullOrWhiteSpace(gameplaySceneName))
                SceneManager.LoadScene(gameplaySceneName);
        }

        GUILayout.Space(10);
        if (GUILayout.Button(showCustomization ? "HIDE CUSTOMIZATION" : "CUSTOMIZE", GUILayout.Height(44)))
            showCustomization = !showCustomization;

        if (showCustomization && customization != null)
        {
            GUILayout.Space(12);
            GUILayout.Label("Player: " + customization.PlayerName);
            GUILayout.Label("Choose your name and color in the customization panel.");
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("GLIMP TAG • EARLY PROTOTYPE", CenteredStyle(11));
        GUILayout.EndArea();
    }

    private GUIStyle CenteredStyle(int size)
    {
        return new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = size,
            fontStyle = FontStyle.Bold
        };
    }
}
