#!/usr/bin/env bash
set -e

MODE="${1:-release}"

if [ "$MODE" = "debug" ]; then
    echo "Building DEBUG (dev mode)..."
    dotnet build -c Debug 2>&1
else
    echo "Building RELEASE..."
    dotnet build -c Release 2>&1
fi

echo
echo "Build successful! ($MODE)"
