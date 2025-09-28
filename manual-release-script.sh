#!/usr/bin/env bash
set -e

# Config
APP_NAME="PockitBook"
OUTPUT_BASE="$HOME/Documents/PockitBook-Releases"
RUNTIME="win-x64"   # Change to win-x86 if needed
CONFIGURATION="Release"

# Ensure base directory exists
mkdir -p "$OUTPUT_BASE"

# Figure out next release number
LAST_RELEASE=$(ls -1 "$OUTPUT_BASE" | grep -E '^release-[0-9]+$' | sort -V | tail -n 1)

if [[ -z "$LAST_RELEASE" ]]; then
    NEXT_RELEASE="release-1"
else
    LAST_NUM=${LAST_RELEASE#release-}
    NEXT_NUM=$((LAST_NUM + 1))
    NEXT_RELEASE="release-$NEXT_NUM"
fi

OUTPUT_DIR="$OUTPUT_BASE/$NEXT_RELEASE"

# Ensure release directory exists
mkdir -p "$OUTPUT_DIR"

echo "Building $APP_NAME into $OUTPUT_DIR"

dotnet publish \
    -c "$CONFIGURATION" \
    -r "$RUNTIME" \
    --self-contained true \
    -o "$OUTPUT_DIR"

echo "✅ Build complete: $OUTPUT_DIR"
