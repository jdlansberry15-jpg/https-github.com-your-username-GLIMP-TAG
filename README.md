# GLIMP TAG

GLIMP TAG is an original arm-swing VR tag-game prototype for Meta Quest 2, Quest 3, and Quest 3S.

## Current vertical-slice scaffold

- Unity XR center-eye camera tracking
- XR Hands palm tracking with Quest Touch controller fallback
- Hand-contact locomotion with tracking-loss recovery, own-collider filtering, and speed limits
- An original generated forest arena with treehouse, circular bridge, climbing trees, gazebo, cave, rocks, and eight spawn points
- Editor-generated network player prefab with local owner XR rig and remote avatar visuals
- Head and hand pose synchronization at a default 20 Hz, including explicit per-hand tracking-validity flags and hidden remote hands during tracking loss
- Relay room-code lobby with Create Room, Join Room, Copy Room Code, player count, and Leave Room
- Unity Services anonymous sign-in and Relay-backed Netcode host/client setup
- Server-host infection rounds, random first infected, hand-pose/reach-checked tag requests, timer, win states, automatic round restart, and owner-side fall recovery
- Temporary round HUD and lobby camera handoff
- Android ARM64 / IL2CPP / APK build settings
- Automated pre-build checks for the generated player prefab, scene wiring, network manager, and XR setup

## Chromebook-first workflow

The project is pinned to Unity 6.3 LTS and prepared for Unity Build Automation. The intended workflow is to connect this public GitHub repository to Unity Build Automation in a browser, build Android in the cloud, and download the APK. See CLOUD_BUILD.md.

After a cloud build produces an APK, a Chromebook can use a browser-based Quest sideloader in Chrome with a data-capable USB-C cable to install it. See QUEST_INSTALL.md.

## Networked hand tracking

See POSE_TRACKING.md for per-hand tracking-validity synchronization, remote hand visibility, and stale-pose tag rejection.

## Compatibility and locomotion notes

- PACKAGE_COMPATIBILITY.md records current Unity 6000.3 package/build checks and the important NGO 2.13.3 release-note correction.
- LOCOMOTION_STABILITY.md explains hand-tracking loss/reacquisition safety and the remaining headset test plan.

## Important status

This is still a source scaffold, not a finished or verified game. The Editor automation generates the scene, network player prefab, NetworkManager, lobby camera, and avatar materials when Unity opens the project. Those generated prefab and scene assets are not committed as binary/YAML files.

The scripts and pre-build validator have not been run in a real Unity Editor. No cloud build has been started, no APK has been produced, and nothing has been installed or tested on Quest 2, Quest 3, or Quest 3S.

The lobby source now supports Unity Relay room codes for private friend rooms. Unity Cloud project linking plus Authentication and Relay enablement are still required before the feature can connect. Public matchmaking, a room browser, friends list, host migration, voice chat, moderation, and server-side movement anti-cheat are not implemented. The owner-authoritative movement remains a prototype. Tag requests now use the attacker's last owner-published hand pose plus a server-side reach envelope, but this is still basic validation rather than authoritative physics or a complete anti-cheat system; it needs multiplayer latency testing.

See RELAY_SETUP.md for the one-time Unity Dashboard setup and known limits.

The forest geometry is original and does not reproduce another game's proprietary map, art, or branding.
