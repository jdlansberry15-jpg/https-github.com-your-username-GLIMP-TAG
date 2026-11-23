using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Compact pose snapshot for the remote head and hands.
/// The owner writes one snapshot at a throttled rate; all peers read it.
/// Positions and rotations are local to the player root.
/// </summary>
public struct GlimpTagAvatarPoseSnapshot : INetworkSerializable, IEquatable<GlimpTagAvatarPoseSnapshot>
{
    public bool IsValid;
    public bool LeftHandTracked;
    public bool RightHandTracked;
    public Vector3 HeadPosition;
    public Quaternion HeadRotation;
    public Vector3 LeftHandPosition;
    public Quaternion LeftHandRotation;
    public Vector3 RightHandPosition;
    public Quaternion RightHandRotation;

    public static GlimpTagAvatarPoseSnapshot Identity
    {
        get
        {
            return new GlimpTagAvatarPoseSnapshot
            {
                IsValid = false,
                LeftHandTracked = false,
                RightHandTracked = false,
                HeadRotation = Quaternion.identity,
                LeftHandRotation = Quaternion.identity,
                RightHandRotation = Quaternion.identity
            };
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref IsValid);
        serializer.SerializeValue(ref LeftHandTracked);
        serializer.SerializeValue(ref RightHandTracked);
        serializer.SerializeValue(ref HeadPosition);
        serializer.SerializeValue(ref HeadRotation);
        serializer.SerializeValue(ref LeftHandPosition);
        serializer.SerializeValue(ref LeftHandRotation);
        serializer.SerializeValue(ref RightHandPosition);
        serializer.SerializeValue(ref RightHandRotation);
    }

    public bool Equals(GlimpTagAvatarPoseSnapshot other)
    {
        return IsValid == other.IsValid &&
            LeftHandTracked == other.LeftHandTracked &&
            RightHandTracked == other.RightHandTracked &&
            HeadPosition.Equals(other.HeadPosition) &&
            HeadRotation.Equals(other.HeadRotation) &&
            LeftHandPosition.Equals(other.LeftHandPosition) &&
            LeftHandRotation.Equals(other.LeftHandRotation) &&
            RightHandPosition.Equals(other.RightHandPosition) &&
            RightHandRotation.Equals(other.RightHandRotation);
    }

    public override bool Equals(object obj)
    {
        return obj is GlimpTagAvatarPoseSnapshot other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = IsValid ? 1 : 0;
            hash = (hash * 31) + (LeftHandTracked ? 1 : 0);
            hash = (hash * 31) + (RightHandTracked ? 1 : 0);
            hash = (hash * 31) + HeadPosition.GetHashCode();
            hash = (hash * 31) + HeadRotation.GetHashCode();
            hash = (hash * 31) + LeftHandPosition.GetHashCode();
            hash = (hash * 31) + LeftHandRotation.GetHashCode();
            hash = (hash * 31) + RightHandPosition.GetHashCode();
            hash = (hash * 31) + RightHandRotation.GetHashCode();
            return hash;
        }
    }
}

/// <summary>
/// Owner-driven head/hand pose replication for a network player prefab.
/// Attach to the same root NetworkObject as GlimpTagNetworkPlayer.
/// </summary>
public sealed class GlimpTagNetworkAvatarPoseSync : NetworkBehaviour
{
    [Header("Owner tracking sources")]
    [SerializeField] private Transform headSource;
    [SerializeField] private Transform leftHandSource;
    [SerializeField] private Transform rightHandSource;

    [Header("Remote visual targets")]
    [SerializeField] private Transform remoteHead;
    [SerializeField] private Transform remoteLeftHand;
    [SerializeField] private Transform remoteRightHand;

    [Header("Network tuning")]
    [SerializeField, Range(5f, 30f)] private float sendRate = 20f;
    [SerializeField, Min(1f)] private float remoteSmoothing = 18f;

    public NetworkVariable<GlimpTagAvatarPoseSnapshot> AvatarPose =
        new NetworkVariable<GlimpTagAvatarPoseSnapshot>(
            GlimpTagAvatarPoseSnapshot.Identity,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner);

    private GlimpTagAvatarPoseSnapshot targetPose;
    private bool hasRemotePose;
    private float nextSendTime;

    public void Configure(
        Transform head,
        Transform leftHand,
        Transform rightHand,
        Transform remoteHeadTarget,
        Transform remoteLeftHandTarget,
        Transform remoteRightHandTarget)
    {
        headSource = head;
        leftHandSource = leftHand;
        rightHandSource = rightHand;
        remoteHead = remoteHeadTarget;
        remoteLeftHand = remoteLeftHandTarget;
        remoteRightHand = remoteRightHandTarget;
    }

    public override void OnNetworkSpawn()
    {
        AvatarPose.OnValueChanged += OnAvatarPoseChanged;

        if (IsOwner)
        {
            // Do not publish prefab-default hand positions before the first XR tracking update.
            AvatarPose.Value = CapturePose(false);
            nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
        }
        else
        {
            SetTargetActive(remoteLeftHand, false);
            SetTargetActive(remoteRightHand, false);
        }

        if (!IsOwner && AvatarPose.Value.IsValid)
        {
            targetPose = AvatarPose.Value;
            hasRemotePose = true;
            ApplyPoseImmediately(targetPose);
        }
    }

    public override void OnNetworkDespawn()
    {
        AvatarPose.OnValueChanged -= OnAvatarPoseChanged;
    }

    private void LateUpdate()
    {
        if (!IsSpawned)
            return;

        if (IsOwner)
        {
            if (Time.unscaledTime >= nextSendTime)
            {
                AvatarPose.Value = CapturePose(true);
                nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
            }
            return;
        }

        if (!hasRemotePose)
            return;

        float blend = 1f - Mathf.Exp(-remoteSmoothing * Time.unscaledDeltaTime);
        SmoothTransform(remoteHead, targetPose.HeadPosition, targetPose.HeadRotation, blend);

        SetTargetActive(remoteLeftHand, targetPose.LeftHandTracked);
        SetTargetActive(remoteRightHand, targetPose.RightHandTracked);
        if (targetPose.LeftHandTracked)
            SmoothTransform(remoteLeftHand, targetPose.LeftHandPosition, targetPose.LeftHandRotation, blend);
        if (targetPose.RightHandTracked)
            SmoothTransform(remoteRightHand, targetPose.RightHandPosition, targetPose.RightHandRotation, blend);
    }

    private GlimpTagAvatarPoseSnapshot CapturePose(bool includeHandTracking)
    {
        if (headSource == null)
            return GlimpTagAvatarPoseSnapshot.Identity;

        bool leftTracked = includeHandTracking && leftHandSource != null &&
            leftHandSource.gameObject.activeInHierarchy;
        bool rightTracked = includeHandTracking && rightHandSource != null &&
            rightHandSource.gameObject.activeInHierarchy;
        Quaternion inverseRootRotation = Quaternion.Inverse(transform.rotation);

        return new GlimpTagAvatarPoseSnapshot
        {
            IsValid = true,
            LeftHandTracked = leftTracked,
            RightHandTracked = rightTracked,
            HeadPosition = transform.InverseTransformPoint(headSource.position),
            HeadRotation = inverseRootRotation * headSource.rotation,
            LeftHandPosition = leftTracked
                ? transform.InverseTransformPoint(leftHandSource.position) : Vector3.zero,
            LeftHandRotation = leftTracked
                ? inverseRootRotation * leftHandSource.rotation : Quaternion.identity,
            RightHandPosition = rightTracked
                ? transform.InverseTransformPoint(rightHandSource.position) : Vector3.zero,
            RightHandRotation = rightTracked
                ? inverseRootRotation * rightHandSource.rotation : Quaternion.identity
        };
    }

    private void OnAvatarPoseChanged(
        GlimpTagAvatarPoseSnapshot previous,
        GlimpTagAvatarPoseSnapshot current)
    {
        if (IsOwner || !current.IsValid)
            return;

        targetPose = current;
        hasRemotePose = current.IsValid;
        SetTargetActive(remoteLeftHand, current.IsValid && current.LeftHandTracked);
        SetTargetActive(remoteRightHand, current.IsValid && current.RightHandTracked);
    }

    private void ApplyPoseImmediately(GlimpTagAvatarPoseSnapshot pose)
    {
        SetTransform(remoteHead, pose.HeadPosition, pose.HeadRotation);
        SetTargetActive(remoteLeftHand, pose.LeftHandTracked);
        SetTargetActive(remoteRightHand, pose.RightHandTracked);
        if (pose.LeftHandTracked)
            SetTransform(remoteLeftHand, pose.LeftHandPosition, pose.LeftHandRotation);
        if (pose.RightHandTracked)
            SetTransform(remoteRightHand, pose.RightHandPosition, pose.RightHandRotation);
    }

    private static void SmoothTransform(
        Transform target,
        Vector3 position,
        Quaternion rotation,
        float blend)
    {
        if (target == null)
            return;

        target.localPosition = Vector3.Lerp(target.localPosition, position, blend);
        target.localRotation = Quaternion.Slerp(target.localRotation, rotation, blend);
    }

    private static void SetTargetActive(Transform target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }

    private static void SetTransform(Transform target, Vector3 position, Quaternion rotation)
    {
        if (target == null)
            return;

        target.localPosition = position;
        target.localRotation = rotation;
    }
}
