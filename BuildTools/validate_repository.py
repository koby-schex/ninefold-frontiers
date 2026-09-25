#!/usr/bin/env python3
"""Repository integrity only; does not run Unity or compile C#."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors = []

def check(ok, message):
    if not ok:
        errors.append(message)

check("m_EditorVersion: 6000.3.21f1" in (ROOT / "ProjectSettings/ProjectVersion.txt").read_text(), "Unexpected Unity version")
manifest = json.loads((ROOT / "Packages/manifest.json").read_text())
check(manifest["dependencies"].get("com.unity.render-pipelines.universal") == "17.3.0", "Unexpected URP version")
for path in ROOT.glob("Assets/**/*.asmdef"):
    data = json.loads(path.read_text())
    if data["name"] == "Ninefold.Core":
        check(data.get("noEngineReferences") is True, "Core must not reference UnityEngine")
        check(data.get("references") == [], "Core must not depend on presentation")
seen = {}
for path in (ROOT / "Assets").rglob("*"):
    if path.suffix == ".meta":
        check(Path(str(path)[:-5]).exists(), f"Orphan metadata: {path.relative_to(ROOT)}")
        match = re.search(r"^guid: ([0-9a-f]{32})$", path.read_text(), re.MULTILINE)
        check(match is not None, f"Invalid GUID: {path.relative_to(ROOT)}")
        if match:
            guid = match.group(1)
            check(guid not in seen, f"Duplicate GUID: {path.relative_to(ROOT)} / {seen.get(guid)}")
            seen[guid] = str(path.relative_to(ROOT))
    else:
        check(Path(str(path) + ".meta").exists(), f"Missing metadata: {path.relative_to(ROOT)}")
tracked = subprocess.check_output(["git", "ls-files", "-z"], cwd=ROOT).decode().split("\0")
for name in filter(None, tracked):
    check(Path(name).parts[0].lower() not in {"library", "temp", "obj", "logs", "usersettings", "build", "builds"}, f"Generated directory tracked: {name}")
    check(Path(name).suffix.lower() not in {".p12", ".pfx", ".mobileprovision", ".keystore", ".jks", ".p8", ".pem", ".key"}, f"Signing/key file tracked: {name}")
for sample in ("SourceArt/example.fbx", "SourceArt/example.blend", "Assets/Ninefold/example.png"):
    result = subprocess.check_output(["git", "check-attr", "filter", "--", sample], cwd=ROOT).decode().strip()
    check(result.endswith(": lfs"), f"Missing LFS rule: {sample}")
if errors:
    raise SystemExit("Repository checks failed:\n" + "\n".join(errors))
print(f"Repository checks passed ({len(seen)} Unity metadata GUIDs). Unity import/build not executed.")
