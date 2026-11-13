using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Creates and joins private GLIMP TAG friend rooms through Unity Relay.
/// Attach this component to the same GameObject as NetworkManager and UnityTransport.
/// </summary>
[DisallowMultipleComponent]
public sealed class GlimpTagRelaySession : MonoBehaviour
{
    [SerializeField, Range(1, 7)]
    private int maxRemotePlayers = 7;

    [SerializeField]
    private string relayConnectionType = "dtls";

    private NetworkManager networkManager;
    private UnityTransport unityTransport;
    private bool operationInProgress;
    private string lastJoinCode = string.Empty;
    private string status = "Not connected";

    public string LastJoinCode => lastJoinCode;
    public string Status => status;
    public bool IsBusy => operationInProgress;

    private void Awake()
    {
        ResolveNetworkComponents();
    }

    public async Task<bool> HostRoomAsync()
    {
        if (!TryBeginOperation())
            return false;

        try
        {
            SetStatus("Signing in and creating a private room...");
            await EnsureUnityServicesReadyAsync();
            ResolveNetworkComponents();
            EnsureNetworkCanStart();

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(
                Mathf.Clamp(maxRemotePlayers, 1, 7));

            unityTransport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, relayConnectionType));

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            if (string.IsNullOrWhiteSpace(joinCode))
                throw new InvalidOperationException("Unity Relay did not return a room code.");

            if (!networkManager.StartHost())
                throw new InvalidOperationException("Netcode could not start the host. Check the NetworkManager and generated player prefab.");

            lastJoinCode = joinCode.Trim().ToUpperInvariant();
            SetStatus("Room created. Share the code with your friends.");
            return true;
        }
        catch (Exception exception)
        {
            SetStatus("Room creation failed: " + exception.Message);
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            operationInProgress = false;
        }
    }

    public async Task<bool> JoinRoomAsync(string joinCode)
    {
        if (!TryBeginOperation())
            return false;

        try
        {
            string normalizedCode = (joinCode ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedCode))
                throw new ArgumentException("Enter your friend's room code first.", nameof(joinCode));

            SetStatus("Signing in and joining the room...");
            await EnsureUnityServicesReadyAsync();
            ResolveNetworkComponents();
            EnsureNetworkCanStart();

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(normalizedCode);

            unityTransport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, relayConnectionType));

            if (!networkManager.StartClient())
                throw new InvalidOperationException("Netcode could not start the client. Check the NetworkManager and generated player prefab.");

            lastJoinCode = normalizedCode;
            SetStatus("Connecting to your friend's room...");
            return true;
        }
        catch (Exception exception)
        {
            SetStatus("Joining failed: " + exception.Message);
            Debug.LogException(exception, this);
            return false;
        }
        finally
        {
            operationInProgress = false;
        }
    }

    public void LeaveRoom()
    {
        if (operationInProgress)
        {
            SetStatus("Please wait for the current connection step to finish.");
            return;
        }

        ResolveNetworkComponents();
        if (networkManager != null && networkManager.IsListening)
            networkManager.Shutdown();

        lastJoinCode = string.Empty;
        SetStatus("Returned to the GLIMP TAG lobby.");
    }

    private bool TryBeginOperation()
    {
        if (operationInProgress)
        {
            SetStatus("Please wait. A connection is already being prepared.");
            return false;
        }

        operationInProgress = true;
        return true;
    }

    private async Task EnsureUnityServicesReadyAsync()
    {
        if (UnityServices.State != ServicesInitializationState.Initialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void ResolveNetworkComponents()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (unityTransport == null && networkManager != null)
            unityTransport = networkManager.GetComponent<UnityTransport>();
    }

    private void EnsureNetworkCanStart()
    {
        if (networkManager == null)
            throw new InvalidOperationException("No NetworkManager.Singleton was found.");

        if (unityTransport == null)
            throw new InvalidOperationException("UnityTransport must be on the NetworkManager GameObject.");

        if (networkManager.IsListening)
            throw new InvalidOperationException("You are already in a room. Leave it before hosting or joining another.");
    }

    private void SetStatus(string message)
    {
        status = message;
        Debug.Log("[GLIMP TAG Relay] " + message, this);
    }
}
