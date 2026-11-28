using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Place on each player's hand trigger collider beneath the player's root
/// NetworkObject. The local owner requests a tag; the server validates it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlimpTagNetworkTagger : NetworkBehaviour
{
    [SerializeField] private bool isLeftHand;
    [SerializeField, Min(0.05f)] private float localRequestCooldownSeconds = 0.12f;

    private float nextRequestTime;

    public bool IsLeftHand => isLeftHand;

    public void ConfigureHand(bool leftHand)
    {
        isLeftHand = leftHand;
    }

    private void Reset()
    {
        Collider handCollider = GetComponent<Collider>();
        handCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        GlimpTagNetworkPlayer tagger = GetComponentInParent<GlimpTagNetworkPlayer>();
        GlimpTagNetworkPlayer target = other.GetComponentInParent<GlimpTagNetworkPlayer>();

        if (tagger == null || target == null || tagger == target)
            return;

        if (!tagger.IsTagged.Value || target.IsTagged.Value)
            return;

        // Throttle each local hand before sending. The player component also
        // applies a combined per-player cooldown on the server.
        if (Time.unscaledTime < nextRequestTime)
            return;

        nextRequestTime = Time.unscaledTime + Mathf.Max(0.05f, localRequestCooldownSeconds);

        // Client-owned hand contact sends a request from the player's
        // NetworkObject. RpcInvokePermission.Owner rejects other clients.
        if (tagger.IsOwner)
            tagger.RequestTagRpc(target.OwnerClientId, isLeftHand);
        else if (tagger.IsServer)
            tagger.TryTagTargetOnServer(target.OwnerClientId, tagger.OwnerClientId, isLeftHand);
    }
}
