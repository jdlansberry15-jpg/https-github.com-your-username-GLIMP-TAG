# GLIMP TAG room roster and remote nameplates

The project includes a runtime-installed roster overlay and generated remote avatar nameplates.

## Player names

- The player saves a display name in the pre-room overlay.
- The owning player submits the name to the server through an owner-authorized NGO RPC.
- The server removes unsupported characters, trims the name to 20 characters, and publishes it through a `FixedString64Bytes` NetworkVariable.
- The fixed string is supported natively by NGO 2.13 and avoids per-update managed string allocations.
- The screen-space room roster now shows the synchronized player name instead of only a client ID.

## Remote avatar nameplates

- The Editor prefab builder creates a lightweight built-in TextMesh above each remote avatar.
- Nameplates display the player name and SAFE / INFECTED state.
- Nameplates turn cyan and add SPEAKING while the matching Vivox participant is detected speaking.
- The nameplate is parented under Remote Avatar Visuals, which is disabled for the local owner, so the player does not see their own floating label.
- Nameplates billboard toward the local headset camera.

## Voice state

The existing Vivox roster continues to provide local MUTE / UNMUTE controls. Speaking state is matched case-insensitively by the saved display name. Players should choose distinct names; duplicate names can cause more than one avatar label to show the same speaking state.

## Current limitations

- The UI is still a temporary screen-space roster, not a polished wrist menu.
- TextMesh uses Unity's built-in font and may need visual tuning on Quest.
- No voice token service, server-side room authorization, friend moderation policy, or report/block workflow is included.
- Unity compilation, real Relay/Vivox sessions, cloud APK generation, and Quest headset testing have not been run in this environment.
