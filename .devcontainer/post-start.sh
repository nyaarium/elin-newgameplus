#!/bin/bash
set -e

/var/post-start-base.sh

# Clear cursor editor history cache to prevent language server noise
rm -rf /home/vscode/.cursor-server/data/User/History

# Ran as user anytime the container is started. Does NOT include command > Reload Window
# Place startup steps here...
