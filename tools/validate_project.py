#!/usr/bin/env python3
"""Dependency-free static preflight for the GLIMP TAG Unity Quest project.

This checks repository structure and pinned project metadata. It deliberately
DOES NOT claim to compile C#, open Unity, validate generated scene assets, or
produce an Android APK.
"""
from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

EXPECTED_PACKAGES = {
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

REQUIRED_FILES = [
    "README.md",
    "CLOUD_BUILD.md",
    "BUILD_VALIDATION.md",
    "QUEST_INSTALL.md",
    "Packages/manifest.json",
    "ProjectSettings/ProjectVersion.txt",
    "Assets/Editor/GlimpTagProjectBuilder.cs",
    "Assets/Editor/GlimpTagNetworkPrefabBuilder.cs",
    "Assets/Editor/GlimpTagBuildValidation.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagRuntimeBootstrap.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagLocomotion.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagNetworkPlayer.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagRelaySession.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagVoiceChat.cs",
    "Assets/GLIMP_TAG/Scripts/GlimpTagWristMenu.cs",
]

@dataclass
class Finding:
    level: str
    code: str
    message: str


def read_text(path: Path, findings: list[Finding], label: str) -> str | None:
    try:
        return path.read_text(encoding="utf-8")
    except FileNotFoundError:
        findings.append(Finding("error", "missing-file", f"Required file is missing: {label}"))
    except UnicodeDecodeError as exc:
        findings.append(Finding("error", "invalid-encoding", f"{label} is not UTF-8: {exc}"))
    except OSError as exc:
        findings.append(Finding("error", "read-error", f"Could not read {label}: {exc}"))
    return None


def check_contains(text: str | None, tokens: list[str], label: str,
                   findings: list[Finding], code: str) -> None:
    if text is None:
        return
    missing = [token for token in tokens if token not in text]
    if missing:
        findings.append(Finding(
            "error", code,
            f"{label} is missing expected integration markers: {', '.join(missing)}"))
    else:
        findings.append(Finding("pass", code, f"{label} contains expected integration markers."))


def validate(root: Path) -> dict[str, Any]:
    findings: list[Finding] = []

    for relative in REQUIRED_FILES:
        path = root / relative
        if path.is_file():
            findings.append(Finding("pass", "required-file", f"Found {relative}"))
        else:
            findings.append(Finding("error", "required-file", f"Missing {relative}"))

    version_text = read_text(root / "ProjectSettings/ProjectVersion.txt", findings,
                              "ProjectSettings/ProjectVersion.txt")
    editor_version = None
    if version_text is not None:
        match = re.search(r"^m_EditorVersion:\s*(\S+)\s*$", version_text, re.MULTILINE)
        if match:
            editor_version = match.group(1)
            if editor_version.startswith("6000.3."):
                findings.append(Finding("pass", "unity-version",
                    f"Unity Editor version is pinned to {editor_version} (6000.3 LTS line)."))
            else:
                findings.append(Finding("warning", "unity-version",
                    f"Project uses Unity {editor_version}; re-check Unity Build Automation support before building."))
        else:
            findings.append(Finding("error", "unity-version",
                "ProjectVersion.txt does not contain a valid m_EditorVersion entry."))

    manifest_text = read_text(root / "Packages/manifest.json", findings,
                              "Packages/manifest.json")
    manifest: dict[str, Any] = {}
    if manifest_text is not None:
        try:
            parsed = json.loads(manifest_text)
            if not isinstance(parsed, dict) or not isinstance(parsed.get("dependencies"), dict):
                raise ValueError"(expected a top-level dependencies object")
            manifest = parsed["dependencies"]
            findings.append(Finding("pass", "manifest-json", "Packages/manifest.json is valid JSON."))
        except (json.JSONDecodeError, ValueError) as exc:
            findings.append(Finding("error", "manifest-json", f"Packages/manifest.json is invalid: {exc}"))

    for package, expected in EXPECTED_PACKAGES.items():
        actual = manifest.get(package)
        if actual == expected:
            findings.append(Finding("pass", "package-pin", f"{package} is pinned to {expected}."))
        elif actual is None:
            findings.append(Finding("error", "package-pin", f"Required package {package} is missing."))
        else:
            findings.append(Finding("error", "package-pin",
                f"{package} is {actual}, expected pinned version {expected}."))

    project_builder = read_text(root / "Assets/Editor/GlimpTagProjectBuilder.cs", findings,
                                "GlimpTagProjectBuilder.cs")
    check_contains(project_builder,
        ["IPreprocessBuildWithReport", "EnsureProjectReady", "ConfigureAndroidPlayer", "ConfigureOpenXR"],
        "GlimpTagProjectBuilder.cs", findings, "editor-builder")

    prefab_builder = read_text(root / "Assets/Editor/GlimpTagNetworkPrefabBuilder.cs", findings,
                               "GlimpTagNetworkPrefabBuilder.cs")
    check_contains(prefab_builder,
        ["GlimpTagWristMenu", "GlimpTagVoiceChat", "GlimpTagNetworkPlayer", "ConfigureNetworkScene"],
        "GlimpTagNetworkPrefabBuilder.cs", findings, "prefab-builder")

    build_validator = read_text(root / "Assets/Editor/GlimpTagBuildValidation.cs", findings,
                                "GlimpTagBuildValidation.cs")
    check_contains(build_validator,
        ["IPreprocessBuildWithReport", "BuildFailedException", "AndroidArchitecture.ARM64",
         "buildAppBundle", "ValidateOpenXR"],
        "GlimpTagBuildValidation.cs", findings, "unity-build-validator")

    cloud_doc = read_text(root / "CLOUD_BUILD.md", findings, "CLOUD_BUILD.md")
    if cloud_doc is not None:
        if editor_version and editor_version in cloud_doc:
            findings.append(Finding("pass", "cloud-doc-version",
                "CLOUD_BUILD.md references the same Unity Editor version as ProjectVersion.txt."))
        else:
            findings.append(Finding("warning", "cloud-doc-version",
                "CLOUD_BUILD.md may not match the pinned Unity Editor version."))
        if "No cloud build has been started" in cloud_doc or "No cloud build has been started or completed" in cloud_doc:
            findings.append(Finding("pass", "honest-build-status",
                "Cloud-build documentation clearly states that no APK build is verified yet."))
        else:
            findings.append(Finding("warning", "honest-build-status",
                "Confirm CLOUD_BUILD.md accurately states the current APK/build status."))

    # These assets are intentionally generated by the Editor automation; their
    # absence from Git is a warning, not a static-preflight failure.
    generated_assets = [
        root / "Assets/Scenes/GLIMP_TAG.unity",
        root / "Assets/Resources/GLIMP_TAG/NetworkPlayer.prefab",
    ]
    missing_generated = [str(path.relative_to(root)) for path in generated_assets if not path.is_file()]
    if missing_generated:
        findings.append(Finding("warning", "editor-generated-assets",
            "Not committed (expected to be generated by Unity Editor automation): " +
            ", ".join(missing_generated)))
    else:
        findings.append(Finding("pass", "editor-generated-assets",
            "Generated scene and player prefab are present in the repository."))

    errors = sum(1 for item in findings if item.level == "error")
    warnings = sum(1 for item in findings if item.level == "warning")
    passed = sum(1 for item in findings if item.level == "pass")
    return {
        "project": "GLIMP TAG",
        "unity_editor": editor_version,
        "result": "PASS" if errors == 0 else "FAIL",
        "summary": {"passed": passed, "warnings": warnings, "errors": errors},
        "findings": [asdict(item) for item in findings],
        "scope": "Static repository and metadata checks only; no Unity compilation, APK build, or headset test.",
    }


def write_github_summary(report: dict[str, Any]) -> None:
    summary_path = os.environ.get("GITHUB_STEP_SUMMARY")
    if not summary_path:
        return
    lines = [
        "# GLIMP TAG static project preflight",
        "",
        f"**Result: {report['result']}**",
        "",
        f"Unity Editor: `{report['unity_editor'] or 'not detected'}`",
        "",
        f"Passed: {report['summary']['passed']} | Warnings: {report['summary']['warnings']} | Errors: {report['summary']['errors']}",
        "",
        "## Findings",
        "",
    ]
    for finding in report["findings"]:
        icon = {"pass": "PASS", "warning": "WARN", "error": "FAIL"}[finding["level"]]
        lines.append(f"- {icon} **{finding['code']}** - {finding['message']}")
    lines += ["", f"> Scope: {report['scope']}", ""]
    with open(summary_path, "a", encoding="utf-8") as stream:
        stream.write("\n".join(lines))


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1],
                        help="Unity project root (defaults to repository root).")
    parser.add_argument("--report", type=Path,
                        help="Optional JSON report output path.")
    args = parser.parse_args()

    root = args.root.resolve()
    report = validate(root)
    rendered = json.dumps(report, indent=2, ensure_ascii=False) + "\n"
    print(rendered, end="")

    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(rendered, encoding="utf-8")
    write_github_summary(report)
    return 0 if report["result"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())
