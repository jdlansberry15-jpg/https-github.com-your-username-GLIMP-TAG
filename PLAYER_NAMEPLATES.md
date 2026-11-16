# GLIMP TAG remote player nameplates

This increment adds server-synchronized display names and world-space nameplates for remote avatars.

## Behavior

1. A player saves a name in the existing roster overlay.
2. When their network player spawns, the owner submits that name through an owner-authorized RPC.
3. The server sanitizes it and publishes a `FixedString64Bytes` NetworkVariable.
4. The generated remote avatar nameplate reads the name and current SAFE / INFECTED state.
5. If the matching Vivox participant is speaking, the nameplate displays SPEAKING in cyan.

The TextMesh is attached under Remote Avatar Visuals, which is disabled for the local owner. It therefore appears only above remote avatars.

## Build integration

`GlimpTagNetworkPrefabBuilder` creates the TextMesh and attaches `GlimpTagNetworkNameplate`. `GlimpTagBuildValidation` checks that both the component and child TextMesh exist.

No manual scene or prefab editing is required when the cloud/editor project builder is run.

## Validation status

The changes are committed as source only. The FixedString NetworkVariable approach follows NGO 2.13 documentation. Unity compilation, generated prefab inspection in Unity, Relay/Vivox multiplayer testing, APK generation, and Quest 2/3/3S device tests remain outstanding.
