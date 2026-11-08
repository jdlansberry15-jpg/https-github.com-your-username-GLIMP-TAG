using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;

/// <summary>
/// Drives dedicated hand proxy transforms from XR Hands when available,
/// falling back to Quest Touch controller poses.
/// The proxy transforms should be children of the player's tracking-space object.
/// </summary>
public sealed class GlimpTagXRHandInput : MonoBehaviour
{
    [Header("Tracking space")]
    [SerializeField] private Transform trackingSpace;

    [Header("Hand proxy targets")]
    [SerializeField] private Transform leftHandTarget;
    [SerializeField] private Transform rightHandTarget;

    [Header("Input preference")]
    [SerializeField] private bool preferHandTracking = true;
    [SerializeField] private bool logDeviceChanges;

    private readonly List<InputDevice> deviceBuffer = new List<InputDevice>(8);
    private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>(2);

    private InputDevice leftController;
    private InputDevice rightController;
    private XRHandSubsystem handSubsystem;
    private float nextDeviceScanTime;
    private float nextHandSubsystemScanTime;

    public bool LeftTracked { get; private set; }
    public bool RightTracked { get; private set; }

    public void Configure(Transform space, Transform leftTarget, Transform rightTarget)
    {
        trackingSpace = space;
        leftHandTarget = leftTarget;
        rightHandTarget = rightTarget;
        RefreshDevices();
        RefreshHandSubsystem();
    }

    private void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        InputDevices.deviceConfigChanged += OnDeviceChanged;
        RefreshDevices();
        RefreshHandSubsystem();
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
        if ((!leftController.isValid || !rightController.isValid) &&
            Time.unscaledTime >= nextDeviceScanTime)
            RefreshDevices();

        if ((handSubsystem == null || !handSubsystem.running) &&
            Time.unscaledTime >= nextHandSubsystemScanTime)
            RefreshHandSubsystem();

        LeftTracked = UpdateHand(Handedness.Left, leftController, leftHandTarget);
        RightTracked = UpdateHand(Handedness.Right, rightController, rightHandTarget);
    }

    private void OnDeviceChanged(InputDevice device)
    {
        if (logDeviceChanges)
            Debug.Log("GLIMP TAG XR device changed: " + device.name + " (" + device.characteristics + ")");

        RefreshDevices();
    }

    private void RefreshDevices()
    {
        leftController = FindController(InputDeviceCharacteristics.Left, XRNode.LeftHand);
        rightController = FindController(InputDeviceCharacteristics.Right, XRNode.RightHand);
        nextDeviceScanTime = Time.unscaledTime + 0.5f;
    }

    private InputDevice FindController(InputDeviceCharacteristics side, XRNode node)
    {
        deviceBuffer.Clear();
        InputDevices.GetDevicesWithCharacteristics(
            side | InputDeviceCharacteristics.Controller, deviceBuffer);

        InputDevice found = FirstValidDevice();
        if (found.isValid) return found;

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

    private void RefreshHandSubsystem()
    {
        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);

        handSubsystem = null;
        for (int i = 0; i < handSubsystems.Count; i++)
        {
            if (handSubsystems[i] != null && handSubsystems[i].running)
            {
                handSubsystem = handSubsystems[i];
                break;
            }
        }

        nextHandSubsystemScanTime = Time.unscaledTime + 1f;
    }

    private bool UpdateHand(Handedness handedness, InputDevice controller, Transform target)
    {
        if (target == null)
            return false;

        if (preferHandTracking && TryGetTrackedHandPose(handedness, out Pose handPose))
        {
            ApplyPose(target, handPose);
            return true;
        }

        if (TryGetControllerPose(controller, out Pose controllerPose))
        {
            ApplyPose(target, controllerPose);
            return true;
        }

        SetTargetActive(target, false);
        return false;
    }

    private bool TryGetTrackedHandPose(Handedness handedness, out Pose pose)
    {
        pose = default;
        if (handSubsystem == null || !handSubsystem.running)
            return false;

        XRHand hand = handedness == Handedness.Left
            ? handSubsystem.leftHand
            : handSubsystem.rightHand;

        if (!hand.isTracked)
            return false;

        XRHandJoint palm = hand.GetJoint(XRHandJointID.Palm);
        if (palm.TryGetPose(out pose))
            return true;

        // Wrist root pose is a safe fallback if the provider does not expose the palm joint.
        pose = hand.rootPose;
        return true;
    }

    private static bool TryGetControllerPose(InputDevice device, out Pose pose)
    {
        pose = default;
        if (!device.isValid)
            return false;

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 position) ||
            !device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rotation))
            return false;

        pose = new Pose(position, rotation);
        return true;
    }

    private void ApplyPose(Transform target, Pose localPose)
    {
        if (trackingSpace != null)
        {
            target.SetPositionAndRotation(
                trackingSpace.TransformPoint(localPose.position),
                trackingSpace.rotation * localPose.rotation);
        }
        else
        {
            target.SetPositionAndRotation(localPose.position, localPose.rotation);
        }

        SetTargetActive(target, true);
    }

    private static void SetTargetActive(Transform target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }
}
