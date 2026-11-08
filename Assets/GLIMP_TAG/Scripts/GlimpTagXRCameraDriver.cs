using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Applies the XR center-eye pose to the camera relative to the player's tracking space.
/// This is a lightweight alternative to a manually assembled XR Origin prefab.
/// </summary>
public sealed class GlimpTagXRCameraDriver : MonoBehaviour
{
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private Transform headCamera;

    private readonly List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>(2);
    private InputDevice headDevice;
    private float nextTrackingModeRequest;

    public void Configure(Transform space, Transform cameraTransform)
    {
        trackingSpace = space;
        headCamera = cameraTransform;
        RequestFloorTrackingMode();
        UpdateHeadPose();
    }

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        Application.onBeforeRender += UpdateHeadPose;
        RequestFloorTrackingMode();
        UpdateHeadPose();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
        Application.onBeforeRender -= UpdateHeadPose;
    }

    private void Update()
    {
        if (!headDevice.isValid)
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (Time.unscaledTime >= nextTrackingModeRequest)
            RequestFloorTrackingMode();

        UpdateHeadPose();
    }

    private void OnDeviceChanged(InputDevice device)
    {
        if ((device.characteristics & InputDeviceCharacteristics.HeadMounted) != 0)
            headDevice = device;
    }

    private void RequestFloorTrackingMode()
    {
        inputSubsystems.Clear();
        SubsystemManager.GetSubsystems(inputSubsystems);

        for (int i = 0; i < inputSubsystems.Count; i++)
        {
            XRInputSubsystem subsystem = inputSubsystems[i];
            if (subsystem != null && subsystem.running)
                subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }

        nextTrackingModeRequest = Time.unscaledTime + 2f;
    }

    private void UpdateHeadPose()
    {
        if (headCamera == null)
            return;

        if (!headDevice.isValid)
            headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);

        if (!headDevice.isValid ||
            !headDevice.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 position) ||
            !headDevice.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation))
            return;

        if (trackingSpace != null)
        {
            headCamera.localPosition = position;
            headCamera.localRotation = rotation;
        }
        else
        {
            headCamera.SetPositionAndRotation(position, rotation);
        }
    }
}
