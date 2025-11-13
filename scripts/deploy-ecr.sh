#!/usr/bin/env bash

set -euo pipefail
source "../scripts/parameters.sh"

echo "Starting ECR deployment process..."
cd ../deploy

ECR_STACK_NAME="EcrStack-${DEPARTMENT_NAME}-${PROJECT_NAME}"

echo "Synthesizing..."
cdk synth $ECR_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy.ts" \
    --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME}

echo "Deploying..."
cdk deploy $ECR_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy.ts" \
    --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME}
