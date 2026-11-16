using UnityEngine;

/// <summary>
/// Displays the server-approved player name and infection/voice state above
/// remote avatars. The local XR player never sees its own remote visual root.
/// </summary>
[DisallowMultipleComponent]
public sealed class GlimpTagNetworkNameplate : MonoBehaviour
{
    [SerializeField] private GlimpTagNetworkPlayer networkPlayer;
    [SerializeField] private TextMesh nameText;
    [SerializeField] private Transform billboardTransform;

    private Camera localCamera;
    private string previousText = string.Empty;
    private Color previousColor;

    public void Configure(GlimpTagNetworkPlayer player, TextMesh text)
    {
        networkPlayer = player;
        nameText = text;
        billboardTransform = text != null ? text.transform : null;
    }

    private void Awake()
    {
        if (networkPlayer == null)
            networkPlayer = GetComponent<GlimpTagNetworkPlayer>();

        if (nameText == null)
            nameText = GetComponentInChildren<TextMesh>(true);

        if (billboardTransform == null && nameText != null)
            billboardTransform = nameText.transform;
    }

    private void LateUpdate()
    {
        if (networkPlayer == null || nameText == null)
            return;

        string playerName = networkPlayer.DisplayName.Value.ToString();
        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "GLIMP PLAYER";

        bool infected = networkPlayer.IsTagged.Value;
        bool speaking = GlimpTagRoomRosterOverlay.IsDisplayNameSpeaking(playerName);

        string state = infected ? "INFECTED" : "SAFE";
        string nextText = playerName +
            (speaking ? "\nSPEAKING" : string.Empty) +
            "\n" + state;

        if (!string.Equals(previousText, nextText, System.StringComparison.Ordinal))
        {
            nameText.text = nextText;
            previousText = nextText;
        }

        Color nextColor = speaking
            ? new Color(0.35f, 1f, 0.88f, 1f)
            : (infected ? new Color(1f, 0.48f, 0.20f, 1f) : Color.white);

        if (previousColor != nextColor)
        {
            nameText.color = nextColor;
            previousColor = nextColor;
        }

        if (localCamera == null || !localCamera.isActiveAndEnabled)
            localCamera = Camera.main;

        if (billboardTransform != null && localCamera != null)
        {
            Vector3 direction = billboardTransform.position - localCamera.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
                billboardTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
