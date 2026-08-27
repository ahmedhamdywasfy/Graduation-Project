#!/usr/bin/env bash
#
# apply_person2_cloudinary_fix.sh
#
# Applies ONE fix: resolves the CS0104/CS0738 build error in
# CloudinaryImageStorageService.cs by fully qualifying ImageUploadResult
# to SmartHorse.Application.Common.Models.ImageUploadResult.
#
# Touches exactly one file:
#   src/SmartHorse.Infrastructure/Images/CloudinaryImageStorageService.cs
#
# Usage:
#   tar -xzf Person2_CloudinaryFix.tar.gz
#   chmod +x apply_person2_cloudinary_fix.sh
#   ./apply_person2_cloudinary_fix.sh
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAYLOAD_DIR="$SCRIPT_DIR/payload"
BACKUP_DIR="$SCRIPT_DIR/backup_$(date +%Y%m%d_%H%M%S)"

TARGET_FILE="src/SmartHorse.Infrastructure/Images/CloudinaryImageStorageService.cs"

echo "== Person 2 Cloudinary Fix — apply script =="

# 1. Verify we're running from the project root.
if [ ! -f "SmartHorse.sln" ]; then
  echo "ERROR: SmartHorse.sln not found in the current directory."
  echo "       Run this script from the root of the SmartHorse project"
  echo "       (the directory that directly contains SmartHorse.sln)."
  exit 1
fi

if [ ! -f "$PAYLOAD_DIR/$TARGET_FILE" ]; then
  echo "ERROR: payload is missing the expected file: $TARGET_FILE"
  echo "       Make sure you extracted the full Person2_CloudinaryFix.tar.gz"
  echo "       archive (which contains both this script and payload/)."
  exit 1
fi

echo "Project root confirmed: $(pwd)"

# 2. Backup the existing file.
if [ -f "$TARGET_FILE" ]; then
  mkdir -p "$BACKUP_DIR/$(dirname "$TARGET_FILE")"
  cp -p "$TARGET_FILE" "$BACKUP_DIR/$TARGET_FILE"
  echo "Backed up existing file to: $BACKUP_DIR/$TARGET_FILE"
else
  echo "WARNING: $TARGET_FILE did not exist in this checkout — no backup needed, will be created fresh."
fi

# 3. Extract/copy the fixed file, preserving permissions of the target
#    location where possible.
mkdir -p "$(dirname "$TARGET_FILE")"
cp -p "$PAYLOAD_DIR/$TARGET_FILE" "$TARGET_FILE"
echo "Replaced: $TARGET_FILE"

echo ""
echo "== SUCCESS =="
echo "1 file changed. Backup (if any) is in: $BACKUP_DIR"
echo ""
echo "This script only copies the file — it does not run dotnet for you."
echo "Next steps:"
echo ""
echo "  dotnet restore"
echo "  dotnet build"
echo "  dotnet test"
