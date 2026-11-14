# GLIMP TAG proximity voice chat

## What this increment adds

GLIMP TAG now has a Unity Vivox v16 integration component designed for room-scoped proximity voice:

- Automatically signs into Vivox after the player joins a Relay room.
- Uses the Relay room join code to derive a matching Vivox channel name.
- Joins a 3D positional audio channel, so nearby players hear each other and voices fade with distance.
- Reports the local network player's world position to Vivox 3 times per second.
- Adds a MUTE MICROPHONE / UNMUTE MICROPHONE control to the existing lobby.
- Adds a retry control if the player denies microphone permission or voice service connection fails.
- Leaves the voice channel when the player leaves or disconnects from the Relay room.

## Package compatibility

The repository is pinned to Unity Editor 6000.3.28f1. Unity's released-package list for Unity 6000.3 lists com.unity.services.vivox 16.12.1 as released. The package is added to Packages/manifest.json.

Official package compatibility:
https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-services-vivox

## One-time Unity Dashboard setup

A project owner must:

1. Open Unity Dashboard and select the Unity Cloud project linked to GLIMP TAG.
2. Go to Development → Products → Vivox Voice and Text Chat.
3. Complete the Vivox onboarding for the project/environment.
4. In Unity Project Settings → Services, confirm the correct Project ID and Vivox credentials/environment are linked. Use the Automatic environment option when available.
5. Enable Authentication for the same Unity Cloud project. The prototype reuses the anonymous Unity Authentication sign-in already used by Relay.

Do not paste Vivox credentials, signing keys, or service tokens into GitHub or ChatGPT. The Unity Authentication integration is used so the SDK can manage Vivox tokens.

## How it works

- The host creates a Relay room. Its join code becomes a channel name such as GLIMPTAGABC123.
- Clients joining that Relay code derive the same channel name.
- The app joins a positional audio-only Vivox channel with a 24-unit audible distance and 2-unit conversational distance.
- Position is updated at 3 Hz (within Unity's recommended 2–4 updates per second).
- The microphone permission is requested at the first room voice join on Android.
- Mute is local: it stops this player's microphone from being transmitted. It does not mute another player for everyone else.

Vivox channel names are restricted to a documented character set; the code normalizes the room code to uppercase alphanumeric characters.

## Privacy, safety, and release limitations

- Voice is not joined until a player enters a Relay room.
- The microphone is only transmitted after Android microphone permission is granted. Players can mute themselves from the lobby.
- A user who denies microphone permission can still play without voice and can retry after enabling permission in Quest app settings.
- The optional Bluetooth Connect permission is disabled by default. Enable it in the component only if external Bluetooth audio device support is needed on Android 12 or later.
- The channel name is derived from the Relay join code. This prototype does not use Vivox Access Tokens (VATs) or a separate authorization server. Treat the room code as private; add server-issued access tokens before a public release.
- Per-player mute/block UI, report tools, parental controls, age gating, moderation workflows, push-to-talk, and voice activity indicators are not implemented.
- Vivox dashboard provisioning and service terms/limits must be reviewed by the project owner.

## Validation status

The C# source, package manifest JSON, component wiring edits, and static brace checks were reviewed in this environment. Unity Editor compilation, Vivox project provisioning, Android microphone permission prompts, voice audio, spatial attenuation, Relay/Vivox cross-service login, APK creation, and Quest 2/3/3S testing have not been performed.
