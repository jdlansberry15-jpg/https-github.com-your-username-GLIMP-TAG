using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network-synchronized avatar state and server-authoritative infection rounds.
/// Add this component to the root NetworkObject of every player prefab.
/// The server player's instance also coordinates the shared round state.
/// </summary>
public class GlimpTagNetworkPlayer : NetworkBehaviour
{
    public const int WaitingForPlayers = 0;
    public const int RoundActive = 1;
    public const int InfectionTeamWon = 2;
    public const int SurvivorsWon = 3;

    [Header("Avatar")]
    [SerializeField] private Renderer[] colorRenderers;
    [SerializeField] private Color taggedColor = new Color(1f, 0.25f, 0.08f);

    [Header("Round rules (server player object controls these)")]
    [SerializeField, Min(2)] private int minimumPlayers = 2;
    [SerializeField, Min(15f)] private float roundDurationSeconds = 120f;
    [SerializeField, Min(1f)] private float roundEndPauseSeconds = 5f;
    [SerializeField, Min(0.5f)] private float serverTagDistance = 3f;

    public NetworkVariable<bool> IsTagged = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<Color> PlayerColor = new NetworkVariable<Color>(
        Color.white, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // These variables are authoritative on the host's player object.
    // Every client reads the host player's copy for the shared round HUD.
    public NetworkVariable<int> RoundPhase = new NetworkVariable<int>(
        WaitingForPlayers, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> RoundNumber = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Integer seconds avoid sending a changed timer value every rendered frame.
    public NetworkVariable<int> RoundSecondsRemaining = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> PlayerCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public NetworkVariable<int> InfectedCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
    private float endPauseRemaining;
    private float roundTickAccumulator;

    public override void OnNetworkSpawn()
    {
        IsTagged.OnValueChanged += OnTaggedChanged;
        PlayerColor.OnValueChanged += OnColorChanged;

        if (IsServer)
            PlayerColor.Value = Random.ColorHSV(0f, 1f, 0.65f, 1f, 0.75f, 1f);

        ApplyAppearance();
    }

    public override void OnNetworkDespawn()
    {
        IsTagged.OnValueChanged -= OnTaggedChanged;
        PlayerColor.OnValueChanged -= OnColorChanged;
    }

    private void Update()
    {
        // The server-owned player object is the single round coordinator.
        if (!IsSpawned || !IsServer || OwnerClientId != NetworkManager.ServerClientId)
            return;

        TickRound(Time.deltaTime);
    }

    /// <summary>
    /// Called by the local owner's hand trigger. The server validates the
    /// sender, current infection state, target, and approximate avatar distance.
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    public void RequestTagRpc(ulong targetClientId, RpcParams rpcParams = default)
    {
        TryTagTargetOnServer(targetClientId, rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Server-only path, also usable by server-side collision handling.
    /// </summary>
    public void TryTagTargetOnServer(ulong targetClientId, ulong senderClientId)
    {
        if (!IsServer || !IsSpawned || senderClientId != OwnerClientId)
            return;

        if (targetClientId == senderClientId || !IsTagged.Value)
            return;

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
            return;

        GlimpTagNetworkPlayer coordinator = FindServerCoordinator(manager);
        if (coordinator == null || coordinator.RoundPhase.Value != RoundActive)
            return;

        if (!manager.ConnectedClients.TryGetValue(targetClientId, out NetworkClient targetClient))
            return;

        if (targetClient.PlayerObject == null)
            return;

        GlimpTagNetworkPlayer target = targetClient.PlayerObject.GetComponent<GlimpTagNetworkPlayer>();
        if (target == null || target == this || target.IsTagged.Value)
            return;

        // Root distance is a coarse server-side anti-spoof check. The hand
        // trigger supplies contact intent; synchronized hand hitboxes are
        // still required for accurate gameplay.
        if (Vector3.Distance(transform.position, target.transform.position) > coordinator.serverTagDistance)
            return;

        target.IsTagged.Value = true;
        coordinator.RefreshInfectedCount();
    }

    private void TickRound(float deltaTime)
    {
        List<GlimpTagNetworkPlayer> players = CollectConnectedPlayers();
        if (PlayerCount.Value != players.Count)
            PlayerCount.Value = players.Count;

        if (players.Count < minimumPlayers)
        {
            if (RoundPhase.Value != WaitingForPlayers)
            {
                SetAllPlayersTagged(players, false);
                RoundPhase.Value = WaitingForPlayers;
                RoundSecondsRemaining.Value = 0;
                InfectedCount.Value = 0;
                endPauseRemaining = 0f;
                roundTickAccumulator = 0f;
            }
            return;
        }

        if (RoundPhase.Value == WaitingForPlayers)
        {
            StartRound(players);
            return;
        }

        if (RoundPhase.Value == RoundActive)
        {
            roundTickAccumulator += deltaTime;
            while (roundTickAccumulator >= 1f && RoundSecondsRemaining.Value > 0)
            {
                RoundSecondsRemaining.Value--;
                roundTickAccumulator -= 1f;
            }

            RefreshInfectedCount(players);

            if (InfectedCount.Value >= players.Count)
            {
                FinishRound(InfectionTeamWon);
            }
            else if (RoundSecondsRemaining.Value <= 0)
            {
                FinishRound(SurvivorsWon);
            }

            return;
        }

        endPauseRemaining -= deltaTime;
        if (endPauseRemaining <= 0f)
            StartRound(players);
    }

    private void StartRound(List<GlimpTagNetworkPlayer> players)
    {
        if (players == null || players.Count < minimumPlayers)
            return;

        SetAllPlayersTagged(players, false);

        int firstInfectedIndex = Random.Range(0, players.Count);
        players[firstInfectedIndex].IsTagged.Value = true;

        RoundNumber.Value++;
        RoundSecondsRemaining.Value = Mathf.CeilToInt(roundDurationSeconds);
        InfectedCount.Value = 1;
        PlayerCount.Value = players.Count;
        endPauseRemaining = 0f;
        roundTickAccumulator = 0f;
        RoundPhase.Value = RoundActive;
    }

    private void FinishRound(int result)
    {
        RoundPhase.Value = result;
        endPauseRemaining = roundEndPauseSeconds;
    }

    private void SetAllPlayersTagged(List<GlimpTagNetworkPlayer> players, bool tagged)
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null)
                players[i].IsTagged.Value = tagged;
        }
    }

    private void RefreshInfectedCount()
    {
        RefreshInfectedCount(CollectConnectedPlayers());
    }

    private void RefreshInfectedCount(List<GlimpTagNetworkPlayer> players)
    {
        int infected = 0;
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] != null && players[i].IsTagged.Value)
                infected++;
        }

        if (InfectedCount.Value != infected)
            InfectedCount.Value = infected;
    }

    private static List<GlimpTagNetworkPlayer> CollectConnectedPlayers()
    {
        List<GlimpTagNetworkPlayer> players = new List<GlimpTagNetworkPlayer>();
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
            return players;

        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            GlimpTagNetworkPlayer player = client.PlayerObject.GetComponent<GlimpTagNetworkPlayer>();
            if (player != null && player.IsSpawned)
                players.Add(player);
        }

        return players;
    }

    private static GlimpTagNetworkPlayer FindServerCoordinator(NetworkManager manager)
    {
        if (manager == null || !manager.IsServer)
            return null;

        NetworkObject serverPlayer = manager.SpawnManager.GetPlayerNetworkObject(NetworkManager.ServerClientId);
        return serverPlayer != null
            ? serverPlayer.GetComponent<GlimpTagNetworkPlayer>()
            : null;
    }

    private void OnTaggedChanged(bool previous, bool current)
    {
        ApplyAppearance();
    }

    private void OnColorChanged(Color previous, Color current)
    {
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        if (colorRenderers == null)
            return;

        Color displayColor = IsTagged.Value ? taggedColor : PlayerColor.Value;
        for (int i = 0; i < colorRenderers.Length; i++)
        {
            Renderer item = colorRenderers[i];
            if (item == null)
                continue;

            item.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", displayColor);
            propertyBlock.SetColor("_BaseColor", displayColor);
            item.SetPropertyBlock(propertyBlock);
        }
    }
}
