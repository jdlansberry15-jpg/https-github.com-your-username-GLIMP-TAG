# GLIMP TAG lobby prototype

The `GlimpTagLobbyUI.cs` script provides simple Host, Join, and Leave buttons for testing Netcode for GameObjects.

## Unity setup
1. Install Netcode for GameObjects and Unity Transport.
2. Add a NetworkManager and UnityTransport to the scene.
3. Configure the player prefab and Network Prefabs list.
4. Add GlimpTagLobbyUI to an active scene object.
5. For local testing, configure the transport address and port before starting a client.

## Important limitations
- This is a basic host/client test menu, not a public matchmaking system.
- There are no room codes, friends list, server browser, or internet relay setup yet.
- A successful StartClient call means the client start was requested; it does not guarantee a connection.
- No voice chat is included.
- The code has not yet been compiled or tested in a Unity project.
