# Chromebook-friendly cloud build route

## Preferred option: Unity Build Automation

Unity Build Automation is the best fit for a Chromebook-only workflow because the Unity Editor and Android build tools run in Unity's cloud service. Unity's documentation says it can connect to GitHub and build Android and Oculus targets.

Official documentation:
- Build Automation overview: https://docs.unity.com/en-us/build-automation
- Connect GitHub source control: https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- Unity DevOps pricing updates: https://unity.com/products/pricing-updates

## What must exist before a real APK build
- A valid Unity project and supported Unity Editor version
- Android / Meta Quest OpenXR configuration
- At least one playable scene included in the build
- An XR Origin and player rig wired to the locomotion and XR input scripts
- A Unity Cloud project and a Build Automation configuration connected to this GitHub repository

## Browser setup that will eventually be needed
1. Sign in to the Unity Dashboard from the Chromebook with the Unity account.
2. Create or select a Unity Cloud project.
3. Open Build Automation and connect the public GitHub repository:
   jdlansberry15-jpg/https-github.com-your-username-GLIMP-TAG
4. Select the main branch and configure an Android / Oculus build target using the Unity version pinned in ProjectSettings/ProjectVersion.txt.
5. Run a build and inspect its logs. Download an APK only after the service reports a successful build.

For a public GitHub repository, Unity's documentation says Build Automation can connect without adding an SSH key. If using a personal access token instead, grant only the minimum repository access needed. Auto-build webhooks need additional write access, so do not grant it unless auto-build is enabled.

## Cost and account notes
Unity's March 2026 DevOps pricing update lists monthly free storage and egress allowances and free build minutes across supported compute types. Build minutes and downloads can be metered; check the live Unity pricing page before enabling paid usage or adding a payment method. Do not assume every build will fit inside a free allowance.

## Why this is not ready to press Build yet
The repository currently has scripts and a package manifest, but it does not yet contain a complete XR Origin scene, a configured player prefab, or a playable scene list. Starting a cloud build before those are created would not produce the intended game.

## Alternative: GitHub Actions + GameCI
This is possible, but GameCI requires a Unity license activation and GitHub Actions secrets. That is more setup than Unity Build Automation for this Chromebook-first project, so it is not the preferred first route.
