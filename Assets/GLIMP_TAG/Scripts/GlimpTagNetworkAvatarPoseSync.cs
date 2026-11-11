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
                HeadRotation = Quaternion.identity,
                LeftHandRotation = Quaternion.identity,
                RightHandRotation = Quaternion.identity
            };
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref IsValid);
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
            AvatarPose.Value = CapturePose();
            nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
        }
        else if (AvatarPose.Value.IsValid)
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
                AvatarPose.Value = CapturePose();
                nextSendTime = Time.unscaledTime + 1f / Mathf.Max(1f, sendRate);
            }
            return;
        }

        if (!hasRemotePose)
            return;

        float blend = 1f - Mathf.Exp(-remoteSmoothing * Time.unscaledDeltaTime);
        SmoothTransform(remoteHead, targetPose.HeadPosition, targetPose.HeadRotation, blend);
        SmoothTransform(remoteLeftHand, targetPose.LeftHandPosition, targetPose.LeftHandRotation, blend);
        SmoothTransform(remoteRightHand, targetPose.RightHandPosition, targetPose.RightHandRotation, blend);
    }

    private GlimpTagAvatarPoseSnapshot CapturePose()
    {
        if (headSource == null || leftHandSource == null || rightHandSource == null)
            return GlimpTagAvatarPoseSnapshot.Identity;

        Quaternion inverseRootRotation = Quaternion.Inverse(transform.rotation);

        return new GlimpTagAvatarPoseSnapshot
        {
            IsValid = true,
            HeadPosition = transform.InverseTransformPoint(headSource.position),
            HeadRotation = inverseRootRotation * headSource.rotation,
            LeftHandPosition = transform.InverseTransformPoint(leftHandSource.position),
            LeftHandRotation = inverseRootRotation * leftHandSource.rotation,
            RightHandPosition = transform.InverseTransformPoint(rightHandSource.position),
            RightHandRotation = inverseRootRotation * rightHandSource.rotation
        };
    }

    private void OnAvatarPoseChanged(
        GlimpTagAvatarPoseSnapshot previous,
        GlimpTagAvatarPoseSnapshot current)
    {
        if (IsOwner || !current.IsValid)
            return;

        targetPose = current;
        hasRemotePose = true;
    }

    private void ApplyPoseImmediately(GlimpTagAvatarPoseSnapshot pose)
    {
        SetTransform(remoteHead, pose.HeadPosition, pose.HeadRotation);
        SetTransform(remoteLeftHand, pose.LeftHandPosition, pose.LeftHandRotation);
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

    private static void SetTransform(Transform target, Vector3 position, Quaternion rotation)
    {
        if (target == null)
            return;

        target.localPosition = position;
        target.localRotation = rotation;
    }
}
