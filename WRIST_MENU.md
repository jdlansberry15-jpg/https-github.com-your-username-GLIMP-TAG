# GLIMP TAG Update 10 — Quest wrist menu

## Included
- Owner-only wrist menu attached to the local left-hand tracking proxy.
- Left controller X button or left-hand pinch toggles the menu.
- Right controller trigger or right-hand pinch selects a menu button while pointing at it.
- Mute/unmute local Vivox microphone.
- Create a Relay room when not connected, or leave/cancel the current room.
- Shows round number, player count, SAFE/INFECTED state, and the host room code.
- A small pointer line helps aim at the menu buttons.
- Menu colliders are triggers on Unity's Ignore Raycast layer, so the panel should not block locomotion or arm contacts.

## Automatic setup
The Editor network-player prefab builder adds and configures `GlimpTagWristMenu` on the generated NetworkPlayer prefab. The build validator checks that the component exists. No manual scene or prefab editing is intended.

## Controls
- Left X / left-hand pinch: open or close the wrist menu.
- Right trigger / right-hand pinch: activate the button currently under the pointer.
- MUTE MIC / UNMUTE MIC: changes only the local microphone.
- LEAVE ROOM: shuts down the current Relay/Netcode room.
- CLOSE MENU: hides the wrist menu.

Create Room and Join Room remain in the existing lobby screen because the network player (and therefore the wrist menu) only exists after a room session has started. Joining by code remains in that lobby screen. The start screen's X-button action takes priority until locomotion has started, so the first X press does not accidentally open the wrist menu.

## Compatibility and validation
The implementation uses the project's existing Unity 6000.3.28f1, XR Hands 1.7.3, Input System 1.20.1, Netcode for GameObjects 2.13.3, Multiplayer Services 2.4.1, and Vivox 16.12.1 pins. It uses Unity XR input-device features and XR Hands index/thumb joint APIs already used by the project.

Static source checks were performed. This environment does not have Unity Editor, so C# compilation, prefab regeneration, controller ray orientation, hand-tracking pinch behavior, comfort/readability, and Quest 2/3/3S performance have not been tested. The first real headset pass should adjust wrist offset and menu rotation if the panel is not naturally visible.
