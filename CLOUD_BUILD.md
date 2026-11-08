# Chromebook-first Quest APK build

The repository is being prepared for Unity Build Automation so the Android build can run in the cloud instead of on a local PC.

## Project version and package pins

The project is pinned to **Unity 6000.3.28f1 (Unity 6.3 LTS)** in `ProjectSettings/ProjectVersion.txt`.

The XR packages are pinned to versions released for Unity 6000.3:
- OpenXR Plugin 1.16.1
- XR Plug-in Management 4.6.1
- XR Hands 1.7.3
- Unity OpenXR Meta 2.7.0
- Input System 1.20.1

Unity Netcode for GameObjects 2.13.3 is a pre-release package for Unity 6000.3. It is included because the repository already contains NGO prototype scripts. The first forest movement slice does not yet use online multiplayer.

## Set up the cloud build from a Chromebook

1. Open the [Unity Dashboard](https://dashboard.unity.com/) in Chrome and sign in with a Unity ID.
2. Open **DevOps → Build Automation**.
3. Create or select a Unity project.
4. Connect the GitHub source repository:
   `jdlansberry15-jpg/https-github.com-your-username-GLIMP-TAG`
5. Select the `main` branch. The repository is public, so Unity Build Automation can connect to public GitHub repositories without a private-repository SSH key.
6. Create a build configuration with **Android** as the target.
7. Choose **Unity 6000.3.28f1** or enable automatic Unity-version detection. The project version file is committed in the repository.
8. Leave the project subfolder path blank because `Assets`, `Packages`, and `ProjectSettings` are at the repository root.
9. In Android settings, make sure **Build app bundles (.aab) instead of an APK** is OFF. The goal is a sideloadable APK.
10. Save the configuration and start a build.

The Editor automation attempts to create the scene, add it to Build Settings, assign OpenXR for Android, enable Quest interaction features, and set ARM64 / IL2CPP / APK settings when the project is opened by the build worker.

## Cost and signing

Unity's published 2026 DevOps pricing includes monthly free usage for several builder types, but build time depends on the selected machine and project. Check the Unity Dashboard's current usage and billing page before starting a build. Do not assume every build will remain free.

If the build configuration asks for Android signing credentials, use a keystore that you control and keep its password private. Do not put signing passwords or private keys in GitHub source files. For an early sideload test, use the signing option Unity Build Automation provides for development builds if available.

## Download and install on the Chromebook

After Unity Build Automation reports a successful build:
1. Download the APK artifact from the build result page.
2. Open [SideQuest's web installer](https://sdq.st/download) in Chrome on the Chromebook.
3. Connect the Quest with a USB-C cable that supports data.
4. Put on the headset and accept the **Allow USB debugging** prompt.
5. Choose the downloaded GLIMP TAG APK in the web installer and install it.
6. On the headset, open Apps and choose **Unknown Sources** to launch the test app.

The user already has Quest Developer Mode enabled. The USB debugging authorization prompt and a data-capable cable are still required for browser sideloading.

## Important limitations

- No cloud build has been started or completed by this repository update.
- This environment cannot sign into the user's Unity Dashboard, create the user's Build Automation configuration, or accept legal/account prompts.
- The editor automation has not been run in an actual Unity Editor yet. If Unity Build Automation reports a compile or XR configuration error, use the build log to identify the first failing script or package before retrying.
- A successful APK build would still need to be installed and tested on the actual Quest headset.
