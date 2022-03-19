#!/bin/bash
#
# Script to update tools
#
cd "$(dirname "$0")"

toolAppImage="appimagetool-x86_64"

echo "1. Cleanup"
rm -rf "$toolAppImage" 2>/dev/null

echo "2. Get AppImage toolkit"
wget "https://github.com/AppImage/AppImageKit/releases/download/continuous/$toolAppImage.AppImage"
chmod a+x "$toolAppImage.AppImage"

"./$toolAppImage.AppImage" --appimage-extract
rm -f "$toolAppImage.AppImage"
mv -f "squashfs-root" "$toolAppImage"

echo "3. Complete"