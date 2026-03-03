#!/bin/bash

# Post-start script for devcontainer

set -e

# Clear cursor editor history cache to prevent language server noise
rm -rf /home/vscode/.cursor-server/data/User/History
