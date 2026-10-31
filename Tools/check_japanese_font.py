"""Check actual bundled font data, provenance checksum and UI glyph coverage.

Requires fontTools for this development check; the Unity game does not depend on Python.
This does not replace a Unity import or rendering test.
"""
from pathlib import Path
import hashlib
import json
import re
from fontTools.ttLib import TTFont

root = Path(__file__).resolve().parents[1]
path = root / "Assets/ThirdParty/Noto/Resources/HoloDebut/NotoSansJP-Regular.otf"
expected = "dff723ba59d57d136764a04b9b2d03205544f7cd785a711442d6d2d085ac5073"
assert hashlib.sha256(path.read_bytes()).hexdigest() == expected, "Bundled font differs from recorded official download"
assert "SIL OPEN FONT LICENSE Version 1.1" in (root / "Assets/ThirdParty/Noto/LICENSE.txt").read_text()
characters = set()
for folder in ["Assets/HoloDebut/Presentation", "Assets/HoloDebut/Runtime"]:
    for source in (root / folder).glob("*.cs"):
        for match in re.finditer(r'"((?:\\.|[^"\\])*)"', source.read_text()):
            text = json.loads('"' + match[1] + '"')
            characters.update(c for c in text if ord(c) >= 128)
for source in (root / "Assets/HoloDebut/Data").glob("*.asset"):
    for line in source.read_text().splitlines():
        if re.match(r"  (title|text|japaneseName|displayName|description):", line):
            characters.update(c for c in line if ord(c) >= 128)
with TTFont(path) as font:
    cmap = font.getBestCmap()
    missing = sorted(c for c in characters if ord(c) not in cmap)
    assert not missing, "Missing UI glyphs: " + "".join(missing)
    assert "Noto Sans JP" in font["name"].getDebugName(1)
print(f"Japanese font: SHA-256 and OFL verified; {len(characters)} non-ASCII UI characters covered. Unity rendering unverified.")
