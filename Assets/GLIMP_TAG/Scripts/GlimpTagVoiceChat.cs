using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Room-scoped, positional proximity voice for GLIMP TAG.
/// Add to the same GameObject as NetworkManager and GlimpTagRelaySession.
/// The Relay join code selects the matching Vivox channel.
/// </summary>
[DisallowMultipleComponent]
public sealed class GlimpTagVoiceChat : MonoBehaviour
{
    [Header("Proximity voice")]
    [SerializeField, Min(1)] private int audibleDistance = 24;
    [SerializeField, Min(0)] private int conversationalDistance = 2;
    [SerializeField, Range(1f, 5f)] private float positionUpdatesPerSecond = 3f;

    [Header("Android audio permissions")]
    [Tooltip("Only enable this if GLIMP TAG needs Bluetooth audio devices on Android 12 or later.")]
    [SerializeField] private bool requestBluetoothConnectPermission = true;

    private GlimpTagRelaySession relaySession;
    private bool vivoxInitialized;
    private bool vivoxLoggedIn;
    private bool voiceOperationInProgress;
    private bool voiceAttemptedForRoom;
    private bool microphoneMuted;
    private string roomCodeBeingHandled = string.Empty;
    private string joinedChannelName = string.Empty;
    private string voiceStatus = "Voice chat idle";
    private float nextPositionUpdate;

    public string Status => voiceStatus;
    public bool IsMuted => microphoneMuted;
    public bool IsConnected => !string.IsNullOrEmpty(joinedChannelName);
    public bool ShouldShowControls =>
        voiceOperationInProgress || voiceAttemptedForRoom || IsConnected;

    private void Awake()
    {
        relaySession = GetComponent<GlimpTagRelaySession>();
    }

    private void Update()
    {
        if (voiceOperationInProgress)
            return;

        NetworkManager manager = NetworkManager.Singleton;
        bool localPlayerInRoom = manager != null &&
            manager.IsListening &&
            (manager.IsHost || manager.IsConnectedClient);

        string currentJoinCode = localPlayerInRoom && relaySession != null
            ? relaySession.LastJoinCode
            : string.Empty;

        if (!localPlayerInRoom || string.IsNullOrEmpty(currentJoinCode))
        {
            if (IsConnected)
                _ = LeaveVoiceChannelAsync();
            else if (!localPlayerInRoom)
                ResetRoomTracking();
            return;
        }

        if (!string.Equals(currentJoinCode, roomCodeBeingHandled, StringComparison.Ordinal))
        {
            roomCodeBeingHandled = currentJoinCode;
            voiceAttemptedForRoom = true;
            _ = JoinVoiceChannelAsync(currentJoinCode);
            return;
        }

        if (IsConnected && Time.unscaledTime >= nextPositionUpdate)
        {
            UpdateLocalVoicePosition(manager);
            nextPositionUpdate = Time.unscaledTime +
                1f / Mathf.Max(1f, positionUpdatesPerSecond);
        }
    }

    /// <summary>
    /// Called by the lobby's microphone button. This mutes only this player's mic.
    /// </summary>
    public void ToggleMicrophone()
    {
        if (!IsConnected || !vivoxLoggedIn)
            return;

        microphoneMuted = !microphoneMuted;
        if (microphoneMuted)
        {
            VivoxService.Instance.MuteInputDevice();
            voiceStatus = "Voice connected — your microphone is muted.";
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            voiceStatus = "Voice connected — microphone is live.";
        }
    }

    /// <summary>
    /// Allows a player to retry after denying microphone permission or a service error.
    /// </summary>
    public void RetryVoice()
    {
        if (voiceOperationInProgress || string.IsNullOrEmpty(roomCodeBeingHandled))
            return;

        voiceAttemptedForRoom = true;
        _ = JoinVoiceChannelAsync(roomCodeBeingHandled);
    }

    /// <summary>
    /// Drawn by the existing room lobby only while a room/voice attempt is active.
    /// </summary>
    public void DrawLobbyControls()
    {
        GUILayout.Space(8f);
        GUILayout.Label("PROXIMITY VOICE CHAT");
        GUILayout.Label(voiceStatus, WrapStyle());

        bool previousEnabled = GUI.enabled;
        GUI.enabled = previousEnabled && IsConnected && !voiceOperationInProgress;

        if (GUILayout.Button(
            microphoneMuted ? "UNMUTE MICROPHONE" : "MUTE MICROPHONE",
            GUILayout.Height(38f)))
        {
            ToggleMicrophone();
        }

        GUI.enabled = previousEnabled &&
            voiceAttemptedForRoom &&
            !IsConnected &&
            !voiceOperationInProgress;

        if (GUILayout.Button("RETRY VOICE CHAT", GUILayout.Height(32f)))
            RetryVoice();

        GUI.enabled = previousEnabled;
    }

    private async Task JoinVoiceChannelAsync(string relayJoinCode)
    {
        if (voiceOperationInProgress || IsConnected)
            return;

        voiceOperationInProgress = true;
        try
        {
            voiceStatus = "Requesting microphone permission...";
            bool microphoneAllowed = await EnsureMicrophonePermissionAsync();
            if (!microphoneAllowed)
            {
                voiceStatus = "Microphone permission was denied. Allow microphone access in Quest app permissions, then retry.";
                return;
            }

            voiceStatus = "Connecting to voice service...";
            await EnsureVivoxLoginAsync();

            string channelName = BuildChannelName(relayJoinCode);
            Channel3DProperties spatialAudio = new Channel3DProperties(
                Mathf.Max(1, audibleDistance),
                Mathf.Clamp(conversationalDistance, 0, Mathf.Max(1, audibleDistance)),
                1f,
                AudioFadeModel.InverseByDistance);

            await VivoxService.Instance.JoinPositionalChannelAsync(
                channelName,
                ChatCapability.Audio,
                spatialAudio);

            joinedChannelName = channelName;
            microphoneMuted = false;
            nextPositionUpdate = 0f;
            voiceStatus = "Voice connected — nearby players can hear you.";
        }
        catch (Exception exception)
        {
            voiceStatus = "Voice unavailable: " + exception.Message;
            Debug.LogException(exception, this);
        }
        finally
        {
            voiceOperationInProgress = false;
        }
    }

    private async Task EnsureVivoxLoginAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        if (!vivoxInitialized)
        {
            await VivoxService.Instance.InitializeAsync();
            vivoxInitialized = true;
        }

        if (!vivoxLoggedIn)
        {
            string displayName = PlayerPrefs.GetString("GlimpTag_PlayerName", "Glimp Player");
            displayName = string.IsNullOrWhiteSpace(displayName)
                ? "Glimp Player"
                : displayName.Trim();

            LoginOptions options = new LoginOptions
            {
                DisplayName = displayName
            };

            await VivoxService.Instance.LoginAsync(options);
            vivoxLoggedIn = true;
        }
    }

    private async Task<bool> EnsureMicrophonePermissionAsync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            return true;

        TaskCompletionSource<bool> permissionResult = new TaskCompletionSource<bool>();
        PermissionCallbacks callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permissionName => permissionResult.TrySetResult(true);
        callbacks.PermissionDenied += permissionName => permissionResult.TrySetResult(false);
        callbacks.PermissionDeniedAndDontAskAgain += permissionName => permissionResult.TrySetResult(false);

        Permission.RequestUserPermission(Permission.Microphone, callbacks);
        bool microphoneAllowed = await permissionResult.Task;
        if (!microphoneAllowed)
            return false;

        if (requestBluetoothConnectPermission && IsAndroid12OrLater())
        {
            const string bluetoothPermission = "android.permission.BLUETOOTH_CONNECT";
            if (!Permission.HasUserAuthorizedPermission(bluetoothPermission))
            {
                TaskCompletionSource<bool> bluetoothResult = new TaskCompletionSource<bool>();
                PermissionCallbacks bluetoothCallbacks = new PermissionCallbacks();
                bluetoothCallbacks.PermissionGranted += permissionName => bluetoothResult.TrySetResult(true);
                bluetoothCallbacks.PermissionDenied += permissionName => bluetoothResult.TrySetResult(false);
                bluetoothCallbacks.PermissionDeniedAndDontAskAgain += permissionName => bluetoothResult.TrySetResult(false);
                Permission.RequestUserPermission(bluetoothPermission, bluetoothCallbacks);
                await bluetoothResult.Task;
            }
        }

        return true;
#else
        return true;
#endif
    }

    private static bool IsAndroid12OrLater()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass version = new AndroidJavaClass("android.os.Build$VERSION"))
            return version.GetStatic<int>("SDK_INT") >= 31;
#else
        return false;
#endif
    }

    private void UpdateLocalVoicePosition(NetworkManager manager)
    {
        if (string.IsNullOrEmpty(joinedChannelName) || manager == null ||
            manager.SpawnManager == null)
            return;

        NetworkObject localPlayer = manager.SpawnManager.GetLocalPlayerObject();
        if (localPlayer != null)
            VivoxService.Instance.Set3DPosition(localPlayer.gameObject, joinedChannelName);
    }

    private async Task LeaveVoiceChannelAsync()
    {
        if (voiceOperationInProgress || string.IsNullOrEmpty(joinedChannelName))
            return;

        voiceOperationInProgress = true;
        string channelToLeave = joinedChannelName;
        try
        {
            await VivoxService.Instance.LeaveChannelAsync(channelToLeave);
            voiceStatus = "Voice chat disconnected.";
        }
        catch (Exception exception)
        {
            voiceStatus = "Voice disconnect warning: " + exception.Message;
            Debug.LogWarning("[GLIMP TAG Voice] " + exception, this);
        }
        finally
        {
            joinedChannelName = string.Empty;
            microphoneMuted = false;
            voiceOperationInProgress = false;
            ResetRoomTracking();
        }
    }

    private void ResetRoomTracking()
    {
        if (!IsConnected && !voiceOperationInProgress)
        {
            roomCodeBeingHandled = string.Empty;
            voiceAttemptedForRoom = false;
            if (string.IsNullOrEmpty(voiceStatus) ||
                voiceStatus.StartsWith("Voice connected", StringComparison.Ordinal))
                voiceStatus = "Voice chat idle";
        }
    }

    private static string BuildChannelName(string relayJoinCode)
    {
        StringBuilder builder = new StringBuilder("GLIMPTAG");
        if (!string.IsNullOrEmpty(relayJoinCode))
        {
            foreach (char character in relayJoinCode)
            {
                if (char.IsLetterOrDigit(character))
                    builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static GUIStyle WrapStyle()
    {
        return new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };
    }
}
