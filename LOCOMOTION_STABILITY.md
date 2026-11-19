# GLIMP TAG locomotion stability notes

This increment hardens the hand-contact locomotion prototype for Quest controllers and hand tracking.

## Changes

- Each hand has independent tracking history.
- When a hand proxy becomes inactive, its history is discarded.
- When tracking returns, the first physics step only records a fresh pose; it does not move the player.
- World-contact checks use non-allocating physics queries.
- The player's own rigidbody and child colliders are ignored by hand-contact queries, preventing the player capsule or hand proxy from acting as a climbing surface.
- The two-hand correction is solved as one bounded body movement.
- The existing maximum speed and damping safety limits remain in place.
- `RecenterHandHistory()` can be called after a tracking-origin recenter or rig reconfiguration.

## Current prototype tuning

| Setting | Default |
|---|---:|
| Hand collision radius | 0.12 m |
| Configured correction ceiling | 0.45 m |
| Actual correction per step | min(0.45 m, 8 m/s × fixedDeltaTime) |
| Maximum body speed | 8 m/s |
| Body velocity damping | 0.03 |
| Physics query buffers | 24 hits |

The 0.45 m value is only a hard ceiling. The actual body correction is also capped by the 8 m/s speed budget multiplied by the current physics step (about 0.089 m at 90 Hz). These are conservative starting values, not measured Quest performance results. The game still needs on-device comfort tuning.

## How tracking is handled

`GlimpTagXRHandInput` activates each hand proxy only while a tracked hand or controller pose is available. `GlimpTagLocomotion` uses that active state as the tracked/untracked signal. This avoids a stale transform being interpreted as a large hand swing after tracking loss.

## Validation status

The repository preflight checks verify that the safety code paths remain present. They do not execute Unity physics. A Unity Editor compile and real Quest 2/3/3S play test are still required before treating locomotion as device-verified.

## Recommended headset test

1. Start with controllers and move one hand against a broad tree trunk.
2. Release the surface and let the hand move freely; the body should not be pulled.
3. Turn on hand tracking, place a palm against a wall, and push.
4. Briefly lose/reacquire hand tracking; the player should not snap from the stale pose.
5. Repeat while near the player's own body capsule to ensure it is not treated as climbable world geometry.
6. Check comfort at the default 8 m/s cap before raising it.
