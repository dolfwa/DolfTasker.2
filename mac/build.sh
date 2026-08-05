#!/bin/bash
# Builds Dolftasker.2.app using the Swift compiler from the Xcode Command Line
# Tools. No Xcode project, no package manager, no dependencies.
#
#   xcode-select --install     # one-time, if swiftc is missing
#   ./build.sh

set -e
cd "$(dirname "$0")"

APP_NAME="Dolftasker.2"
BUNDLE_ID="com.dolfwa.dolftasker2"
APP="build/${APP_NAME}.app"

if ! command -v swiftc >/dev/null 2>&1; then
    echo "swiftc not found. Install the Xcode Command Line Tools:"
    echo "    xcode-select --install"
    exit 1
fi

rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS"
mkdir -p "$APP/Contents/Resources"

# main.swift must come last: Swift requires the file with top-level code to be
# the final one passed to the compiler.
# No -target: swiftc builds for the host architecture, so this works unchanged
# on both Apple Silicon and Intel.
swiftc -O \
    -o "$APP/Contents/MacOS/${APP_NAME}" \
    Sources/Macro.swift \
    Sources/Design.swift \
    Sources/InputEngine.swift \
    Sources/ActionsWindow.swift \
    Sources/MainWindow.swift \
    Sources/main.swift

cat > "$APP/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>${BUNDLE_ID}</string>
    <key>CFBundleExecutable</key>
    <string>${APP_NAME}</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
    <key>NSAppleEventsUsageDescription</key>
    <string>Dolftasker replays recorded mouse and keyboard input.</string>
</dict>
</plist>
PLIST

# macOS ties Accessibility permission to the code signature. An ad-hoc signature
# keeps that grant stable across rebuilds; without it you must re-approve the app
# in System Settings every single build.
codesign --force --deep --sign - "$APP" 2>/dev/null || \
    echo "warning: ad-hoc codesign failed; you may need to re-approve Accessibility after each build"

echo "Build OK -> ${APP}"
echo
echo "Run it with:  open \"${APP}\""
echo "First launch will ask for Accessibility access (required for input capture)."
