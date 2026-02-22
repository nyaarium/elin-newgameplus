#!/usr/bin/env bash

set -e

echo Building...

ELIN_ROOT="/mnt/elin"

dotnet build -c Release -p:ELIN_INSTALL_PATH="$ELIN_ROOT" -p:DeployPath="$ELIN_ROOT" 2>&1

echo
echo Build successful!
