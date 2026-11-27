# GLIMP TAG automated project preflight

A GitHub Actions workflow now checks the repository on pushes to `main`, pull requests targeting `main`, and manual dispatches.

## What the workflow checks

- Required Unity project, Editor automation, XR, locomotion, networking, Relay, voice, and wrist-menu source files exist.
- `ProjectSettings/ProjectVersion.txt` has a valid Unity Editor version and remains on the documented Unity 6.3 LTS line.
- `Packages/manifest.json` parses as JSON and the XR, NGO, Multiplayer Services, Transport, and Vivox packages remain pinned to the versions documented for this project.
- The Editor project builder, network prefab builder, and Unity pre-build validator still contain their expected integration markers.
- The Unity Build Automation `PreExport` method exists, is fail-fast, and is documented in CLOUD_BUILD.md.
- The cloud-build instructions reference the same pinned Unity Editor version.
- Missing generated scene/prefab files are reported as warnings because the project intentionally creates them through Unity Editor automation.

## Where to see the report

Open the repository's **Actions** tab, select **GLIMP TAG Project Preflight**, and open the latest run. The workflow summary shows each check. A JSON report is uploaded as `glimp-tag-preflight-report` even when a check fails.

## Important scope limit

This workflow is a fast, dependency-free static check. It does **not** install Unity, compile C#, resolve Unity packages, run the Editor scene/prefab generator, execute Unity's `IPreprocessBuildWithReport` validation, build an APK, or test Quest hardware. A green run is not a Unity build result.

The actual APK still needs Unity Build Automation (or another licensed Unity build runner) connected to the Unity Cloud project. See [CLOUD_BUILD.md](CLOUD_BUILD.md). Unity's current Build Automation documentation lists Unity 6000.3 LTS as supported. The build configuration and service/account setup remain outside this repository workflow.
