# GLIMP TAG

GLIMP TAG is an original arm-swing VR tag-game prototype for Meta Quest 2, Quest 3, and Quest 3S.

## Current vertical-slice scaffold

- Unity XR center-eye camera tracking
- XR Hands palm tracking with Quest Touch controller fallback
- Hand-contact locomotion with correction and speed limits
- An original generated forest arena with treehouse, circular bridge, climbing trees, gazebo, cave, rocks, and six spawn points
- Editor-generated network player prefab with local owner XR rig and remote avatar visuals
- Head and hand pose synchronization at a default 20 Hz
- Host/join local Wi-Fi lobby using Unity Transport
- Server-host infection rounds, random first infected, tag requests, timer, win states, and automatic round restart
- Temporary round HUD and lobby camera handoff
- Android ARM64 / IL2CPP / APK build settings

## Chromebook-first workflow

The project is pinned to Unity 6.3 LTS and prepared for Unity Build Automation. The intended workflow is to connect this public GitHub repository to Unity Build Automation in a browser, build Android in the cloud, and download the APK. See CLOUD_BUILD.md.

After a cloud build produces an APK, a Chromebook can use WebADB in Chrome with a data-capable USB-C cable to install it. See QUEST_INSTALL.md.

## Important status

This is still a source scaffold, not a finished or verified game. The Editor automation generates the scene, network player prefab, NetworkManager, lobby camera, and avatar materials when Unity opens the project. Those generated prefab and scene assets are not committed as binary/YAML files.

The scripts have not been compiled in a real Unity Editor. No cloud build has been started, no APK has been produced, and nothing has been installed or tested on Quest 2, Quest 3, or Quest 3S.

The lobby currently supports direct same-Wi-Fi host/join only. Internet Relay, room codes, matchmaking, friends, voice chat, moderation, and server-side movement anti-cheat are not implemented. The owner-authoritative movement and simple tag-distance validation are prototypes that need multiplayer testing.

The forest geometry is original and does not reproduce another game's proprietary map, art, or branding.
