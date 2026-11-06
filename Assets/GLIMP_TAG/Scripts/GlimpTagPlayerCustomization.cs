using UnityEngine;

/// <summary>
/// Simple local player name and color customization menu.
/// Add to a scene object and assign the renderers on the local avatar.
/// </summary>
public class GlimpTagPlayerCustomization : MonoBehaviour
{
    public Renderer[] avatarRenderers;
    public string PlayerName { get; private set; } = "Glimp Player";
    public Color PlayerColor { get; private set; } = Color.cyan;

    private string nameInput;
    private readonly Color[] palette =
    {
        new Color(0.15f, 0.85f, 1f),
        new Color(1f, 0.25f, 0.25f),
        new Color(0.3f, 1f, 0.35f),
        new Color(1f, 0.8f, 0.15f),
        new Color(0.75f, 0.35f, 1f),
        new Color(1f, 0.45f, 0.75f),
        Color.white,
        new Color(0.2f, 0.2f, 0.25f)
    };

    private void Start()
    {
        PlayerName = PlayerPrefs.GetString("GlimpTag_PlayerName", "Glimp Player");
        nameInput = PlayerName;

        int savedColor = PlayerPrefs.GetInt("GlimpTag_PlayerColor", 0);
        savedColor = Mathf.Clamp(savedColor, 0, palette.Length - 1);
        SetColor(savedColor);
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(20, 225, 300, 260), GUI.skin.box);
        GUILayout.Label("PLAYER CUSTOMIZATION");
        GUILayout.Label("Name");
        nameInput = GUILayout.TextField(nameInput ?? "", 16);

        if (GUILayout.Button("SAVE NAME"))
        {
            string cleaned = (nameInput ?? "").Trim();
            if (cleaned.Length > 0)
            {
                PlayerName = cleaned;
                PlayerPrefs.SetString("GlimpTag_PlayerName", PlayerName);
                PlayerPrefs.Save();
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("Choose a color");
        for (int i = 0; i < palette.Length; i++)
        {
            if (GUILayout.Button("Color " + (i + 1)))
                SetColor(i);
        }

        GUILayout.EndArea();
    }

    private void SetColor(int index)
    {
        index = Mathf.Clamp(index, 0, palette.Length - 1);
        PlayerColor = palette[index];

        if (avatarRenderers != null)
        {
            foreach (Renderer item in avatarRenderers)
            {
                if (item != null)
                    item.material.color = PlayerColor;
            }
        }

        PlayerPrefs.SetInt("GlimpTag_PlayerColor", index);
        PlayerPrefs.Save();
    }
}
