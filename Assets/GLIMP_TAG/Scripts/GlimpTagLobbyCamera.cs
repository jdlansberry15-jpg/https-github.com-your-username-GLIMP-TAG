using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Keeps a lightweight XR camera available in the lobby. It is disabled while
/// the local network player camera is active, then restored when that player
/// leaves or disconnects.
/// </summary>
public sealed class GlimpTagLobbyCamera : MonoBehaviour
{
    private Camera lobbyCamera;
    private AudioListener lobbyAudioListener;

    private void Awake()
    {
        lobbyCamera = GetComponent<Camera>();
        lobbyAudioListener = GetComponent<AudioListener>();
    }

    private void Update()
    {
        NetworkManager manager = NetworkManager.Singleton;
        bool hasLocalPlayer = manager != null &&
            manager.IsListening &&
            manager.SpawnManager != null &&
            manager.SpawnManager.GetLocalPlayerObject() != null;

        SetLobbyCameraActive(!hasLocalPlayer);
    }

    public void SetLobbyCameraActive(bool active)
    {
        if (lobbyCamera == null)
            lobbyCamera = GetComponent<Camera>();
        if (lobbyAudioListener == null)
            lobbyAudioListener = GetComponent<AudioListener>();

        if (lobbyCamera != null && lobbyCamera.enabled != active)
            lobbyCamera.enabled = active;
        if (lobbyAudioListener != null && lobbyAudioListener.enabled != active)
            lobbyAudioListener.enabled = active;
    }
}
