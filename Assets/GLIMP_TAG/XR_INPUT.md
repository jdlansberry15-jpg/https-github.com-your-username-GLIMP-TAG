# Quest XR rig and hand input

The Unity Editor builder generates a network player prefab and wires the local XR rig automatically.

## Generated local player rig
- Root Rigidbody and capsule collider for hand-contact locomotion
- Tracking-space transform and center-eye camera
- XR Hands palm-joint tracking when the hand subsystem is running
- Quest Touch controller pose fallback
- Left and right tracked hand proxy objects with tag trigger colliders
- Arm-swing locomotion enabled only for the owning network client
- A small start overlay that enables locomotion after the player spawns

## Remote avatar rig
- Remote player body, head, and hand visuals
- Head and hand pose snapshot synchronized by an owner-writable NetworkVariable
- 20 Hz default pose update rate
- Smoothed remote visual targets
- Remote Rigidbody made kinematic and local camera/audio disabled

## OpenXR setup
The Editor automation attempts to:
- Assign the OpenXR loader for Android
- Enable Meta Quest Support
- Enable the Oculus Touch and Meta Quest Touch Plus interaction profiles
- Enable XR Hands OpenXR features when available
- Set Android ARM64, IL2CPP, and APK output settings

## Locomotion and comfort
The existing movement prototype uses hand contact to move the player in the opposite direction, with per-step correction and speed limits. The networked player prefab starts with the locomotion menu enabled and only the local owner's rig reads XR devices.

This remains prototype movement. Tracking-origin behavior, collisions, hand placement, avatar interpolation, and comfort need real Quest testing.

## Build status
The editor-generated prefab and scene have not been compiled or tested. No APK has been produced. See CLOUD_BUILD.md for the Chromebook-friendly cloud build route.
