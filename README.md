# GLIMP TAG

GLIMP TAG is an original VR tag-game prototype targeting Meta Quest 2, Quest 3, and Quest 3S.

## Current source scaffold
- Physics-based arm-push locomotion prototype
- Procedural original forest-arena generator
- Local tag / infection round logic
- NGO player tag-state and color synchronization prototypes
- Basic host / client lobby UI
- Local player name and color customization
- Main menu prototype
- Quest XR controller / hand pose bridge
- Unity 6.1 package manifest and project version pin

## Open the project
This repository is being shaped into a Unity project scaffold. The package manifest is in Packages/manifest.json and scripts are under Assets/GLIMP_TAG.

## Chromebook-first build direction
The preferred no-local-PC route is Unity Build Automation: it can build Unity projects from GitHub source control in the cloud and supports Android / Oculus targets. It still needs a Unity account, a configured Unity Cloud project and build target, and a valid Unity project with scenes and XR settings. See CLOUD_BUILD.md.

## Honest status
This is not a finished playable game. The repository still needs generated Unity scenes, a configured XR Origin/player rig, proper scene wiring, and a successful cloud build. No APK has been produced or tested on a headset yet.

The arena direction is an original forest with a central treehouse, climb routes, platforms, a stump hangout, rock perimeter, and cave passage. It does not copy another game's map or assets.
