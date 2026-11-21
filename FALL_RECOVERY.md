# GLIMP TAG fall recovery

## What it does

The owning headset watches its own network player root. If the player falls below the arena safety height, that same owner returns to the player's assigned spawn point.

This is a recovery path for missed jumps and accidental falls. It does not replace the server-directed round reset.

## Current defaults

| Setting | Default |
|---|---:|
| Fall reset height | -8 metres |
| Recovery cooldown | 1.25 seconds |
| Spawn selection | OwnerClientId modulo the eight generated spawn slots |

The recovery runs only when the player NetworkObject is spawned and the local client owns it. This is required because the player root uses owner-authoritative NetworkTransform.

## Recovery sequence

1. Check the local player's root height.
2. If the root is below -8 m and the 1.25 s cooldown has elapsed, call the existing owner reset method.
3. Move the Rigidbody root to its assigned forest spawn.
4. Clear linear and angular velocity.
5. Recenter hand locomotion history so the fall/teleport cannot become a giant hand-swing correction.
6. The owner-authoritative NetworkTransform publishes the recovered root position.

## Validation status

The repository preflight checks verify that the owner-only height check, cooldown, and reuse of the existing reset method remain present.

This has **not been Unity-compiled or Quest-tested**. The threshold and cooldown are starting values. A real Quest 2, Quest 3, and Quest 3S test should deliberately fall outside the arena, verify the player returns upright, and confirm other clients see the recovered position without a long interpolation or snap-back.
