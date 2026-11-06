using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Small local tag/infection round controller.
/// Assign player components in the Inspector. Multiplayer authority is not included.
/// </summary>
public class GlimpTagGameMode : MonoBehaviour
{
    public GlimpTagPlayerTag[] players;
    public bool infectionMode = true;
    public UnityEvent onRoundWon;
    public UnityEvent onPlayerTagged;

    private bool roundFinished;

    private void Start()
    {
        ResetRound();
    }

    public void TryTag(GlimpTagPlayerTag tagger, GlimpTagPlayerTag target)
    {
        if (roundFinished || tagger == null || target == null) return;
        if (!tagger.isTagged || target.isTagged) return;

        target.SetTagged(true);
        onPlayerTagged?.Invoke();
        CheckForWinner();
    }

    public void ResetRound()
    {
        roundFinished = false;
        if (players == null) return;

        foreach (GlimpTagPlayerTag player in players)
        {
            if (player == null) continue;
            player.gameMode = this;
            player.SetTagged(false);
        }

        if (infectionMode && players.Length > 0 && players[0] != null)
            players[0].SetTagged(true);
    }

    private void CheckForWinner()
    {
        if (!infectionMode || players == null || players.Length == 0) return;

        foreach (GlimpTagPlayerTag player in players)
        {
            if (player != null && !player.isTagged)
                return;
        }

        roundFinished = true;
        onRoundWon?.Invoke();
    }
}
