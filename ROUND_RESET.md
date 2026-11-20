# GLIMP TAG round reset behavior

At the beginning of every infection round, the host now requests a spawn reset for every connected player.

## Flow

1. The host collects the currently connected, spawned player objects.
2. The host clears each player's infection state.
3. The host selects the first infected player and updates the shared round variables.
4. The host invokes `ResetOwnerForRoundRpc` on each player object using NGO's `SendTo.Owner` target.
5. Only that player's owning client runs `GlimpTagNetworkPlayerRig.ResetOwnerToSpawn()`.
6. The owner returns to the same deterministic spawn slot assigned from their client ID, clears rigidbody linear/angular velocity, and calls `GlimpTagLocomotion.RecenterHandHistory()`.
7. The owner-authoritative NetworkTransform then publishes the new root position to the other peers.

## Why the owner moves itself

The player root uses `GlimpTagOwnerNetworkTransform`, which is owner-authoritative. Moving a remote client's root directly from the host would fight the transform authority. The server therefore controls *when* a reset happens, while the owner applies the actual local teleport.

## Spawn selection

The current arena has eight ground-level spawn points. The slot is deterministic:

`spawnIndex = OwnerClientId % SpawnPoints.Length` (eight slots for the host plus up to seven Relay guests).

This keeps each player's reset location stable between rounds and avoids duplicate slots for the full eight-player Relay room.

## Safety notes

- Resets occur when a new round starts, not on every tag.
- The host does not accept a client request to reset another player.
- The local rigidbody velocity is cleared before movement resumes.
- Hand movement history is invalidated so a teleport cannot be interpreted as a giant hand swing.
- This is not a public-match anti-cheat system; owner-authoritative player movement still needs server-side movement validation before competitive deployment.

## Validation status

The GitHub preflight checks the expected RPC and owner-side reset code paths, and verifies that eight spawn slots match Relay's host-plus-seven-guest capacity. The change has **not been Unity-compiled or Quest-tested** in this environment. A two-headset Relay test is still required to verify the RPC, transform replication, and spawn spacing in a real session.
