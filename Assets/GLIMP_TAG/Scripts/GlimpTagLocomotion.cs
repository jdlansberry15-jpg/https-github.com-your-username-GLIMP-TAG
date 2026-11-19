using UnityEngine;

/// <summary>
/// Hand-contact locomotion for GLIMP TAG.
/// Tracked hands push against world geometry to move the player body in the
/// opposite direction. Lost/reacquired tracking never reuses stale hand history.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class GlimpTagLocomotion : MonoBehaviour
{
    [Header("Tracked hand proxies")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Contact movement")]
    [Min(0.03f)] public float handRadius = 0.12f;
    [Min(0.05f)] public float maxCorrectionPerStep = 0.45f;
    [Min(0.5f)] public float maxSpeed = 8f;
    public LayerMask solidLayers = ~0;

    [Header("Comfort / safety")]
    [Min(0f)] public float bodyVelocityDamping = 0.03f;
    public bool freezePlayerRotation = true;

    private readonly Collider[] overlapBuffer = new Collider[24];
    private readonly RaycastHit[] castBuffer = new RaycastHit[24];

    private Rigidbody body;
    private Vector3 previousLeft;
    private Vector3 previousRight;
    private bool hasLeftHistory;
    private bool hasRightHistory;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        if (freezePlayerRotation)
            body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void OnEnable()
    {
        RecenterHandHistory();
    }

    private void FixedUpdate()
    {
        bool leftTracked = IsHandTracked(leftHand);
        bool rightTracked = IsHandTracked(rightHand);

        // A newly reacquired hand only seeds its history. It cannot create a
        // large correction from a stale pose recorded before tracking was lost.
        if (!leftTracked)
        {
            hasLeftHistory = false;
        }
        else if (!hasLeftHistory)
        {
            previousLeft = leftHand.position;
            hasLeftHistory = true;
        }

        if (!rightTracked)
        {
            hasRightHistory = false;
        }
        else if (!hasRightHistory)
        {
            previousRight = rightHand.position;
            hasRightHistory = true;
        }

        if (!leftTracked && !rightTracked)
        {
            ApplyVelocitySafety();
            return;
        }

        Vector3 leftNow = leftTracked ? leftHand.position : Vector3.zero;
        Vector3 rightNow = rightTracked ? rightHand.position : Vector3.zero;

        Vector3 leftCorrection = leftTracked
            ? GetHandCorrection(previousLeft, leftNow)
            : Vector3.zero;

        // Solve the second hand in the space created by the first correction,
        // so a single body movement is not counted twice.
        Vector3 rightCorrection = Vector3.zero;
        if (rightTracked)
        {
            Vector3 rightAfterLeft = rightNow + leftCorrection;
            Vector3 previousRightAfterLeft = previousRight + leftCorrection;
            rightCorrection = GetHandCorrection(
                previousRightAfterLeft, rightAfterLeft);
        }

        Vector3 appliedCorrection = Vector3.ClampMagnitude(
            leftCorrection + rightCorrection, maxCorrectionPerStep);

        if (appliedCorrection.sqrMagnitude > 0.000001f)
            body.MovePosition(body.position + appliedCorrection);

        // Hand targets are children of the tracking space, so the player's
        // root correction moves their world positions by the same amount.
        if (leftTracked)
            previousLeft = leftNow + appliedCorrection;

        if (rightTracked)
            previousRight = rightNow + appliedCorrection;

        ApplyVelocitySafety();
    }

    private Vector3 GetHandCorrection(Vector3 previousPosition, Vector3 currentPosition)
    {
        Vector3 movement = currentPosition - previousPosition;
        float distance = movement.magnitude;
        if (distance < 0.001f)
            return Vector3.zero;

        // While the palm proxy is touching world geometry, keep it planted.
        if (TouchesWorld(currentPosition))
        {
            return Vector3.ClampMagnitude(
                previousPosition - currentPosition, maxCorrectionPerStep);
        }

        // On first contact, sweep the hand volume along its movement path.
        // Non-alloc queries keep the fixed-step path lightweight on Quest.
        int hitCount = Physics.SphereCastNonAlloc(
            previousPosition,
            handRadius,
            movement / distance,
            castBuffer,
            distance,
            solidLayers,
            QueryTriggerInteraction.Ignore);

        int nearestHitIndex = -1;
        float nearestDistance = float.PositiveInfinity;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = castBuffer[i];
            if (hit.collider == null || IsOwnCollider(hit.collider))
                continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearestHitIndex = i;
            }
        }

        if (nearestHitIndex < 0)
            return Vector3.zero;

        RaycastHit nearestHit = castBuffer[nearestHitIndex];
        Vector3 safeContact = nearestHit.point +
            nearestHit.normal * (handRadius * 0.98f);

        return Vector3.ClampMagnitude(
            safeContact - currentPosition, maxCorrectionPerStep);
    }

    private bool TouchesWorld(Vector3 position)
    {
        int count = Physics.OverlapSphereNonAlloc(
            position,
            handRadius * 1.05f,
            overlapBuffer,
            solidLayers,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider candidate = overlapBuffer[i];
            if (candidate != null && !IsOwnCollider(candidate))
                return true;
        }

        return false;
    }

    private bool IsOwnCollider(Collider candidate)
    {
        if (candidate == null)
            return false;

        if (candidate.attachedRigidbody == body)
            return true;

        Transform candidateTransform = candidate.transform;
        return candidateTransform == transform ||
            candidateTransform.IsChildOf(transform);
    }

    private static bool IsHandTracked(Transform hand)
    {
        return hand != null && hand.gameObject.activeInHierarchy;
    }

    private void ApplyVelocitySafety()
    {
        Vector3 velocity = body.velocity;
        float speedLimit = Mathf.Max(0.5f, maxSpeed);

        if (velocity.sqrMagnitude > speedLimit * speedLimit)
            body.velocity = velocity.normalized * speedLimit;

        if (bodyVelocityDamping > 0f)
        {
            float damping = Mathf.Clamp01(
                bodyVelocityDamping * Time.fixedDeltaTime);
            body.velocity *= 1f - damping;
        }
    }

    /// <summary>
    /// Call after a tracking-origin recenter or XR rig reconfiguration.
    /// </summary>
    public void RecenterHandHistory()
    {
        hasLeftHistory = false;
        hasRightHistory = false;
    }
}
