# GLIMP TAG

GLIMP TAG is an original arm-swing VR tag-game prototype for Meta Quest 2, Quest 3, and Quest 3S.

## Current vertical-slice scaffold

- A dedicated Quest camera pose driver using Unity XR input
- XR Hands palm-pose input with Touch controller fallback
- Hand-contact locomotion using collision correction and a speed cap
- An automatically generated original forest arena
- Central treehouse, circular raised bridge, climbable trees and branches
- Stump gazebo hangout, cave tunnel, rock perimeter, and spawn points
- A headset-friendly start overlay
- Android ARM64 / IL2CPP / APK build settings
- Editor automation that creates the initial scene and configures OpenXR when the project opens

## Chromebook-first workflow

The project is configured for Unity 6.3 LTS and Unity Build Automation. The intended workflow is to connect this public GitHub repository to Unity Build Automation in a browser, create an Android build target, and download the APK. See [CLOUD_BUILD.md](CLOUD_BUILD.md).

After a cloud build produces an APK, a Chromebook can use the SideQuest web installer with Chrome and a data-capable USB-C cable. See [QUEST_INSTALL.md](QUEST_INSTALL.md).

## Important status

This is a generated source scaffold and early single-player forest movement test, not a finished game. The editor automation has not been executed in a real Unity Editor in this environment. The project has not been compiled, the XR rig has not been validated on a headset, and no APK has been produced.

The networking scripts remain prototype code. There is no working online matchmaking, public room system, voice chat, or verified multiplayer session yet. The local tag scripts are not yet wired into the new runtime scene.

The forest geometry is original and does not reproduce another game's proprietary map, art, or branding.
