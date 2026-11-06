using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative network tag trigger prototype.
/// Put on a trigger collider belonging to the player's tagging hand.
/// The other player's hand collider must have a GlimpTagNetworkPlayer in its parents.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GlimpTagNetworkTagger : NetworkBehaviour
{
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        GlimpTagNetworkPlayer target = other.GetComponentInParent<GlimpTagNetworkPlayer>();
        GlimpTagNetworkPlayer tagger = GetComponentInParent<GlimpTagNetworkPlayer>();

        if (target == null || tagger == null || target == tagger) return;
        if (!tagger.IsTagged.Value || target.IsTagged.Value) return;

        target.IsTagged.Value = true;
    }
}
