# Quest XR rig and hand input

The project now creates its first XR player rig automatically when the generated scene starts.

## Runtime rig

- A player root with a Rigidbody and capsule collider
- A floor-relative tracking-space transform
- A camera driven from the XR center-eye pose
- Left and right hand proxy transforms
- XR Hands palm-joint tracking when the OpenXR hand subsystem is running
- Quest Touch controller pose fallback
- Hand-contact locomotion with a per-step correction limit and player speed cap

## OpenXR setup

The Unity Editor automation in `Assets/Editor/GlimpTagProjectBuilder.cs` attempts to:
- Assign the OpenXR loader for Android
- Enable Meta Quest Support
- Enable the Oculus Touch controller interaction profile
- Enable the Meta Quest Touch Plus controller profile
- Enable XR Hands OpenXR features when they are present
- Set Android ARM64, IL2CPP, and APK output settings

Unity's official Meta Quest OpenXR guidance requires Meta Quest Support and a supported interaction profile. The editor script uses the same settings programmatically.

## Hand-tracking notes

The hand input uses `XRHandSubsystem` and the Palm joint. If the provider does not expose a Palm pose, it falls back to the hand root pose. When hand tracking is unavailable, the script uses the Touch controller grip pose.

Finger curls, pinch gestures, hand meshes, haptic feedback, and full avatar hand animation are not implemented yet.

## Locomotion notes

When a hand is already planted against a solid surface, moving that tracked hand moves the player root in the opposite direction. First contact only corrects the hand to the surface; subsequent hand motion pushes the player. Corrections are capped to reduce sudden jumps.

This is still a prototype. Surface contact, body collision, tracking-origin behavior, and comfort need real Quest testing before the movement can be called stable.

## Build status

The scripts have not been compiled in Unity or tested on Quest 2, Quest 3, or Quest 3S. See `CLOUD_BUILD.md` for the browser-based build path.
