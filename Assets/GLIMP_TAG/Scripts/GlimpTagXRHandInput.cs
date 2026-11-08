using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Feeds Quest controller or tracked-hand poses into dedicated hand target transforms.
/// Assign the XR Origin's tracking-space transform and two proxy targets.
/// The proxy targets can then be assigned to GlimpTagLocomotion.leftHand/rightHand.
/// </summary>
public sealed class GlimpTagXRHandInput : MonoBehaviour
{
    [Header("XR tracking space")]
    [Tooltip("Usually the XR Origin's Camera Offset / tracking-space transform.")]
    [SerializeField] private Transform trackingSpace;

    [Header("Dedicated hand proxy targets")]
    [Tooltip("A separate proxy transform, not the controller object itself.")]
    [SerializeField] private Transform leftHandTarget;
    [Tooltip("A separate proxy transform, not the controller object itself.")]
    [SerializeField] private Transform rightHandTarget;

    [Header("Device preference")]
    [Tooltip("Use tracked hands when available; otherwise fall back to Touch controllers.")]
    [SerializeField] private bool preferHandTracking = true;

    [Tooltip("Write device connection changes to the Unity Console.")]
    [SerializeField] private bool logDeviceChanges;

    private readonly List<InputDevice> deviceBuffer = new List<InputDevice>(8);
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private float nextDeviceScanTime;

    public bool LeftTracked { get; private set; }
    public bool RightTracked { get; private set; }

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        InputDevices.deviceConfigChanged += OnDeviceChanged;
        RefreshDevices();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
        InputDevices.deviceConfigChanged -= OnDeviceChanged;
        SetTargetActive(leftHandTarget, false);
        SetTargetActive(rightHandTarget, false);
        LeftTracked = false;
        RightTracked = false;
    }

    private void Update()
    {
        // Retry periodically while the XR runtime is starting or devices are changing.
        if ((!leftDevice.isValid || !rightDevice.isValid) &&
            Time.unscaledTime >= nextDeviceScanTime)
        {
            RefreshDevices();
        }

        LeftTracked = UpdateHandPose(leftDevice, leftHandTarget);
        RightTracked = UpdateHandPose(rightDevice, rightHandTarget);
    }

    private void OnDeviceChanged(InputDevice device)
    {
        if (logDeviceChanges)
            Debug.Log("GLIMP TAG XR device changed: " + device.name + " (" + device.characteristics + ")");

        RefreshDevices();
    }

    private void RefreshDevices()
    {
        leftDevice = FindDevice(InputDeviceCharacteristics.Left, XRNode.LeftHand);
        rightDevice = FindDevice(InputDeviceCharacteristics.Right, XRNode.RightHand);
        nextDeviceScanTime = Time.unscaledTime + 0.5f;
    }

    private InputDevice FindDevice(InputDeviceCharacteristics side, XRNode node)
    {
        InputDeviceCharacteristics preferred = preferHandTracking
            ? InputDeviceCharacteristics.HandTracking
            : InputDeviceCharacteristics.Controller;

        InputDeviceCharacteristics fallback = preferHandTracking
            ? InputDeviceCharacteristics.Controller
            : InputDeviceCharacteristics.HandTracking;

        deviceBuffer.Clear();
        InputDevices.GetDevicesWithCharacteristics(side | preferred, deviceBuffer);
        InputDevice selected = FirstValidDevice();
        if (selected.isValid) return selected;

        deviceBuffer.Clear();
        InputDevices.GetDevicesWithCharacteristics(side | fallback, deviceBuffer);
        selected = FirstValidDevice();
        if (selected.isValid) return selected;

        // Some XR providers expose a hand node without all expected characteristic flags.
        deviceBuffer.Clear();
        InputDevices.GetDevicesAtXRNode(node, deviceBuffer);
        return FirstValidDevice();
    }

    private InputDevice FirstValidDevice()
    {
        for (int i = 0; i < deviceBuffer.Count; i++)
        {
            if (deviceBuffer[i].isValid)
                return deviceBuffer[i];
        }

        return default;
    }

    private bool UpdateHandPose(InputDevice device, Transform target)
    {
        if (target == null || !device.isValid)
        {
            SetTargetActive(target, false);
            return false;
        }

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPosition) ||
            !device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRotation))
        {
            SetTargetActive(target, false);
            return false;
        }

        Vector3 worldPosition = trackingSpace != null
            ? trackingSpace.TransformPoint(localPosition)
            : localPosition;

        Quaternion worldRotation = trackingSpace != null
            ? trackingSpace.rotation * localRotation
            : localRotation;

        target.SetPositionAndRotation(worldPosition, worldRotation);
        SetTargetActive(target, true);
        return true;
    }

    private static void SetTargetActive(Transform target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }
}
