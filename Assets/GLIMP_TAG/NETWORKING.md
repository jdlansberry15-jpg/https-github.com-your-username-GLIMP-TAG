# Multiplayer prototype notes

The project uses Netcode for GameObjects (NGO) with Unity Transport.

## Automatic setup
When Unity opens the project, Assets/Editor/GlimpTagProjectBuilder.cs calls GlimpTagNetworkPrefabBuilder. The editor script creates:
- Assets/Resources/GLIMP_TAG/NetworkPlayer.prefab
- A NetworkManager and UnityTransport in the generated GLIMP TAG scene
- A host/join lobby UI and infection round HUD
- A player rig that enables XR input only on the owning client

The prefab contains a root NetworkObject, owner-authoritative root NetworkTransform, networked infection state, head/hand pose synchronization, local hand trigger colliders, remote avatar visuals, and a Rigidbody/capsule.

## Local Wi-Fi testing
The lobby currently supports direct LAN connections only:
1. Start Host on one Quest.
2. On the other Quest, enter the host headset's Wi-Fi IP and press Join.
3. Both headsets must be on the same Wi-Fi network and the router must permit device-to-device UDP traffic.

The host binds to all local interfaces on UDP port 7777. The joining headset connects to the IP entered in the lobby. This is not internet matchmaking and does not use Unity Relay.

## Packages
The manifest currently pins:
- Netcode for GameObjects 2.13.3 (pre-release in the Unity 6.3 package listing)
- Unity Transport 2.7.5
- XR Hands 1.7.3 (released for Unity 6.3)
- Meta OpenXR 2.7.0
- OpenXR Plugin 1.16.1

The Unity 6.3 package catalogue lists XR Hands 1.7.3 as released for Editor 6000.3, while NGO 2.13.3 is listed as pre-release. Revisit the NGO package version before a public release.

## Important limitations
- The generated prefab and scene are created when Unity Editor automation runs; they are not committed binary/YAML assets in this repository.
- The direct LAN lobby is only for same-network testing. Relay, room codes, matchmaking, friends, voice chat, and moderation are not implemented.
- Owner-authoritative movement trusts the client. Add server movement validation before public matches.
- The server validates tag requests with ownership, infection state, and a coarse player-root distance check. Accurate hand hit detection depends on the generated hand colliders and synchronized avatar poses.
- The generated player prefab and scene have not been compiled or tested in Unity or on Quest 2, Quest 3, or Quest 3S.
