using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enables the XR rig only for the owning player and configures remote avatars
/// to be kinematic, camera-free visual replicas.
/// </summary>
public sealed class GlimpTagNetworkPlayerRig : NetworkBehaviour
{
    [Header("Local XR rig")]
    [SerializeField] private Rigidbody playerBody;
    [SerializeField] private Camera localCamera;
    [SerializeField] private AudioListener localAudioListener;
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private Transform leftHandSource;
    [SerializeField] private Transform rightHandSource;
    [SerializeField] private Collider leftHandTrigger;
    [SerializeField] private Collider rightHandTrigger;
    [SerializeField] private GlimpTagNetworkTagger leftHandTagger;
    [SerializeField] private GlimpTagNetworkTagger rightHandTagger;
    [SerializeField] private Renderer leftHandSourceRenderer;
    [SerializeField] private Renderer rightHandSourceRenderer;
    [SerializeField] private GlimpTagXRHandInput handInput;
    [SerializeField] private GlimpTagXRCameraDriver cameraDriver;
    [SerializeField] private GlimpTagLocomotion locomotion;
    [SerializeField] private GlimpTagStartMenu startMenu;

    [Header("Remote avatar")]
    [SerializeField] private Transform remoteAvatarRoot;

    public void Configure(
        Rigidbody body,
        Camera camera,
        AudioListener audioListener,
        Transform space,
        Transform leftSource,
        Transform rightSource,
        Collider leftTrigger,
        Collider rightTrigger,
        GlimpTagNetworkTagger leftTagger,
        GlimpTagNetworkTagger rightTagger,
        Renderer leftSourceRenderer,
        Renderer rightSourceRenderer,
        GlimpTagXRHandInput xrHandInput,
        GlimpTagXRCameraDriver xrCameraDriver,
        GlimpTagLocomotion armLocomotion,
        GlimpTagStartMenu menu,
        Transform remoteVisualRoot)
    {
        playerBody = body;
        localCamera = camera;
        localAudioListener = audioListener;
        trackingSpace = space;
        leftHandSource = leftSource;
        rightHandSource = rightSource;
        leftHandTrigger = leftTrigger;
        rightHandTrigger = rightTrigger;
        leftHandTagger = leftTagger;
        rightHandTagger = rightTagger;
        leftHandSourceRenderer = leftSourceRenderer;
        rightHandSourceRenderer = rightSourceRenderer;
        handInput = xrHandInput;
        cameraDriver = xrCameraDriver;
        locomotion = armLocomotion;
        startMenu = menu;
        remoteAvatarRoot = remoteVisualRoot;

        if (handInput != null)
            handInput.Configure(trackingSpace, leftHandSource, rightHandSource);

        if (cameraDriver != null)
            cameraDriver.Configure(trackingSpace, localCamera != null ? localCamera.transform : null);

        if (locomotion != null)
        {
            locomotion.leftHand = leftHandSource;
            locomotion.rightHand = rightHandSource;
            locomotion.solidLayers = ~0;
        }

        if (startMenu != null)
            startMenu.locomotion = locomotion;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            ConfigureLocalOwner();
        else
            ConfigureRemoteReplica();
    }

    private void ConfigureLocalOwner()
    {
        PlaceAtForestSpawn();

        if (playerBody != null)
        {
            playerBody.isKinematic = false;
            playerBody.useGravity = true;
        }

        SetEnabled(localCamera, true);
        SetEnabled(localAudioListener, true);
        SetEnabled(handInput, true);
        SetEnabled(cameraDriver, true);
        SetEnabled(locomotion, true);
        SetEnabled(startMenu, true);

        SetEnabled(leftHandTrigger, true);
        SetEnabled(rightHandTrigger, true);
        SetEnabled(leftHandTagger, true);
        SetEnabled(rightHandTagger, true);
        SetEnabled(leftHandSourceRenderer, true);
        SetEnabled(rightHandSourceRenderer, true);

        if (remoteAvatarRoot != null)
            remoteAvatarRoot.gameObject.SetActive(false);
    }

    private void ConfigureRemoteReplica()
    {
        if (playerBody != null)
        {
            playerBody.isKinematic = true;
            playerBody.useGravity = false;
            playerBody.velocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        SetEnabled(localCamera, false);
        SetEnabled(localAudioListener, false);
        SetEnabled(handInput, false);
        SetEnabled(cameraDriver, false);
        SetEnabled(locomotion, false);
        SetEnabled(startMenu, false);

        SetEnabled(leftHandTrigger, false);
        SetEnabled(rightHandTrigger, false);
        SetEnabled(leftHandTagger, false);
        SetEnabled(rightHandTagger, false);
        SetEnabled(leftHandSourceRenderer, false);
        SetEnabled(rightHandSourceRenderer, false);

        if (remoteAvatarRoot != null)
            remoteAvatarRoot.gameObject.SetActive(true);
    }

    private void PlaceAtForestSpawn()
    {
        GlimpTagRuntimeBootstrap arena = FindObjectOfType<GlimpTagRuntimeBootstrap>();
        if (arena == null || arena.SpawnPoints == null || arena.SpawnPoints.Length == 0)
            return;

        int index = (int)(OwnerClientId % (ulong)arena.SpawnPoints.Length);
        Transform spawn = arena.SpawnPoints[index];
        if (spawn == null)
            return;

        if (playerBody != null)
        {
            playerBody.position = spawn.position;
            playerBody.rotation = spawn.rotation;
        }
        else
        {
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
        }
    }

    private static void SetEnabled(Behaviour component, bool enabled)
    {
        if (component != null)
            component.enabled = enabled;
    }

    private static void SetEnabled(Collider component, bool enabled)
    {
        if (component != null)
            component.enabled = enabled;
    }

    private static void SetEnabled(Renderer component, bool enabled)
    {
        if (component != null)
            component.enabled = enabled;
    }
}
