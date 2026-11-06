# GLIMP TAG setup notes

## Target
- Unity project configured for Android
- Meta Quest 2 / Quest 3 / Quest 3S
- OpenXR with Meta Quest support
- XR Origin with left and right controller or hand transforms

## Locomotion prototype
Add `GlimpTagLocomotion.cs` to the player root. Assign:
- Player Rigidbody
- Left hand transform
- Right hand transform

The hand transforms should follow the tracked controller/hand poses. Tune hand radius and movement limits in the Inspector.

## Current limitations
This is starter source, not a complete Unity project. It does not yet include a finished arena, menus, avatars, matchmaking, voice chat, or a tested Quest APK.

## Arena direction
Build an original forest arena with a central treehouse, branching climb routes, wooden platforms, a cave passage, and a perimeter rock wall. Do not reproduce another game's exact map or assets.
