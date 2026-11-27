# GLIMP TAG tracked-hand pose validity

This increment fixes a multiplayer edge case in the XR hand/avatar pipeline.

## Previous behavior

When a controller or hand-tracking provider lost a hand, GlimpTagXRHandInput deactivated that hand's proxy GameObject. The avatar pose synchronizer still held a reference to the Transform and continued publishing its last world pose as if the hand were tracked.

That could cause two visible/gameplay problems:

- Remote players could see an untracked hand frozen at its last position.
- The host's hand-distance tag validator could use that stale hand pose as a valid tag/contact point.

## Changes

- GlimpTagAvatarPoseSnapshot now serializes independent LeftHandTracked and RightHandTracked flags.
- Pose equality and hashing include both flags, so a tracking-state change is a real network state change.
- The owner sends an initial head-only snapshot before the first XR update; it does not publish prefab-default hand positions.
- Subsequent snapshots mark a hand tracked only while its proxy is active in the hierarchy.
- Untracked hand positions are serialized as zero with identity rotation instead of reusing an old pose.
- Remote avatar hand visuals are hidden when their corresponding hand is untracked and shown again when tracking returns.
- Invalid avatar snapshots also hide both remote hands instead of leaving their last visible pose on screen.
- The server rejects a tag request if the requested attacking hand is not marked tracked in the attacker's latest owner-published pose.
- The server only considers the target's hands as contact points when those hands are currently marked tracked. The target torso and head remain valid target points.

## Network and security notes

The tracking flags are part of the owner-write NetworkVariable snapshot. They prevent accidental stale-pose tags during normal tracking loss, but they are not proof that a modified client is honest. A malicious client can still attempt to publish fabricated pose data. Production competitive play needs stronger server-side movement envelopes, rate limits, and abuse monitoring.

## Validation status

The repository preflight checks confirm that tracking flags are serialized, compared, hashed, used to hide/show remote hands, and checked by the server tag validator.

These are static source checks only. The changes have not been compiled in Unity or tested with a Quest headset. Real validation still needs controller disconnect/reconnect, hand-tracking loss/reacquisition, and two-player Relay tests.

## Server-side pose freshness

Each owner pose snapshot now includes a monotonically increasing sequence number. The sequence changes on every scheduled send, including while the avatar is stationary, so a stationary player still produces a fresh network update.

The server records its own local receipt time whenever the sequence changes. Tag validation requires the attacking player's pose to have arrived within the host-configured 0.75 second freshness window. A target's head/hand points are used only while that target snapshot is also fresh; otherwise the validator falls back to the target's synchronized root/torso position.

This avoids relying on client clocks and prevents a frozen, previously valid hand pose from remaining eligible indefinitely. The extra sequence field adds four bytes to the pose snapshot before transport overhead.
