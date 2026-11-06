# Player customization prototype

`GlimpTagPlayerCustomization.cs` adds a simple name field and color palette.

## Setup
1. Add the script to an active scene object.
2. Assign the local avatar's body renderers to `avatarRenderers`.
3. Enter Play mode to use the on-screen customization panel.

The chosen name and color are saved locally with Unity PlayerPrefs.

## Current limitations
- Names and colors are local only; they are not yet sent to other players.
- This is a basic prototype menu, not a polished Quest wrist menu.
- There are no hats, cosmetics shop, unlocks, or saved online profiles.
- The script has not yet been compiled or tested in a complete Unity project.
