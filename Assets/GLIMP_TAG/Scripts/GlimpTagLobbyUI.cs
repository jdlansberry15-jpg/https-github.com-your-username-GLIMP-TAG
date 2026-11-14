using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Room-code lobby for the GLIMP TAG Quest prototype.
/// Uses Unity Relay when GlimpTagRelaySession is present.
/// </summary>
public sealed class GlimpTagLobbyUI : MonoBehaviour
{
    private GlimpTagRelaySession relaySession;
    private GlimpTagVoiceChat voiceChat;
    private string joinCodeInput = string.Empty;

    private void Awake()
    {
        relaySession = GetComponent<GlimpTagRelaySession>();
        voiceChat = GetComponent<GlimpTagVoiceChat>();
    }

    private void OnGUI()
    {
        float width = Mathf.Min(500f, Screen.width - 32f);
        Rect panel = new Rect(16f, 16f, width, 510f);

        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Space(8f);
        GUILayout.Label("GLIMP TAG", HeaderStyle());
        GUILayout.Label("FOREST FRIENDS LOBBY", CenterStyle());
        GUILayout.Space(10f);

        NetworkManager manager = NetworkManager.Singleton;
        bool listening = manager != null && manager.IsListening;
        bool connected = listening && (manager.IsHost || manager.IsConnectedClient);

        if (!listening)
            DrawDisconnectedMenu();
        else if (connected)
            DrawConnectedMenu(manager);
        else
            DrawConnectingMenu(manager);

        if (voiceChat != null && voiceChat.ShouldShowControls)
            voiceChat.DrawLobbyControls();

        GUILayout.Space(10f);
        string currentStatus = relaySession != null
            ? relaySession.Status
            : "Relay room-code networking is not configured.";
        GUILayout.Label(currentStatus, WrapStyle());
        GUILayout.EndArea();
    }

    private void DrawDisconnectedMenu()
    {
        if (relaySession == null)
        {
            GUILayout.Label("Room-code networking is not configured. Rebuild the Quest project from the GLIMP TAG menu.", WrapStyle());
            return;
        }

        GUILayout.Label("CREATE A PRIVATE ROOM", CenterStyle());
        GUILayout.Label("Host a game and share the room code with your friends.", WrapStyle());

        bool wasEnabled = GUI.enabled;
        GUI.enabled = wasEnabled && !relaySession.IsBusy;

        if (GUILayout.Button("CREATE ROOM", GUILayout.Height(48f)))
            StartHostRoom();

        GUILayout.Space(12f);
        GUILayout.Label("JOIN A FRIEND", CenterStyle());
        joinCodeInput = GUILayout.TextField(joinCodeInput, 16).Trim().ToUpperInvariant();

        if (GUILayout.Button("JOIN ROOM", GUILayout.Height(48f)))
            StartJoinRoom();

        GUI.enabled = wasEnabled;
    }

    private void DrawConnectingMenu(NetworkManager manager)
    {
        GUILayout.Label("CONNECTING TO ROOM...", CenterStyle());
        GUILayout.Label("Keep this screen open while GLIMP TAG connects.", WrapStyle());

        if (GUILayout.Button("CANCEL / LEAVE", GUILayout.Height(44f)))
        {
            if (relaySession != null)
                relaySession.LeaveRoom();
            else if (manager != null)
                manager.Shutdown();
        }
    }

    private void DrawConnectedMenu(NetworkManager manager)
    {
        GUILayout.Label(manager.IsHost ? "YOU ARE THE HOST" : "YOU ARE IN A ROOM", CenterStyle());
        GUILayout.Label("Players connected: " + manager.ConnectedClientsIds.Count, CenterStyle());

        if (manager.IsHost && relaySession != null && !string.IsNullOrEmpty(relaySession.LastJoinCode))
        {
            GUILayout.Space(8f);
            GUILayout.Label("SHARE THIS ROOM CODE", CenterStyle());
            GUILayout.TextField(relaySession.LastJoinCode);
            GUILayout.Label("Your friends enter this code on their Join Room screen.", WrapStyle());

            if (GUILayout.Button("COPY ROOM CODE", GUILayout.Height(40f)))
                GUIUtility.systemCopyBuffer = relaySession.LastJoinCode;
        }
        else if (!manager.IsHost)
        {
            GUILayout.Label("Connected to the host. Have fun in the forest!", CenterStyle());
        }

        GUILayout.Space(12f);
        if (GUILayout.Button("LEAVE ROOM", GUILayout.Height(44f)))
        {
            if (relaySession != null)
                relaySession.LeaveRoom();
            else
                manager.Shutdown();
        }
    }

    private async void StartHostRoom()
    {
        if (relaySession != null)
            await relaySession.HostRoomAsync();
    }

    private async void StartJoinRoom()
    {
        if (relaySession != null)
            await relaySession.JoinRoomAsync(joinCodeInput);
    }

    private static GUIStyle HeaderStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
    }

    private static GUIStyle CenterStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }

    private static GUIStyle WrapStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}
