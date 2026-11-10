# Network infection rounds

This increment adds a shared server-authoritative infection round loop to the existing NGO player scripts.

## What it does
- Waits until at least two network player objects are connected.
- Picks one random player as the first infected.
- Lets an infected player's hand trigger request a tag.
- Checks on the server that the caller owns the player object, is infected, the target is not infected, and the two player roots are within the configured tag distance.
- Synchronizes infection state, round number, player count, infected count, and round timer.
- Ends when everyone is infected or the timer expires, pauses briefly, then starts another round.
- Shows a basic screen-space round HUD.

## Required network setup
1. Every connected player must have a PlayerObject with a root `NetworkObject` and `GlimpTagNetworkPlayer`.
2. Register that player prefab in the NetworkManager's Player Prefab field or use the project's chosen server-side player-spawn flow.
3. Add a `NetworkTransform` (or another server-authoritative transform solution) to the player root so the server has current player positions.
4. Put a trigger collider and `GlimpTagNetworkTagger` on each hand beneath the player root NetworkObject. The trigger must be enabled on the owning player's client.
5. Add `GlimpTagNetworkRoundHud` to an active scene object for the temporary round display.
6. Configure the Unity Transport connection before testing with two devices or two app instances.

## Important limitations
- This is a gameplay code increment, not a complete matchmaking or relay setup.
- The server distance check uses avatar root positions as a coarse validation. Accurate hand contact still depends on correctly synchronized player rigs and hand colliders.
- The current runtime bootstrap creates a local XR test rig; it does not yet create/register a production network player prefab. That prefab/rig integration is still required before these round scripts can run in a real multiplayer session.
- No Unity compilation, multiplayer simulation, APK build, or Quest device test has been run in this environment.
- The HUD is a temporary screen-space debug panel, not the final VR wrist menu.
