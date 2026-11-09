# GLIMP TAG setup notes

## Target
- Unity 6.3 LTS (6000.3.28f1)
- Meta Quest 2 / Quest 3 / Quest 3S
- Android ARM64 / IL2CPP
- OpenXR with Meta Quest support

## Automatic scene setup
When the project opens in Unity, `Assets/Editor/GlimpTagProjectBuilder.cs` attempts to create `Assets/Scenes/GLIMP_TAG.unity`, add it to Build Settings, configure Android player settings, assign the OpenXR loader, and enable Quest controller / hand-tracking features.

The generated scene contains `GlimpTagRuntimeBootstrap`. At runtime it builds the original forest arena, spawn points, player Rigidbody/capsule, tracked camera, hand proxies, and start overlay.

## Chromebook workflow
The preferred route is Unity Build Automation. Connect this public GitHub repository to Unity Build Automation, select Android, use Unity 6000.3.28f1, and make sure the build output is an APK rather than an AAB. See the repository root `CLOUD_BUILD.md`.

After an APK is produced, use the SideQuest web installer in Chrome on the Chromebook. See `QUEST_INSTALL.md`.

## Prototype limitations
- The project builder has not been executed in a real Unity Editor here.
- The camera and hand tracking code have not been compiled or tested on Quest.
- The forest arena is generated from built-in primitives; it is not a final art pass.
- The tag and networking scripts are not yet connected to the generated scene.
- No cloud build or APK has been produced.
