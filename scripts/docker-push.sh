#!/usr/bin/env bash

set -euo pipefail
source "./parameters.sh"

#construct image name and tag.  Format: reg-cg:pdf2data-1.0.0
LOCAL_IMAGE="${COMPONENT_NAME}:${COMPONENT_VERSION}"
ECR_IMAGE="${DEPARTMENT_NAME}-${PROJECT_NAME}:${COMPONENT_NAME}-${COMPONENT_VERSION}"

echo "Starting to push local image ${LOCAL_IMAGE} as ${ECR_IMAGE}..."

# Get temporary auth token from ECR and pipes it into Docker to perform login
# fun fact: ECR always uses 'AWS' as the username and reads the password from stdin from previous command via pipe
aws ecr get-login-password \
    --region "${REGION}" | docker login \
    --username AWS --password-stdin "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com"

# # Tag the local image with the ECR repository URI
echo "Tagging image..."
docker tag "${LOCAL_IMAGE}" "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${ECR_IMAGE}"

# echo "Pushing image..."
docker push "${ACCOUNT_ID}.dkr.ecr.${REGION}.amazonaws.com/${ECR_IMAGE}"

echo "Image pushed successfully."
