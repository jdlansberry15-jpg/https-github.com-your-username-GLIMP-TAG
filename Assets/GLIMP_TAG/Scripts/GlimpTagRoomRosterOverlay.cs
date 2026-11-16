using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime-installed development overlay for room roster and Vivox moderation.
/// It does not require a prefab or scene edit, keeping the Chromebook workflow simple.
/// </summary>
public sealed class GlimpTagRoomRosterOverlay : MonoBehaviour
{
    private static GlimpTagRoomRosterOverlay instance;
    private readonly List<VivoxParticipant> participants = new List<VivoxParticipant>();
    private readonly HashSet<string> speakingNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool subscribedToVivox;
    private string playerNameInput = "Glimp Player";
    private GUIStyle headerStyle;
    private GUIStyle rowStyle;
    private GUIStyle detailStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (FindObjectOfType<GlimpTagRoomRosterOverlay>() != null)
            return;

        GameObject overlayObject = new GameObject("GLIMP TAG Room Roster Overlay");
        DontDestroyOnLoad(overlayObject);
        overlayObject.AddComponent<GlimpTagRoomRosterOverlay>();
    }

    private void Awake()
    {
        instance = this;
        playerNameInput = PlayerPrefs.GetString("GlimpTag_PlayerName", "Glimp Player");
        TrySubscribeVivox();
    }

    private void Update()
    {
        if (!subscribedToVivox)
            TrySubscribeVivox();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        foreach (VivoxParticipant participant in participants)
            UnsubscribeParticipant(participant);
        participants.Clear();
        speakingNames.Clear();

        if (!subscribedToVivox)
            return;

        try
        {
            VivoxService.Instance.ParticipantAddedToChannel -= HandleParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel -= HandleParticipantRemoved;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[GLIMP TAG Roster] Vivox event cleanup warning: " + exception.Message);
        }

        subscribedToVivox = false;
    }

    /// <summary>
    /// Used by remote world-space nameplates to mirror Vivox speaking state.
    /// Names are matched case-insensitively to the server-synchronized roster.
    /// </summary>
    public static bool IsDisplayNameSpeaking(string displayName)
    {
        if (instance == null || string.IsNullOrWhiteSpace(displayName))
            return false;

        return instance.speakingNames.Contains(displayName);
    }

    private void TrySubscribeVivox()
    {
        if (subscribedToVivox)
            return;

        try
        {
            VivoxService.Instance.ParticipantAddedToChannel += HandleParticipantAdded;
            VivoxService.Instance.ParticipantRemovedFromChannel += HandleParticipantRemoved;
            subscribedToVivox = true;
        }
        catch (Exception)
        {
            // Vivox may not be initialized yet. Update retries until the service is ready.
        }
    }

    private void HandleParticipantAdded(VivoxParticipant participant)
    {
        if (participant == null ||
            string.IsNullOrEmpty(participant.ChannelName) ||
            !participant.ChannelName.StartsWith("GLIMPTAG", StringComparison.Ordinal))
            return;

        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i] != null &&
                participants[i].ChannelName == participant.ChannelName &&
                participants[i].PlayerId == participant.PlayerId)
            {
                UnsubscribeParticipant(participants[i]);
                participants[i] = participant;
                SubscribeParticipant(participant);
                RefreshSpeakingNames();
                return;
            }
        }

        participants.Add(participant);
        SubscribeParticipant(participant);
        RefreshSpeakingNames();
    }

    private void HandleParticipantRemoved(VivoxParticipant participant)
    {
        if (participant == null)
            return;

        for (int i = participants.Count - 1; i >= 0; i--)
        {
            VivoxParticipant existing = participants[i];
            if (existing == null ||
                (existing.ChannelName == participant.ChannelName &&
                 existing.PlayerId == participant.PlayerId))
            {
                UnsubscribeParticipant(existing);
                participants.RemoveAt(i);
            }
        }

        RefreshSpeakingNames();
    }

    private void SubscribeParticipant(VivoxParticipant participant)
    {
        if (participant == null)
            return;

        participant.ParticipantSpeechDetected += HandleParticipantStateChanged;
        participant.ParticipantMuteStateChanged += HandleParticipantStateChanged;
    }

    private void UnsubscribeParticipant(VivoxParticipant participant)
    {
        if (participant == null)
            return;

        participant.ParticipantSpeechDetected -= HandleParticipantStateChanged;
        participant.ParticipantMuteStateChanged -= HandleParticipantStateChanged;
    }

    private void HandleParticipantStateChanged()
    {
        RefreshSpeakingNames();
    }

    private void RefreshSpeakingNames()
    {
        speakingNames.Clear();
        foreach (VivoxParticipant participant in participants)
        {
            if (participant == null || participant.IsSelf || participant.IsMuted ||
                !participant.SpeechDetected || string.IsNullOrWhiteSpace(participant.DisplayName))
                continue;

            speakingNames.Add(participant.DisplayName);
        }
    }

    private void OnGUI()
    {
        float width = Mathf.Min(360f, Screen.width - 24f);
        float x = Screen.width - width - 12f;
        float height = Mathf.Min(Screen.height - 24f, 680f);
        Rect panel = new Rect(x, 12f, width, height);

        GUI.Box(panel, GUIContent.none);
        GUILayout.BeginArea(new Rect(panel.x + 10f, panel.y + 8f, panel.width - 20f, panel.height - 16f));

        EnsureStyles();
        GUILayout.Label("GLIMP TAG", headerStyle);
        GUILayout.Label("ROOM ROSTER", detailStyle);

        NetworkManager manager = NetworkManager.Singleton;
        bool inSession = manager != null && manager.IsListening;

        if (!inSession)
            DrawPlayerNameSetting();
        else
            DrawNetworkPlayers(manager);

        DrawVoiceParticipants();

        GUILayout.EndArea();
    }

    private void DrawPlayerNameSetting()
    {
        GUILayout.Space(8f);
        GUILayout.Label("PLAYER NAME", detailStyle);
        playerNameInput = GUILayout.TextField(playerNameInput, 20);

        if (GUILayout.Button("SAVE PLAYER NAME", GUILayout.Height(34f)))
        {
            string cleaned = GlimpTagNetworkPlayer.SanitizeDisplayName(playerNameInput);

            PlayerPrefs.SetString("GlimpTag_PlayerName", cleaned);
            PlayerPrefs.Save();
            playerNameInput = cleaned;
        }

        GUILayout.Label("Save this before creating or joining a room. Vivox uses this name in the voice roster.", detailStyle);
    }

    private void DrawNetworkPlayers(NetworkManager manager)
    {
        GUILayout.Space(8f);
        GUILayout.Label("PLAYERS IN GAME", detailStyle);

        if (manager.ConnectedClientsList == null || manager.ConnectedClientsList.Count == 0)
        {
            GUILayout.Label("Waiting for players...", detailStyle);
            return;
        }

        foreach (NetworkClient client in manager.ConnectedClientsList)
        {
            if (client == null || client.PlayerObject == null)
                continue;

            GlimpTagNetworkPlayer player = client.PlayerObject.GetComponent<GlimpTagNetworkPlayer>();
            string playerName = player != null
                ? player.DisplayName.Value.ToString()
                : "Player " + client.ClientId;
            if (string.IsNullOrWhiteSpace(playerName))
                playerName = "Player " + client.ClientId;

            string playerState = player != null && player.IsTagged.Value
                ? "INFECTED"
                : "SAFE";

            GUILayout.Label(playerName + "  —  " + playerState, rowStyle);
        }
    }

    private void DrawVoiceParticipants()
    {
        GUILayout.Space(12f);
        GUILayout.Label("VOICE CHAT", detailStyle);

        if (!subscribedToVivox)
        {
            GUILayout.Label("Waiting for Vivox to initialize...", detailStyle);
            return;
        }

        bool foundRemote = false;
        foreach (VivoxParticipant participant in participants)
        {
            if (participant == null || participant.IsSelf)
                continue;

            foundRemote = true;
            string voiceState = participant.IsMuted
                ? "MUTED FOR YOU"
                : (participant.SpeechDetected ? "SPEAKING" : "VOICE READY");

            GUILayout.BeginHorizontal();
            GUILayout.Label(participant.DisplayName + " — " + voiceState, rowStyle);

            if (GUILayout.Button(
                participant.IsMuted ? "UNMUTE" : "MUTE",
                GUILayout.Width(76f),
                GUILayout.Height(30f)))
            {
                if (participant.IsMuted)
                    participant.UnmutePlayerLocally();
                else
                    participant.MutePlayerLocally();
            }

            GUILayout.EndHorizontal();
        }

        if (!foundRemote)
            GUILayout.Label("No other voice participants detected yet.", detailStyle);

        GUILayout.Label("MUTE only affects what you hear. It does not mute another player's microphone for everyone.", detailStyle);
    }

    private void EnsureStyles()
    {
        if (headerStyle != null)
            return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        rowStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        detailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}