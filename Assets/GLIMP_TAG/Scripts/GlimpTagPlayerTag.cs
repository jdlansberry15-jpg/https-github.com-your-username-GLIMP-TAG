using UnityEngine;

/// <summary>
/// Local prototype for tagging. Put a trigger collider on the tagger's hand
/// and assign the player root for both players. This is not networked yet.
/// </summary>
public class GlimpTagPlayerTag : MonoBehaviour
{
    public string playerId = "Player";
    public bool isTagged;
    public GlimpTagGameMode gameMode;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("PlayerHand")) return;

        GlimpTagPlayerTag target = other.GetComponentInParent<GlimpTagPlayerTag>();
        if (target == null || target == this || target.isTagged) return;

        if (gameMode != null)
            gameMode.TryTag(this, target);
    }

    public void SetTagged(bool tagged)
    {
        isTagged = tagged;
    }
}
