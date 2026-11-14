# GLIMP TAG Relay room setup

## What is now wired in source

The project now includes:
- `GlimpTagRelaySession` on the NetworkManager GameObject.
- A room-code lobby with Create Room, Join Room, Copy Room Code, player count, and Leave Room.
- Unity Authentication anonymous sign-in before Relay requests.
- Unity Relay allocation/join-code calls and Unity Transport Relay configuration.
- A build preflight check that requires the Relay lobby component.

## Unity package versions

For the repository's Unity Editor **6000.3.28f1**:
- `com.unity.services.multiplayer` **2.4.1** — released for Unity 6000.3.
- `com.unity.transport` **2.7.5** — released for Unity 6000.3.
- `com.unity.netcode.gameobjects` **2.13.3** — still listed as a pre-release for Unity 6000.3.

The unified Multiplayer Services package supplies the Relay and Authentication APIs used by the component. The standalone Relay package is not added.

## Required one-time Unity Dashboard setup

A project owner must:
1. Open Unity Dashboard and select the Unity Cloud project linked to this Unity project.
2. Enable Authentication and Relay in the development environment.
3. Confirm the project is linked under Unity Editor Project Settings → Services.
4. Confirm the Relay service is available for the project's region/environment.

No access token, service secret, or player credential belongs in the source repository. The prototype uses anonymous sign-in.

## How the lobby works

- Host presses **CREATE ROOM**. The app signs in, requests a Relay allocation for up to seven remote players, obtains a join code, configures Unity Transport with DTLS, then starts the NGO host.
- Friends enter the code and press **JOIN ROOM**. The app signs in, joins the allocation, configures the same transport, and starts the NGO client.
- The host can display the code and attempt to copy it to the system clipboard.
- Leave Room shuts down the local NGO session; the existing lobby camera handoff restores the lobby camera.

Relay join codes are shared out-of-band. There is no public room browser, friend list, quick-match, or host migration yet.

## Important limits

- The room creator's headset is the host. If it disconnects, the room ends.
- Anonymous authentication is only a prototype identity. Account recovery, moderation, parental controls, and bans are not implemented.
- Clipboard access can vary by Quest OS/runtime. The code remains visibly displayed so it can be read aloud or shared separately.
- The OnGUI lobby is a temporary debug UI, not a polished VR wrist menu.
- This source has not been compiled in Unity, connected to a real Unity Cloud project, or tested on a Quest. A StartHost/StartClient return value does not prove that a remote player connected.


## Proximity voice chat

The project now includes Unity Vivox 16.12.1 for Unity 6000.3. See [VOICE_CHAT.md](VOICE_CHAT.md) for Unity Dashboard provisioning, microphone permission, positional audio, mute controls, and release limitations.

Voice joins the Vivox positional channel derived from the Relay room code after the local player connects. The lobby displays a local microphone mute/unmute control. Vivox must be enabled and provisioned in the same Unity Cloud project before voice can work.
