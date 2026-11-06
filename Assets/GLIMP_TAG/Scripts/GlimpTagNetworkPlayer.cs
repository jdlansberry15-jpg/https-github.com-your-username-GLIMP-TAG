using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network-synchronized player state prototype using Netcode for GameObjects.
/// Add to the player NetworkObject prefab.
/// </summary>
public class GlimpTagNetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<bool> IsTagged = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(
        Color.white, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [SerializeField] private Renderer[] colorRenderers;

    public override void OnNetworkSpawn()
    {
        IsTagged.OnValueChanged += OnTaggedChanged;
        PlayerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
            PlayerColor.Value = new Color(Random.value, Random.value, Random.value);

        ApplyColor(PlayerColor.Value);
    }

    public override void OnNetworkDespawn()
    {
        IsTagged.OnValueChanged -= OnTaggedChanged;
        PlayerColor.OnValueChanged -= OnColorChanged;
    }

    private void OnTaggedChanged(bool previous, bool current)
    {
        // Hook up a visual indicator or sound in the Unity Inspector later.
    }

    private void OnColorChanged(Color previous, Color current)
    {
        ApplyColor(current);
    }

    private void ApplyColor(Color color)
    {
        if (colorRenderers == null) return;
        foreach (Renderer item in colorRenderers)
        {
            if (item != null && item.material != null)
                item.material.color = color;
        }
    }

    [ServerRpc]
    public void SetTaggedServerRpc(bool tagged)
    {
        IsTagged.Value = tagged;
    }
}
