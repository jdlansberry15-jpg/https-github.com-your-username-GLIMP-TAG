# GLIMP TAG server-side tag validation

## What changed

A local hand trigger still reports contact intent, but it no longer sends an arbitrary world-space hand position to the host. Each hand reports only whether it is the left or right hand.

The host reads the attacker's most recent owner-published avatar pose snapshot and validates:

1. The attacker and target are different connected players.
2. The attacker is currently infected and the target is not.
3. Their player roots are within the existing 3 m outer envelope.
4. The attacker's latest owner-published pose was received by the host within the configured 0.75 second freshness window.
5. The selected attacking hand is currently tracked and within 2.4 m of the attacker's torso reference point.
6. The attacking hand is within 1.35 m of the target torso, or of the target's head/hand pose only when that target snapshot is also fresh.

The last two distances are server-side checks against replicated data. A client cannot pass a hand world position directly through the tag RPC.

## Why the tolerance is not the collider radius

Avatar poses are sent at 20 Hz while the root transform is synchronized separately. A real hand contact can therefore be observed using slightly different snapshots on the host. The 1.35 m target envelope is deliberately more forgiving than the physical hand sphere so normal latency does not reject every contact. It is still much tighter than allowing any tag within the 3 m root range.

## Remaining security and test limits

- Movement and avatar pose are still owner-authoritative. A modified client can falsify its own hand pose within the configured reach envelope.
- This is not server-authoritative physics, rewind/lag compensation, or a production anti-cheat system.
- The distance thresholds need tuning with real two-headset latency and play tests.
- The server now timestamps each sequenced pose update on receipt and rejects attacker snapshots older than 0.75 seconds. Target head/hand points are ignored when their snapshot is stale; the synchronized target root remains the fallback.
- The 20 Hz pose snapshot can still be delayed by packet loss or very high latency; the freshness threshold needs two-headset tuning.
- The change has not been Unity-compiled or Quest-tested. GitHub preflight only checks that the expected source paths and wiring are present.

For a public competitive release, move player movement and collision simulation to trusted server authority or add server-side movement plausibility checks and lag-compensated contact validation.
