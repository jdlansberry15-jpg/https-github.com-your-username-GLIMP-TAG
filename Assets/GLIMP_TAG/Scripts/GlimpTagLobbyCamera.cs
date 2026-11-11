using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Keeps a lightweight XR camera alive while the player is in the lobby.
/// It removes itself after the local network player prefab has spawned.
/// </summary>
public sealed class GlimpTagLobbyCamera : MonoBehaviour
{
    private void Update()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsListening)
            return;

        if (manager.SpawnManager == null ||
            manager.SpawnManager.GetLocalPlayerObject() == null)
            return;

        GameObject cameraRoot = transform.parent != null
            ? transform.parent.gameObject
            : gameObject;

        Destroy(cameraRoot);
    }
}
