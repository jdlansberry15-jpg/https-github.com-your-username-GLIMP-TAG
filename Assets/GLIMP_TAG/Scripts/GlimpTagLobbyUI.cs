using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal in-game host/client menu for local network testing.
/// This is not an internet room-code or matchmaking service.
/// </summary>
public class GlimpTagLobbyUI : MonoBehaviour
{
    private string status = "Not connected";

    private void OnGUI()
    {
        const int width = 300;
        const int height = 190;
        Rect panel = new Rect(20, 20, width, height);

        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("GLIMP TAG");
        GUILayout.Label("Forest Lobby");
        GUILayout.Space(8);

        NetworkManager manager = NetworkManager.Singleton;
        bool connected = manager != null && manager.IsClient;

        if (!connected)
        {
            if (GUILayout.Button("HOST GAME", GUILayout.Height(38)))
            {
                if (manager != null && manager.StartHost())
                    status = "Hosting local session";
                else
                    status = "Host could not start";
            }

            if (GUILayout.Button("JOIN LOCAL GAME", GUILayout.Height(38)))
            {
                if (manager != null && manager.StartClient())
                    status = "Connecting to configured server";
                else
                    status = "Client could not start";
            }
        }
        else
        {
            GUILayout.Label("Connected players: " + manager.ConnectedClientsIds.Count);
            if (GUILayout.Button("LEAVE GAME", GUILayout.Height(38)))
            {
                manager.Shutdown();
                status = "Disconnected";
            }
        }

        GUILayout.Space(6);
        GUILayout.Label(status);
        GUILayout.EndArea();
    }
}
