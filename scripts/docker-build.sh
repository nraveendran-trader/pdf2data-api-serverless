#!/bin/bash

set -euo pipefail # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting Docker build process..."
cd ../pdf2data
docker build -t ${COMPONENT_NAME}:${COMPONENT_VERSION} . -f Dockerfile
echo "Docker build process completed."
