#!/usr/bin/env bash

set -e

echo Building...

dotnet build -c Release 2>&1

echo
echo Build successful!
