using UnityEngine;

/// <summary>
/// Local prototype tag state. Attach this to each player root.
/// Any collider beneath another player root can register a touch.
/// </summary>
public class GlimpTagPlayerTag : MonoBehaviour
{
    public string playerId = "Player";
    public bool isTagged;
    public GlimpTagGameMode gameMode;

    private void OnTriggerEnter(Collider other)
    {
        GlimpTagPlayerTag target = other.GetComponentInParent<GlimpTagPlayerTag>();
        if (target == null || target == this || target.isTagged)
            return;

        if (gameMode != null)
            gameMode.TryTag(this, target);
    }

    public void SetTagged(bool tagged)
    {
        isTagged = tagged;
    }
}
