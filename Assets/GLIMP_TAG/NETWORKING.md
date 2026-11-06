# Multiplayer prototype notes

The networking scripts use **Netcode for GameObjects** (NGO).

## Unity packages still required
- Netcode for GameObjects
- Unity Transport
- XR Plug-in Management and OpenXR for Quest input

## Scene setup still required
1. Add a NetworkManager to the scene.
2. Add UnityTransport to the NetworkManager object.
3. Create a player prefab with a NetworkObject and GlimpTagNetworkPlayer.
4. Register the player prefab in NetworkManager's Network Prefabs list.
5. Add networked transform synchronization appropriate to the chosen player rig.
6. Configure a real session/relay service before testing across the internet.

## Prototype limits
These scripts are source examples only. They have not been compiled or tested in Unity. They do not yet provide matchmaking, room codes, voice chat, anti-cheat, or a complete Quest player rig. The earlier local tag scripts are not automatically network synchronized; use GlimpTagNetworkPlayer and GlimpTagNetworkTagger for the networked tag-state prototype.
