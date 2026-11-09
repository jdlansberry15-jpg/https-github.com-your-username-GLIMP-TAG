using UnityEngine;

/// <summary>
/// Hand-contact locomotion prototype for GLIMP TAG.
/// When a hand proxy touches a solid surface, moving that tracked hand moves
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

        Vector3 leftNow = leftHand.position;
        Vector3 rightNow = rightHand.position;

        // Solve the left hand first, then account for that same body movement
        // when testing the right hand. This avoids a false second push.
        Vector3 leftCorrection = GetHandCorrection(previousLeft, leftNow);
        Vector3 rightAfterLeft = rightNow + leftCorrection;
        Vector3 previousRightAfterLeft = previousRight + leftCorrection;
        Vector3 rightCorrection = GetHandCorrection(previousRightAfterLeft, rightAfterLeft);

        Vector3 appliedCorrection = Vector3.ClampMagnitude(
            leftCorrection + rightCorrection, maxCorrectionPerStep);

        if (appliedCorrection.sqrMagnitude > 0.000001f)
            body.MovePosition(body.position + appliedCorrection);

        // Both hand proxies are children of the tracking space, so both shift
        // with the player body. Keep both history positions in that new space.
        previousLeft = leftNow + appliedCorrection;
        previousRight = rightNow + appliedCorrection;

        Vector3 velocity = body.velocity;
        if (velocity.sqrMagnitude > maxSpeed * maxSpeed)
            body.velocity = velocity.normalized * maxSpeed;

        if (bodyVelocityDamping > 0f)
            body.velocity *= 1f - Mathf.Clamp01(bodyVelocityDamping * Time.fixedDeltaTime);
    }

    private Vector3 GetHandCorrection(Vector3 previousPosition, Vector3 currentPosition)
    {
        Vector3 movement = currentPosition - previousPosition;
        if (movement.sqrMagnitude < 0.000001f)
            return Vector3.zero;

        // Only keep a hand planted while its current pose is still in contact.
        if (Physics.CheckSphere(currentPosition, handRadius * 1.05f,
            solidLayers, QueryTriggerInteraction.Ignore))
            return Vector3.ClampMagnitude(previousPosition - currentPosition,
                maxCorrectionPerStep);

        // First contact corrects the proxy to the surface without teleporting the player.
        float distance = movement.magnitude;
        if (Physics.SphereCast(previousPosition, handRadius, movement / distance,
            out RaycastHit hit, distance, solidLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 safeContact = hit.point + hit.normal * (handRadius * 0.98f);
            return Vector3.ClampMagnitude(safeContact - currentPosition,
                maxCorrectionPerStep);
        }

        return Vector3.zero;
    }

    public void RecenterHandHistory()
    {
        hasHandHistory = false;
    }
}
