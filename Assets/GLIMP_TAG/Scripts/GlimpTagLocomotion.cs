using UnityEngine;

/// <summary>
/// Hand-contact locomotion prototype for GLIMP TAG.
/// When a hand proxy touches a solid surface, moving the tracked hand moves
/// the player body in the opposite direction while keeping the hand planted.
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

    private Rigidbody body;
    private Vector3 previousLeft;
    private Vector3 previousRight;
    private bool hasHandHistory;

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
        hasHandHistory = false;
    }

    private void FixedUpdate()
    {
        if (leftHand == null || rightHand == null)
        {
            hasHandHistory = false;
            return;
        }

        if (!hasHandHistory)
        {
            previousLeft = leftHand.position;
            previousRight = rightHand.position;
            hasHandHistory = true;
            return;
        }

        ApplyHandContact(leftHand, ref previousLeft);
        ApplyHandContact(rightHand, ref previousRight);

        Vector3 velocity = body.velocity;
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            body.velocity = velocity.normalized * maxSpeed;

        if (bodyVelocityDamping > 0f)
            body.velocity *= 1f - Mathf.Clamp01(bodyVelocityDamping * Time.fixedDeltaTime);
    }

    private void ApplyHandContact(Transform hand, ref Vector3 previousPosition)
    {
        Vector3 currentPosition = hand.position;
        Vector3 movement = currentPosition - previousPosition;

        if (movement.sqrMagnitude < 0.000001f)
        {
            previousPosition = currentPosition;
            return;
        }

        // If the hand was already planted, move the player opposite the hand's motion.
        if (Physics.CheckSphere(previousPosition, handRadius, solidLayers,
            QueryTriggerInteraction.Ignore))
        {
            MovePlayer(previousPosition - currentPosition, ref previousPosition, currentPosition);
            return;
        }

        // First contact only corrects penetration. On the next step, the hand is planted.
        float distance = movement.magnitude;
        if (Physics.SphereCast(previousPosition, handRadius, movement / distance,
            out RaycastHit hit, distance, solidLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 safeContact = hit.point + hit.normal * handRadius;
            Vector3 correction = safeContact - currentPosition;
            MovePlayer(correction, ref previousPosition, currentPosition);
            return;
        }

        previousPosition = currentPosition;
    }

    private void MovePlayer(Vector3 correction, ref Vector3 previousPosition, Vector3 currentPosition)
    {
        if (correction.sqrMagnitude < 0.000001f)
        {
            previousPosition = currentPosition;
            return;
        }

        correction = Vector3.ClampMagnitude(correction, maxCorrectionPerStep);
        body.MovePosition(body.position + correction);

        // The hand proxy is parented to the player tracking space, so the body correction
        // moves the proxy by the same amount. Store that corrected world position as history.
        previousPosition = currentPosition + correction;
    }

    public void RecenterHandHistory()
    {
        hasHandHistory = false;
    }
}
