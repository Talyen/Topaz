#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
command -v unity >/dev/null || { echo "Unity CLI is required" >&2; exit 1; }
python3 "$project_root/scripts/check-assets.py"
python3 "$project_root/scripts/check-asset-review.py"

mkdir -p "$project_root/TestResults"
unity test "$project_root" --mode EditMode --report-format junit --output "$project_root/TestResults/editmode.xml" --timeout 900
unity test "$project_root" --mode PlayMode --report-format junit --output "$project_root/TestResults/playmode.xml" --timeout 900
"$project_root/scripts/build.sh" mac
git -C "$project_root" diff --check
