"""Check serialized local references and metadata without pretending to run Unity."""
from pathlib import Path
import json
import re
import sys

root = Path(__file__).resolve().parents[1]
assets = root / "Assets"
errors = []
guids = {}
count = 0
for path in sorted(assets.rglob("*")):
    if path.suffix == ".meta":
        continue
    meta = Path(str(path) + ".meta")
    if not meta.exists():
        errors.append(f"Missing meta: {path.relative_to(root)}")
        continue
    match = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
    if not match:
        errors.append(f"Invalid meta: {meta.relative_to(root)}")
        continue
    guid = match[1]
    if guid in guids:
        errors.append(f"Duplicate GUID: {path} / {guids[guid]}")
    guids[guid] = path
    if path.is_file():
        count += 1

for path in assets.rglob("*"):
    if path.suffix not in (".unity", ".prefab", ".asset"):
        continue
    for line in path.read_text().splitlines():
        m = re.search(r"fileID: (\d+), guid: ([0-9a-f]{32}), type: (\d+)", line)
        if not m:
            continue
        file_id, guid, reference_type = int(m[1]), m[2], int(m[3])
        target = guids.get(guid)
        if target is None:
            errors.append(f"Unknown reference in {path.name}: {guid}")
            continue
        if target.suffix == ".cs":
            if file_id != 11500000 or reference_type != 3:
                errors.append(f"Invalid MonoScript reference: {path.name} -> {target.name}")
        else:
            target_ids = {int(n) for n in re.findall(r"^--- !u!\d+ &(\d+)", target.read_text(), re.M)}
            if file_id not in target_ids:
                errors.append(f"Missing fileID {file_id}: {path.name} -> {target.name}")
for path in assets.rglob("*.asmdef"):
    json.loads(path.read_text())
json.loads((root / "Packages/manifest.json").read_text())
print(f"Unity asset metadata/reference check: {count} files, {len(errors)} errors (not an import/build).")
for error in errors:
    print(error, file=sys.stderr)
sys.exit(bool(errors))
