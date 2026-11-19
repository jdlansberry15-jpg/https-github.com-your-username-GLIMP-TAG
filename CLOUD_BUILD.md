# Chromebook-first Quest APK build

The repository is being prepared for Unity Build Automation so the Android build can run in the cloud instead of on a local PC.

## Project version and package pins

The project is pinned to **Unity 6000.3.28f1 (Unity 6.3 LTS)** in ProjectSettings/ProjectVersion.txt. Unity Build Automation currently lists Unity 6000.3 LTS as supported.

The manifest pins these XR and multiplayer dependencies:
- OpenXR Plugin 1.16.1
- XR Plug-in Management 4.6.1
- XR Hands 1.7.3
- Unity OpenXR Meta 2.7.0
- Unity Transport 2.7.5
- Input System 1.20.1
- Multiplayer Services 2.4.1
- Netcode for GameObjects 2.13.3
- Vivox 16.12.1

**Netcode compatibility correction:** an older version of this guide called NGO 2.13.3 a pre-release for Unity 6000.3. That was outdated. Unity's 6000.3.26f1 release notes show NGO being updated from 2.13.2 to 2.13.3, so 2.13.3 is the released 6000.3 package line used by this 6000.3.28f1 project. The 6000.3 package listing now shows 2.14.0 as a separate pre-release; do not upgrade to 2.14.0 without a compile and multiplayer regression test.

The manifest pins are not proof that every package has resolved or compiled in this project. The first Unity Build Automation run must still confirm Package Manager resolution and C# compilation. See PACKAGE_COMPATIBILITY.md for the source links and status.

## Set up the cloud build from a Chromebook

1. Open the Unity Dashboard in Chrome and sign in with a Unity ID.
2. Open DevOps → Build Automation.
3. Create or select a Unity project.
4. Connect the GitHub source repository:
   jdlansberry15-jpg/https-github.com-your-username-GLIMP-TAG
5. Select the main branch. Unity Build Automation supports GitHub repositories and automatically connects to public repositories without an SSH key.
6. Create a build configuration with Android as the target.
7. Choose Unity 6000.3.28f1 or enable automatic Unity-version detection. The project version file is committed in the repository.
8. Leave the project subfolder path blank because Assets, Packages, and ProjectSettings are at the repository root.
9. In Android settings, make sure Build app bundles (.aab) instead of an APK is OFF. The goal is a sideloadable APK.
10. Save the configuration and start a build.

The Editor automation attempts to generate the forest scene, network player prefab, NetworkManager, UnityTransport, XR lobby camera, lobby UI, and infection HUD. It also configures Android ARM64 / IL2CPP / APK settings and attempts to assign OpenXR for Android with Quest interaction features. A second pre-build callback checks the generated prefab references, scene wiring, network manager, Android output settings, and OpenXR loader; see BUILD_VALIDATION.md.

## Cost and signing

Unity's published 2026 free DevOps allocation includes 200 Windows Micro minutes, 100 Linux Micro minutes, 100 Mac Standard minutes, 25 GB of storage, and 100 GB of monthly egress per organization. The actual minutes charged depend on the builder selected for the Android configuration and how long the build takes. Check the Dashboard's current usage before starting. If free limits are exhausted, Unity requires pay-as-you-go billing to continue; without a payment method, service can be blocked.

If the build configuration asks for Android signing credentials, use a keystore that you control and keep its password private. Do not put signing passwords or private keys in GitHub source files. For an early sideload test, use the signing option Unity Build Automation provides for development builds if available.

## Download and install on the Chromebook

After Unity Build Automation reports a successful build:
1. Download the APK artifact from the build result page.
2. Open a browser-based Quest sideloader such as QuestLoad or Quest Webloader in Chrome.
3. Connect the Quest with a USB-C data cable and select the headset in the browser's USB picker.
4. Enable Meta Developer Mode and accept the USB debugging prompt inside the headset.
5. Choose the downloaded GLIMP TAG APK in the sideloader and install it.
6. On the headset, open Apps → Unknown Sources to launch the test app.

Browser sideloaders use WebUSB, so no local ADB installation is needed. They still require a Chromium browser, HTTPS, a data-capable cable, Developer Mode, and the headset's debugging approval. This installation path has not yet been exercised with GLIMP TAG.

## Important limitations

- No cloud build has been started or completed.
- This environment cannot sign into the user's Unity Dashboard, create the user's Build Automation configuration, or accept legal/account prompts.
- The editor automation and pre-build validator have not been run in an actual Unity Editor yet. The generated prefab and scene therefore remain unverified.
- If Unity Build Automation reports a compile or XR configuration error, use the build log to identify the first failing script or package before retrying.
- A successful APK build would still need to be installed and tested on the actual Quest headset.

## Relay room-code dependency

The project pins `com.unity.services.multiplayer` **2.4.1** for the Unity 6000.3 line. The generated NetworkManager receives a Relay session component and the lobby UI uses Relay join codes rather than same-Wi-Fi IP addresses. Before a real connection test, the Unity Cloud project must be linked and Authentication plus Relay enabled in Unity Dashboard. See RELAY_SETUP.md. NGO 2.13.3 is the released package line for Unity 6000.3.26f1 and later.
