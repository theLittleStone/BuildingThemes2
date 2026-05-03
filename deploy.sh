#!/usr/bin/env bash
# deploy.sh — Build Building Themes 2, stage files, and copy to the game's Mods folder.
#
# Configuration is read from .env in the repo root (copy .env.example to .env).
#
# Usage:
#   ./deploy.sh           → Debug build
#   ./deploy.sh --release → Release build

set -euo pipefail

# ── Load .env if present ────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [[ -f "$SCRIPT_DIR/.env" ]]; then
    # Parse .env manually so that values containing spaces work correctly.
    while IFS='=' read -r _key _value; do
        [[ "$_key" =~ ^[[:space:]]*# ]] && continue  # skip comments
        [[ -z "$_key" ]]               && continue  # skip blank lines
        # Strip optional surrounding single/double quotes from the value
        _value="${_value%\"}"  ; _value="${_value#\"}"
        _value="${_value%\'}"  ; _value="${_value#\'}"
        export "$_key=$_value"
    done < "$SCRIPT_DIR/.env"
    unset _key _value
fi

# ── Configuration ───────────────────────────────────────────────────────────────
CONFIGURATION="Debug"
if [[ "${1:-}" == "--release" ]]; then
    CONFIGURATION="Release"
fi

MOD_NAME="BuildingThemes2"
BUILD_OUT="$SCRIPT_DIR/BuildingThemes/bin/$CONFIGURATION"
WORKSHOP_DIR="$SCRIPT_DIR/Workshop"
PACKAGES="$SCRIPT_DIR/packages"
DIST="$SCRIPT_DIR/dist/$MOD_NAME"

# Mount points — defaults match mount-cities.sh; override via .env if needed.
GAME_MOUNT="${CITIES_GAME_MOUNT:-/mnt/cities_skylines}"
DATA_MOUNT="${CITIES_DATA_MOUNT:-/mnt/cities_skylines_data}"
MODS_DIR="$DATA_MOUNT/Addons/Mods/$MOD_NAME"
LOG_FILE="$GAME_MOUNT/Cities_Data/output_log.txt"

# ── Inject version (release builds only) ─────────────────────────────────────────
if [[ "$CONFIGURATION" == "Release" ]]; then
    MANIFEST="$SCRIPT_DIR/.release-please-manifest.json"
    GH_REPO=$(git remote get-url origin 2>/dev/null | sed 's|.*github.com[:/]\(.*\)\.git|\1|;s|.*github.com[:/]\(.*\)|\1|')
    RELEASE_PR_VERSION=$(gh pr list --repo "${GH_REPO}" --state open --json title \
        --jq '.[].title | select(test("^chore\\(m(ain|aster)\\): release ")) | split(" ")[-1]' \
        2>/dev/null | head -1)
    if [[ -n "$RELEASE_PR_VERSION" ]]; then
        MOD_VERSION="$RELEASE_PR_VERSION"
        echo "Version: $MOD_VERSION (from open Release Please PR)"
    elif [[ -f "$MANIFEST" ]]; then
        MOD_VERSION=$(python3 -c "import json; print(json.load(open('$MANIFEST'))['.'])")
        echo "Version: $MOD_VERSION (from manifest — no open release PR found)"
    fi
    if [[ -n "${MOD_VERSION:-}" ]]; then
        ASSEMBLY_INFO="$SCRIPT_DIR/BuildingThemes/Properties/AssemblyInfo.cs"
        sed -i "s/\[assembly: AssemblyVersion(\"[^\"]*\")\]/[assembly: AssemblyVersion(\"$MOD_VERSION.0\")]/" "$ASSEMBLY_INFO"
        sed -i "s/\[assembly: AssemblyFileVersion(\"[^\"]*\")\]/[assembly: AssemblyFileVersion(\"$MOD_VERSION.0\")]/" "$ASSEMBLY_INFO"
    fi
fi

# ── Build ────────────────────────────────────────────────────────────────────────
echo "Building ($CONFIGURATION)..."
cd "$SCRIPT_DIR"
xbuild BuildingThemes.sln /p:Configuration="$CONFIGURATION" /nologo /verbosity:quiet
echo "Build succeeded."

# ── Stage to dist/ ───────────────────────────────────────────────────────────────
rm -rf "$DIST"
mkdir -p "$DIST"

cp "$BUILD_OUT/BuildingThemes.dll" "$DIST/$MOD_NAME.dll"
cp "$PACKAGES/CitiesHarmony.API.2.1.0/lib/net35/CitiesHarmony.API.dll" "$DIST/"

echo ""
echo "Staged to: $DIST"
ls -lh "$DIST"

# ── Deploy to game (if mounted) ───────────────────────────────────────────────────
if mountpoint -q "$DATA_MOUNT"; then
    echo ""
    echo "Game mount detected — copying to $MODS_DIR ..."
    mkdir -p "$MODS_DIR"
    cp "$DIST/$MOD_NAME.dll"         "$MODS_DIR/"
    cp "$DIST/CitiesHarmony.API.dll" "$MODS_DIR/"
    echo "Done. Files in game Mods folder:"
    ls -lh "$MODS_DIR"
else
    echo ""
    echo "Game mount not detected — skipping in-game deploy."
    echo "Run ./mount-cities.sh first, then re-run this script."
fi

# ── Show log tail (if mounted) ───────────────────────────────────────────────────
echo ""
if [[ -f "$LOG_FILE" ]]; then
    echo "════════════════════════  output_log.txt (last 60 lines)  ════════════════════════"
    tail -n 60 "$LOG_FILE"
    echo "══════════════════════════════════════════════════════════════════════════════════"
else
    echo "Log not found at $LOG_FILE"
    echo "Run ./mount-cities.sh to mount the game share."
fi
