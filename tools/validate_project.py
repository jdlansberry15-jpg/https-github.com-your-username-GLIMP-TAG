#!/usr/bin/env python3
"""Static repository preflight. This does not compile Unity or build an APK."""
import argparse
import json
import os
import re
import sys
from pathlib import Path

PACKAGES = {
    "com.unity.inputsystem": "1.20.1",
    "com.unity.netcode.gameobjects": "2.13.3",
    "com.unity.services.multiplayer": "2.4.1",
    "com.unity.transport": "2.7.5",
    "com.unity.xr.hands": "1.7.3",
    "com.unity.xr.management": "4.6.1",
    "com.unity.xr.meta-openxr": "2.7.0",
    "com.unity.xr.openxr": "1.16.1",
    "com.unity.services.vivox": "16.12.1",
}
FILES = [
    "README.md", "CLOUD_BUILD.md", "BUILD_VALIDATION.md", "QUEST_INSTALL.md", "TAG_VALIDATION.md",
    "LOCOMOTION_STABILITY.md", "PACKAGE_COMPATIBILITY.md", "ROUND_RESET.md", "FALL_RECOVERY.md",
    "Packages/manifest.json", "ProjectSettings/ProjectVersion.txt",
    "Assets/Editor/GlimpTagProjectBuilder.cs",
    "Assets/Editor/GlimpTagNetworkPrefabBuilder.cs",
    "Assets/Editor/GlimpTagBuildValidation.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagRuntimeBootstrap.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagLocomotion.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagNetworkPlayer.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagNetworkTagger.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagNetworkPlayerRig.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagRelaySession.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagRelaySession.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagVoiceChat.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagWristMenu.cs",
]


def validate(root):
    findings = []

    def add(level, code, message):
        findings.append({"level": level, "code": code, "message": message})

    texts = {}
    for relative in FILES:
        path = root / relative
        if path.is_file():
            add("pass", "required-file", "Found " + relative)
            try:
                texts[relative] = path.read_text(encoding="utf-8")
            except (OSError, UnicodeDecodeError) as exc:
                add("error", "read-file", relative + ": " + str(exc))
        else:
            add("error", "required-file", "Missing " + relative)

    version_text = texts.get("ProjectSettings/ProjectVersion.txt", "")
    match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", version_text, re.M)
    editor_version = match.group(1) if match else None
    if not editor_version:
        add("error", "unity-version", "ProjectVersion.txt has no valid m_EditorVersion.")
    elif editor_version.startswith("6000.3."):
        add("pass", "unity-version", "Unity " + editor_version + " is on the documented 6000.3 LTS line.")
    else:
        add("warning", "unity-version", "Unity " + editor_version + " is outside the pinned 6000.3 LTS line; re-check UBA support.")

    manifest = {}
    try:
        parsed = json.loads(texts.get("Packages/manifest.json", ""))
        manifest = parsed.get("dependencies", {})
        if not isinstance(manifest, dict):
            raise ValueError("dependencies must be an object")
        add("pass", "manifest-json", "Packages/manifest.json is valid JSON.")
    except (json.JSONDecodeError, ValueError, AttributeError) as exc:
        add("error", "manifest-json", "Packages/manifest.json is invalid: " + str(exc))

    for name, expected in PACKAGES.items():
        actual = manifest.get(name)
        if actual == expected:
            add("pass", "package-pin", name + " pinned to " + expected)
        else:
            add("error", "package-pin", name + " is " + repr(actual) + "; expected " + expected)

    checks = {
        "Assets/Editor/GlimpTagProjectBuilder.cs": ["IPreprocessBuildWithReport", "EnsureProjectReady", "ConfigureAndroidPlayer", "ConfigureOpenXR"],
        "Assets/Editor/GlimpTagNetworkPrefabBuilder.cs": ["GlimpTagWristMenu", "GlimpTagVoiceChat", "GlimpTagNetworkPlayer", "ConfigureNetworkScene"],
        "Assets/Editor/GlimpTagBuildValidation.cs": ["IPreprocessBuildWithReport", "BuildFailedException", "AndroidArchitecture.ARM64", "buildAppBundle", "ValidateOpenXR"],
    }
    for relative, markers in checks.items():
        source = texts.get(relative, "")
        missing = [marker for marker in markers if marker not in source]
        if missing:
            add("error", "integration-markers", relative + " missing: " + ", ".join(missing))
        else:
            add("pass", "integration-markers", relative + " has expected integration markers.")

    locomotion = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagLocomotion.cs", "")
    locomotion_markers = [
        "hasLeftHistory", "hasRightHistory", "OverlapSphereNonAlloc",
        "SphereCastNonAlloc", "IsOwnCollider", "activeInHierarchy",
        "RecenterHandHistory", "speedStepLimit", "stepDistanceLimit",
    ]
    missing_locomotion = [marker for marker in locomotion_markers if marker not in locomotion]
    if missing_locomotion:
        add("error", "locomotion-safety", "Locomotion source is missing safety paths: " + ", ".join(missing_locomotion))
    elif "Physics.CheckSphere(" in locomotion or "Physics.SphereCast(" in locomotion:
        add("error", "locomotion-safety", "Locomotion reverted to allocating/legacy contact queries.")
    else:
        add("pass", "locomotion-safety", "Hand tracking reacquisition, own-collider filtering, and non-alloc contact queries are present.")

    arena_source = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagRuntimeBootstrap.cs", "")
    relay_source = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagRelaySession.cs", "")
    spawn_markers = ["public const int SpawnPointCount = 8", "new Transform[SpawnPointCount]", "spawnRadius = 20f", "Quaternion.LookRotation"]
    missing_spawn = [marker for marker in spawn_markers if marker not in arena_source]
    if missing_spawn or "Range(1, 7)" not in relay_source or "maxRemotePlayers = 7" not in relay_source:
        add("error", "spawn-capacity", "Eight-player spawn layout does not match Relay capacity: " + ", ".join(missing_spawn))
    else:
        add("pass", "spawn-capacity", "Eight ground spawn slots match one host plus up to seven Relay guests.")

    network_player = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagNetworkPlayer.cs", "")
    network_rig = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagNetworkPlayerRig.cs", "")
    round_markers = ["[Rpc(SendTo.Owner)]", "ResetOwnerForRoundRpc", "players[i].ResetOwnerForRoundRpc();", "RoundPhase.Value = RoundActive"]
    missing_round = [marker for marker in round_markers if marker not in network_player]
    if missing_round or "ResetOwnerToSpawn" not in network_rig or "RecenterHandHistory" not in network_rig:
        details = missing_round + [marker for marker in ["ResetOwnerToSpawn", "RecenterHandHistory"] if marker not in network_rig]
        add("error", "round-reset", "Owner-side round reset is incomplete: " + ", ".join(details))
    else:
        add("pass", "round-reset", "Each new round requests an owner-side spawn reset and clears locomotion hand history.")

    tagger_source = texts.get("Assets/GLIMP_TAG/Scripts/GlimpTagNetworkTagger.cs", "")
    prefab_builder = texts.get("Assets/Editor/GlimpTagNetworkPrefabBuilder.cs", "")
    tag_doc = texts.get("TAG_VALIDATION.md", "")
    tag_markers = [
        "serverHandTagDistance", "serverMaxHandReach",
        "attackerPoseSync.AvatarPose.Value", "targetPoseSync.AvatarPose.Value",
        "RequestTagRpc(ulong targetClientId, bool isLeftHand",
    ]
    tagger_markers = [
        "private bool isLeftHand", "ConfigureHand(bool leftHand)",
        "RequestTagRpc(target.OwnerClientId, isLeftHand)",
        "TryTagTargetOnServer(target.OwnerClientId, tagger.OwnerClientId, isLeftHand)",
    ]
    builder_markers = ["leftTagger.ConfigureHand(true)", "rightTagger.ConfigureHand(false)"]
    missing_tag = [marker for marker in tag_markers if marker not in network_player]
    missing_tagger = [marker for marker in tagger_markers if marker not in tagger_source]
    missing_builder = [marker for marker in builder_markers if marker not in prefab_builder]
    if missing_tag or missing_tagger or missing_builder:
        add("error", "server-tag-validation",
            "Hand-pose tag validation is incomplete: " +
            ", ".join(missing_tag + missing_tagger + missing_builder))
    elif "not been Unity-compiled or Quest-tested" in tag_doc:
        add("pass", "server-tag-validation",
            "Tag requests identify the attacking hand and the host checks its latest network pose and reach.")
    else:
        add("warning", "server-tag-validation-doc",
            "TAG_VALIDATION.md should explicitly state Unity/Quest testing limits.")

    rig_fall = network_rig
    fall_markers = ["fallResetHeight", "fallResetCooldown", "Time.unscaledTime", "ResetOwnerToSpawn()", "IsOwner"]
    missing_fall = [marker for marker in fall_markers if marker not in rig_fall]
    fall_doc = texts.get("FALL_RECOVERY.md", "")
    if missing_fall:
        add("error", "fall-recovery", "Owner fall recovery is missing safety paths: " + ", ".join(missing_fall))
    elif "not been Unity-compiled or Quest-tested" in fall_doc:
        add("pass", "fall-recovery", "Owner-only fall recovery returns dropped players to their spawn and documents device-test limits.")
    else:
        add("warning", "fall-recovery-doc", "Review FALL_RECOVERY.md for explicit device-test limitations.")

    round_doc = texts.get("ROUND_RESET.md", "")
    if "owner-authoritative" in round_doc and "not been Unity-compiled or Quest-tested" in round_doc:
        add("pass", "round-reset-doc", "Round reset notes document the authority model and test limitations.")
    else:
        add("warning", "round-reset-doc", "Review ROUND_RESET.md for owner-authority and device-test caveats.")

    locomotion_doc = texts.get("LOCOMOTION_STABILITY.md", "")
    if "Quest" in locomotion_doc and "not measured Quest performance results" in locomotion_doc and "0.089 m at 90 Hz" in locomotion_doc:
        add("pass", "locomotion-doc", "Locomotion notes document Quest tuning as unverified.")
    else:
        add("warning", "locomotion-doc", "Review LOCOMOTION_STABILITY.md for device-test caveats.")

    package_doc = texts.get("PACKAGE_COMPATIBILITY.md", "")
    package_markers = ["6000.3.26f1", "2.13.3", "2.14.0", "OverlapSphereNonAlloc", "SphereCastNonAlloc"]
    missing_package_markers = [marker for marker in package_markers if marker not in package_doc]
    cloud_doc = texts.get("CLOUD_BUILD.md", "")
    if missing_package_markers:
        add("error", "package-compatibility", "PACKAGE_COMPATIBILITY.md missing: " + ", ".join(missing_package_markers))
    elif "remains a pre-release package for Unity 6000.3" in cloud_doc:
        add("error", "package-compatibility", "CLOUD_BUILD.md contains the outdated NGO 2.13.3 lifecycle note.")
    elif "6000.3.26f1" in cloud_doc:
        add("pass", "package-compatibility", "NGO 2.13.3 lifecycle note matches Unity 6000.3.26f1 release notes.")
    else:
        add("warning", "package-compatibility", "Review the documented NGO package lifecycle for Unity 6000.3.")

    if editor_version and editor_version in cloud_doc:
        add("pass", "cloud-doc-version", "CLOUD_BUILD.md matches ProjectVersion.txt.")
    else:
        add("warning", "cloud-doc-version", "CLOUD_BUILD.md may not match the pinned Editor version.")
    if "No cloud build has been started" in cloud_doc:
        add("pass", "honest-build-status", "Cloud-build guide does not claim an APK exists.")
    else:
        add("warning", "honest-build-status", "Re-check the documented APK/build status.")

    generated = ["Assets/Scenes/GLIMP_TAG.unity", "Assets/Resources/GLIMP_TAG/NetworkPlayer.prefab"]
    absent = [name for name in generated if not (root / name).is_file()]
    if absent:
        add("warning", "editor-generated-assets", "Generated by Unity Editor automation, not committed: " + ", ".join(absent))
    else:
        add("pass", "editor-generated-assets", "Generated scene and prefab are committed.")

    errors = sum(item["level"] == "error" for item in findings)
    return {
        "project": "GLIMP TAG",
        "unity_editor": editor_version,
        "result": "PASS" if errors == 0 else "FAIL",
        "summary": {
            "passed": sum(item["level"] == "pass" for item in findings),
            "warnings": sum(item["level"] == "warning" for item in findings),
            "errors": errors,
        },
        "findings": findings,
        "scope": "Static repository and metadata checks only; no Unity compilation, APK build, or headset test.",
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()
    report = validate(args.root.resolve())
    output = json.dumps(report, indent=2) + "\n"
    print(output, end="")
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(output, encoding="utf-8")
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        lines = ["# GLIMP TAG static project preflight", "", "**Result: " + report["result"] + "**", "",
                 "Unity Editor: " + str(report["unity_editor"] or "not detected"), "",
                 "Passed: {passed} | Warnings: {warnings} | Errors: {errors}".format(**report["summary"]), "", "## Findings", ""]
        for item in report["findings"]:
            lines.append("- {0} **{1}** - {2}".format(item["level"].upper(), item["code"], item["message"]))
        lines.extend(["", "> " + report["scope"], ""])
        with open(summary, "a", encoding="utf-8") as stream:
            stream.write("\n".join(lines))
    return 0 if report["result"] == "PASS" else 1


if __name__ == "__main__":
    sys.exit(main())
