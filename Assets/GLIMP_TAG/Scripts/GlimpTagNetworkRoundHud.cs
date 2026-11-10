using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple screen-space debug HUD for the shared infection round.
/// Add to an active scene object. Replace with a world-space wrist UI later.
/// </summary>
public sealed class GlimpTagNetworkRoundHud : MonoBehaviour
{
    [SerializeField] private bool showOfflineHint = false;

    private GUIStyle titleStyle;
    private GUIStyle detailStyle;

    private void OnGUI()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
        {
            if (showOfflineHint)
                DrawPanel("GLIMP TAG", "Start or join a network session to play infection.");
            return;
        }

        GlimpTagNetworkPlayer localPlayer = null;
        NetworkObject localObject = manager.SpawnManager.GetLocalPlayerObject();
        if (localObject != null)
            localPlayer = localObject.GetComponent<GlimpTagNetworkPlayer>();

        GlimpTagNetworkPlayer coordinator = FindCoordinator(manager);
        if (coordinator == null)
        {
            DrawPanel("GLIMP TAG", "Waiting for the network player objects...");
            return;
        }

        string phaseText;
        switch (coordinator.RoundPhase.Value)
        {
            case GlimpTagNetworkPlayer.RoundActive:
                phaseText = "INFECTION ROUND";
                break;
            case GlimpTagNetworkPlayer.InfectionTeamWon:
                phaseText = "INFECTED TEAM WINS";
                break;
            case GlimpTagNetworkPlayer.SurvivorsWon:
                phaseText = "SURVIVORS WIN";
                break;
            default:
                phaseText = "WAITING FOR PLAYERS";
                break;
        }

        string details = "Round " + coordinator.RoundNumber.Value +
            "   Players " + coordinator.PlayerCount.Value +
            "   Infected " + coordinator.InfectedCount.Value + "\n";

        if (coordinator.RoundPhase.Value == GlimpTagNetworkPlayer.RoundActive)
            details += "Time: " + coordinator.RoundSecondsRemaining.Value + "s\n";

        if (localPlayer != null)
            details += localPlayer.IsTagged.Value ? "YOU ARE INFECTED" : "YOU ARE SAFE";

        DrawPanel(phaseText, details);
    }

    private static GlimpTagNetworkPlayer FindCoordinator(NetworkManager manager)
    {
        foreach (NetworkObject networkObject in manager.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null || networkObject.OwnerClientId != NetworkManager.ServerClientId)
                continue;

            GlimpTagNetworkPlayer player = networkObject.GetComponent<GlimpTagNetworkPlayer>();
            if (player != null)
                return player;
        }

        return null;
    }

    private void DrawPanel(string title, string details)
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
        }

        float width = Mathf.Min(440f, Screen.width - 24f);
        Rect area = new Rect((Screen.width - width) * 0.5f, 16f, width, 108f);
        GUI.Box(area, GUIContent.none);
        GUILayout.BeginArea(new Rect(area.x + 8f, area.y + 5f, area.width - 16f, area.height - 10f));
        GUILayout.Label(title, titleStyle, GUILayout.Height(32f));
        GUILayout.Label(details, detailStyle, GUILayout.ExpandHeight(true));
        GUILayout.EndArea();
    }
}
