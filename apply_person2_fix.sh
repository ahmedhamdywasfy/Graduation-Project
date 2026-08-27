#!/usr/bin/env bash
#
# apply_person2_fix.sh
#
# Applies the Person 2 Fix Sprint changes on top of an existing SmartHorse
# project checkout. Fixes ONLY:
#   1. The CS0104/CS0738 build error in CloudinaryImageStorageService.cs
#   2. Verbose SQL/parameter logging enabled by default
#   3. The missing EF Core migration for Horses/Breeds/Colors/Genders/
#      HorseStatuses/HorseImages/OwnershipHistories
#
# Usage:
#   tar -xzf Person2_FixSprint.tar.gz
#   chmod +x apply_person2_fix.sh
#   ./apply_person2_fix.sh
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PAYLOAD_DIR="$SCRIPT_DIR/payload"
BACKUP_DIR="$SCRIPT_DIR/backup_$(date +%Y%m%d_%H%M%S)"

echo "== Person 2 Fix Sprint — apply script =="

# 1. Verify we're running from the project root.
if [ ! -f "SmartHorse.sln" ]; then
  echo "ERROR: SmartHorse.sln not found in the current directory."
  echo "       Run this script from the root of the SmartHorse project"
  echo "       (the directory that directly contains SmartHorse.sln)."
  exit 1
fi

if [ ! -d "$PAYLOAD_DIR" ]; then
  echo "ERROR: payload/ directory not found next to this script."
  echo "       Make sure you extracted the full Person2_FixSprint.tar.gz"
  echo "       archive (which contains both this script and payload/)."
  exit 1
fi

echo "Project root confirmed: $(pwd)"
echo "Backups will be written to: $BACKUP_DIR"
mkdir -p "$BACKUP_DIR"

# List of files this fix touches, relative to the project root.
FILES=(
  "src/SmartHorse.API/appsettings.json"
  "src/SmartHorse.Infrastructure/DependencyInjection.cs"
  "src/SmartHorse.Infrastructure/Images/CloudinaryImageStorageService.cs"
  "src/SmartHorse.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs"
)

NEW_FILES=(
  "src/SmartHorse.Infrastructure/Migrations/20260826195303_Person2_HorseCore_And_Images.cs"
  "src/SmartHorse.Infrastructure/Migrations/20260826195303_Person2_HorseCore_And_Images.Designer.cs"
)

echo ""
echo "-- Backing up and replacing modified files --"
for f in "${FILES[@]}"; do
  if [ ! -f "$PAYLOAD_DIR/$f" ]; then
    echo "ERROR: payload is missing expected file: $f"
    exit 1
  fi

  if [ -f "$f" ]; then
    mkdir -p "$BACKUP_DIR/$(dirname "$f")"
    cp -p "$f" "$BACKUP_DIR/$f"
    echo "  backed up: $f"
  else
    echo "  WARNING: $f did not exist in this checkout — will be created fresh (no backup needed)."
  fi

  mkdir -p "$(dirname "$f")"
  # Preserve destination permissions if the file already existed; otherwise
  # default (umask) permissions for a newly created file are fine here since
  # none of these files need to be executable.
  cp -p "$PAYLOAD_DIR/$f" "$f"
  echo "  replaced:  $f"
done

echo ""
echo "-- Adding new migration files --"
for f in "${NEW_FILES[@]}"; do
  if [ ! -f "$PAYLOAD_DIR/$f" ]; then
    echo "ERROR: payload is missing expected new file: $f"
    exit 1
  fi

  if [ -f "$f" ]; then
    echo "ERROR: $f already exists — refusing to overwrite a file that wasn't"
    echo "       backed up. Investigate before re-running."
    exit 1
  fi

  mkdir -p "$(dirname "$f")"
  cp -p "$PAYLOAD_DIR/$f" "$f"
  echo "  added: $f"
done

echo ""
echo "== SUCCESS =="
echo "6 files touched (4 replaced with backups in $BACKUP_DIR, 2 new)."
echo ""
echo "This script does NOT run dotnet build/test/ef for you — it only copies"
echo "files. Required next steps, in order:"
echo ""
echo "  dotnet restore"
echo "  dotnet build"
echo "  dotnet test"
echo "  dotnet ef migrations add VerifySnapshot --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API"
echo "    (expected: EF reports no/near-empty changes — see FIX_SPRINT_REPORT.md"
echo "     for why this step matters and what a non-trivial diff would mean)"
echo "  dotnet ef database update --project src/SmartHorse.Infrastructure --startup-project src/SmartHorse.API"
echo ""
echo "Read FIX_SPRINT_REPORT.md before treating any of this as verified —"
echo "these changes were made without a local .NET SDK/SQL Server available,"
echo "so none of the above commands have actually been run yet."
