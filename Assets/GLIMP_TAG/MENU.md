# GLIMP TAG main menu prototype

`GlimpTagMainMenu.cs` draws a simple title screen with Play and Customize buttons.

## Setup
1. Create a scene named `MainMenu`.
2. Add an empty GameObject and attach `GlimpTagMainMenu.cs`.
3. Create a gameplay scene named `ForestArena` (or change the script's gameplaySceneName).
4. Add both scenes to Build Settings / Build Profiles scene list.
5. Optionally assign a `GlimpTagPlayerCustomization` component to the customization field.

## Limitations
- This is a basic Unity OnGUI prototype, not a polished VR menu.
- The gameplay scene must exist and be included in the build.
- The Play button does not create or join a multiplayer room.
- The script has not yet been compiled or tested in a complete Unity project.
