using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Place on each player's hand trigger collider beneath the player's root
/// NetworkObject. The local owner requests a tag; the server validates it.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlimpTagNetworkTagger : NetworkBehaviour
{
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

        // Client-owned hand contact sends a request from the player's
        // NetworkObject. RpcInvokePermission.Owner rejects other clients.
        if (tagger.IsOwner)
            tagger.RequestTagRpc(target.OwnerClientId);
        else if (tagger.IsServer)
            tagger.TryTagTargetOnServer(target.OwnerClientId, tagger.OwnerClientId);
    }
}
