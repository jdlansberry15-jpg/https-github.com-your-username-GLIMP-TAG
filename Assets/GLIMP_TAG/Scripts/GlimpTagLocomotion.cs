using UnityEngine;

/// <summary>
/// Simple physics-driven hand-push locomotion prototype.
/// Assign tracked left/right hand transforms and a Rigidbody on the player.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GlimpTagLocomotion : MonoBehaviour
{
    [Header("Tracked hands")]
    public Transform leftHand;
    public Transform rightHand;

    [Header("Movement")]
    [Min(0.01f)] public float handRadius = 0.12f;
    [Min(0f)] public float pushMultiplier = 1.15f;
    [Min(0f)] public float maxSpeed = 7.5f;
    [Min(0f)] public float velocityDamping = 0.15f;
    public LayerMask solidLayers = ~0;

    private Rigidbody body;
    private Vector3 lastLeftPosition;
    private Vector3 lastRightPosition;
    private bool hasPreviousPositions;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = true;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (leftHand == null || rightHand == null) return;

        if (!hasPreviousPositions)
        {
            lastLeftPosition = leftHand.position;
            lastRightPosition = rightHand.position;
            hasPreviousPositions = true;
            return;
        }

        PushFromHand(leftHand, ref lastLeftPosition);
        PushFromHand(rightHand, ref lastRightPosition);

        Vector3 velocity = body.velocity;
        if (velocity.magnitude > maxSpeed)
            body.velocity = velocity.normalized * maxSpeed;

        if (velocityDamping > 0f)
            body.velocity *= 1f - Mathf.Clamp01(velocityDamping * Time.fixedDeltaTime);
    }

    private void PushFromHand(Transform hand, ref Vector3 previousPosition)
    {
        Vector3 currentPosition = hand.position;
        Vector3 handDelta = currentPosition - previousPosition;

        if (Physics.SphereCast(
            previousPosition,
            handRadius,
            handDelta.sqrMagnitude > 0.000001f ? handDelta.normalized : Vector3.down,
            out RaycastHit hit,
            handDelta.magnitude + handRadius,
            solidLayers,
            QueryTriggerInteraction.Ignore))
        {
            Vector3 push = previousPosition - currentPosition;
            push = Vector3.ProjectOnPlane(push, hit.normal);
            body.AddForce(push * pushMultiplier / Mathf.Max(Time.fixedDeltaTime, 0.001f), ForceMode.Acceleration);
        }

        previousPosition = currentPosition;
    }

    public void RecenterHandHistory()
    {
        hasPreviousPositions = false;
    }
}
