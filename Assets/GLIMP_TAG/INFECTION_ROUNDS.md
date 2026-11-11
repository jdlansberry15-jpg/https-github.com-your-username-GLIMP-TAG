# Network infection rounds

The server-host player object coordinates the infection round.

## Round behavior
- Waits until at least two network player objects are connected.
- Picks one random player as the first infected.
- Infected players can request tags through their local hand trigger colliders.
- The server validates the caller's ownership, infection state, target, and approximate player-root distance.
- Synchronizes infection state, round number, player count, infected count, and timer.
- Ends when everyone is infected or the timer expires, pauses briefly, then starts another round.
- Infected avatars change to a bright orange-red tint.

## Automatic prefab setup
The Editor builder creates a network player prefab containing the root NetworkObject, owner-authoritative NetworkTransform, head/hand pose sync, local hand trigger colliders, remote hand colliders, and the XR rig binder. The prefab is registered as the NetworkManager's PlayerPrefab.

## Known limitations
- This implementation uses the host's PlayerObject as the shared round coordinator; dedicated-server mode needs a separate scene NetworkObject coordinator.
- Owner-authoritative transforms trust each player's client. The root-distance check is only a coarse tag sanity check, not anti-cheat.
- Hand contact depends on the generated hand colliders and remote avatar pose synchronization.
- The HUD is a temporary screen-space debug panel, not the final VR wrist menu.
- No Unity compile, two-device multiplayer simulation, APK build, or Quest device test has been run.
