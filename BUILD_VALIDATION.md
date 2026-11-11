# Quest build preflight validation

The Editor script Assets/Editor/GlimpTagBuildValidation.cs runs before Android builds.

## Checks that fail the build
- Android target, APK output, IL2CPP, and ARM64 are selected.
- GLIMP TAG main scene is enabled in Build Settings.
- The generated NetworkPlayer prefab exists.
- The prefab contains its NetworkObject, Rigidbody, capsule, XR rig scripts, owner NetworkTransform, avatar pose sync, local hand triggers, remote visuals, and required serialized references.
- The main scene contains the forest bootstrap, NetworkManager, UnityTransport, lobby UI, infection HUD, and temporary XR lobby camera.
- NetworkManager uses the scene UnityTransport and references the generated NetworkPlayer prefab.

## Checks that produce warnings
- Android OpenXR loader assignment.
- Meta Quest Support OpenXR feature activation.

The warnings are intentional because OpenXR feature registration can vary with package metadata and needs confirmation in the cloud build log.

## Run manually in Unity
Use the menu item GLIMP TAG > Validate Quest Build Setup. This is optional; Unity Build Automation runs the same checks automatically before an Android build.

## What this does not prove
- It does not compile C# itself; Unity compiles scripts before the build callbacks run.
- It does not simulate multiplayer, validate hand tracking, measure Quest frame rate, or test a headset.
- A passing preflight means the generated scene/prefab wiring and build settings passed these checks. It is not proof that the APK launches or plays correctly.
