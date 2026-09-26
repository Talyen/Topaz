#!/usr/bin/env python3
"""Check that Unity assets and folders have their paired metadata files."""

from pathlib import Path
import sys

assets = Path(__file__).resolve().parents[1] / "Assets"
# Unity Performance Testing emits these machine-specific build reports after import,
# without .meta files. They are ignored build artifacts, not authored Resources assets.
generated_reports = {
    Path("Resources/PerformanceTestRunInfo.json"),
    Path("Resources/PerformanceTestRunSettings.json"),
}
missing = [
    path.relative_to(assets)
    for path in assets.rglob("*")
    if path.relative_to(assets) not in generated_reports
    and not path.name.endswith(".meta")
    and not path.name.startswith(".")
    and not Path(f"{path}.meta").is_file()
]

if missing:
    for path in missing:
        print(f"Missing .meta: Assets/{path}", file=sys.stderr)
    sys.exit(1)

print("Unity asset metadata pairs are complete.")
