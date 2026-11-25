# Server-assigned forest spawn slots

GLIMP TAG has eight forest spawn points for the Relay room capacity of one host plus seven guests.

## Assignment

- The server assigns each newly spawned player the first unoccupied slot.
- The selected integer is stored in the player's server-write/everyone-read `SpawnSlot` NetworkVariable.
- The allocator checks other connected player objects and ignores the player currently being assigned.
- When a player disconnects, their player object leaves the connected-player list; that slot can be reused by the next joiner.
- The local owner rig reads the replicated slot before moving to the forest. A slot update also triggers an owner-side placement, covering component spawn-order differences.
- Round resets and fall recovery use the same assigned slot. They no longer calculate a position from `OwnerClientId % spawnCount`, which could collide after reconnects.

## Capacity behavior

The arena has eight slots. If no free slot exists, the server logs an error and leaves the slot unassigned instead of silently placing two players on the same spawn. Relay room capacity must remain capped at eight total players.

## Validation

The Editor build validator checks that the generated NetworkPlayer prefab has the SpawnSlot variable. The GitHub static preflight checks the allocator, replication callback, owner-rig usage, and this document.

These checks do not prove NGO runtime ordering, Unity compilation, Relay reconnect behavior, or Quest hardware behavior. This change has not been Unity-compiled or Quest-tested in this environment.
