#!/usr/bin/env bash
set -euo pipefail

project_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
platform="${1:-mac}"

case "$platform" in
  mac)
    profile="$project_root/Assets/Topaz/Build/Profiles/macOS.asset"
    output="$project_root/Builds/Topaz.app"
    ;;
  windows)
    profile="$project_root/Assets/Topaz/Build/Profiles/Windows.asset"
    output="$project_root/Builds/Topaz.exe"
    ;;
  *)
    echo "Usage: $0 [mac|windows]" >&2
    exit 2
    ;;
esac

command -v unity >/dev/null || { echo "Unity CLI is required" >&2; exit 1; }
mkdir -p "$project_root/Builds"
unity build "$project_root" --profile "$profile" --output-path "$output" --allow-dirty-build --timeout 1200 --no-tail
