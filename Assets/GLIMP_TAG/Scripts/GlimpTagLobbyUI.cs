using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Local Wi-Fi host/join menu for early multiplayer testing.
/// Internet matchmaking and Relay room codes are not included yet.
/// </summary>
public sealed class GlimpTagLobbyUI : MonoBehaviour
{
    private const ushort Port = 7777;
    private string hostAddress = "127.0.0.1";
    private string status = "Not connected";

    private void OnGUI()
    {
        const int width = 360;
        const int height = 270;
        Rect panel = new Rect(20, 20, width, height);

        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("GLIMP TAG");
        GUILayout.Label("FOREST LOBBY");
        GUILayout.Space(6);

        NetworkManager manager = NetworkManager.Singleton;
        bool connected = manager != null && manager.IsListening;

        if (!connected)
        {
            GUILayout.Label("Host IP address (joiners only)");
            hostAddress = GUILayout.TextField(hostAddress, 32);
            GUILayout.Label("Same Wi-Fi network required for this test.");
            GUILayout.Space(6);

            if (GUILayout.Button("HOST LOCAL GAME", GUILayout.Height(42)))
            {
                UnityTransport transport = manager != null
                    ? manager.GetComponent<UnityTransport>()
                    : null;

                if (manager == null || transport == null)
                {
                    status = "Network setup is missing.";
                }
                else
                {
                    transport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");
                    status = manager.StartHost()
                        ? "Host started. Share this headset's Wi-Fi IP."
                        : "Host could not start.";
                }
            }

            if (GUILayout.Button("JOIN LOCAL GAME", GUILayout.Height(42)))
            {
                UnityTransport transport = manager != null
                    ? manager.GetComponent<UnityTransport>()
                    : null;

                if (manager == null || transport == null)
                {
                    status = "Network setup is missing.";
                }
                else if (string.IsNullOrWhiteSpace(hostAddress))
                {
                    status = "Enter the host headset's Wi-Fi IP.";
                }
                else
                {
                    transport.SetConnectionData(hostAddress.Trim(), Port);
                    status = manager.StartClient()
                        ? "Connecting to host..."
                        : "Client could not start.";
                }
            }

            GUILayout.Label("Find the host IP in Quest Settings > Wi-Fi > connected network.");
        }
        else
        {
            GUILayout.Label("Connected players: " + manager.ConnectedClientsIds.Count);
            GUILayout.Label(manager.IsHost
                ? "Hosting on local Wi-Fi. Relay is not configured."
                : "Connected to a local Wi-Fi host.");

            if (GUILayout.Button("LEAVE GAME", GUILayout.Height(42)))
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
