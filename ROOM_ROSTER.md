# GLIMP TAG room roster overlay

The project now includes a runtime-installed screen-space room roster at:

`Assets/GLIMP_TAG/Scripts/GlimpTagRoomRosterOverlay.cs`

## What it does

- Installs itself after scene load; no scene or prefab editing is required.
- Lets the player save a display name before hosting or joining. The existing Vivox login code reads `GlimpTag_PlayerName` from PlayerPrefs.
- Shows connected NGO players and their SAFE / INFECTED state.
- Shows Vivox participants in channels whose names start with `GLIMPTAG`.
- Shows the Vivox `SpeechDetected` state as SPEAKING.
- Adds per-participant local MUTE / UNMUTE buttons.

## Local mute behavior

The mute button affects only the local player's audio. It does not mute another player's microphone for everyone. This is intentional: global moderation requires server-authorized moderation controls and a moderation policy.

## Current limitations

- The network roster uses NGO client IDs; remote avatar nameplates are not implemented yet.
- The overlay is a temporary screen-space debug UI, not a final world-space VR wrist menu.
- The overlay depends on the existing Vivox service being enabled and the player having joined the room's positional voice channel.
- The room-code-derived Vivox channel name is not a secure authorization boundary.

## Validation

The new source file was committed to the repository. API names were checked against the Unity Vivox 16 documentation and NGO 2.13 NetworkVariable documentation. Unity compilation, a real Relay/Vivox session, cloud APK generation, and Quest device testing have not been run in this environment.
