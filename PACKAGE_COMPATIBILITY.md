# GLIMP TAG package and build compatibility notes

Last checked: 2026-11-18

## Editor and cloud build

- Project Editor: Unity 6000.3.28f1 (Unity 6.3 LTS).
- Unity Build Automation lists the 6000.3 LTS line as supported.
- Android output target: APK, not AAB; ARM64; IL2CPP.

Official references:
- [Unity Build Automation supported Unity versions](https://docs.unity.com/en-us/build-automation/reference/supported-unity-versions)
- [Unity Build Automation build configuration](https://docs.unity.com/en-us/build-automation/basic-build-configuration/overview)

## Netcode for GameObjects

The project pins `com.unity.netcode.gameobjects` **2.13.3**.

The Unity 6000.3.26f1 release notes explicitly show Netcode for GameObjects being updated from 2.13.2 to 2.13.3. This corrects an older project note that called 2.13.3 pre-release for Unity 6000.3. The project uses Unity 6000.3.28f1, which is later in that LTS patch line.

- [Unity 6000.3.26f1 release notes](https://unity.com/releases/editor/whats-new/6000.3.26f1)
- [Unity 6000.3 Netcode for GameObjects pre-release listing](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-preview/com-unity-netcode-gameobjects)

Unity's current 6000.3 pre-release listing shows 2.14.0 as a separate pre-release. Do not move the project to 2.14.0 just because it is newer: it needs an actual compile and network regression test first.

## Unity Transport

The project pins Unity Transport **2.7.5**, which Unity lists as released for Unity 6000.3.

- [Unity Transport package status for Unity 6000.3](https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-transport)

## Locomotion physics API

The locomotion stability increment uses:
- `Physics.OverlapSphereNonAlloc`
- `Physics.SphereCastNonAlloc`

These APIs accept reusable result buffers, return the number of stored hits, and avoid allocating a new result array for each physics query.

- [Unity 6000.3 OverlapSphereNonAlloc API](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/physics/overlapspherenonalloc)
- [Unity SphereCastNonAlloc API](https://docs.unity.cn/6000.1/Documentation/ScriptReference/Physics.SphereCastNonAlloc.html)

## What has and has not been verified

Verified from source/documentation:
- The pinned Editor version is on the Unity 6000.3 LTS line.
- Unity Build Automation supports the 6000.3 LTS line.
- NGO 2.13.3 appears in Unity 6000.3.26f1 release notes.
- Unity Transport 2.7.5 is listed as released for Unity 6000.3.
- The locomotion physics query signatures exist in Unity 6 documentation.

Not verified:
- Actual Package Manager resolution of every manifest dependency in this project.
- C# compilation in Unity 6000.3.28f1.
- Editor-generated scene and prefab correctness.
- Android/Quest APK build.
- Runtime performance, comfort, hand tracking, or multiplayer behavior on Quest 2, Quest 3, or Quest 3S.

The GitHub Actions preflight checks source presence, JSON pins, and expected code markers. It is not a Unity compiler or APK builder.
