#!/bin/bash

#this script deploys the VPC stack using AWS CDK.  Run it from the 'scripts' directory.

set -euo pipefail # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

./docker-build.sh
./docker-push.sh
    
cd ../deploy
echo "Building CDK application..."
npm run build

LAMBDA_STACK_NAME="LambdaStack-${DEPARTMENT_NAME}-${ENV_NAME}-${STAGE_NAME}-${PROJECT_NAME}"

echo "Synthesizing..."
cdk synth $LAMBDA_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy.ts" \
    --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME} \
    --context componentName=${COMPONENT_NAME} \
    --context componentVersion=${COMPONENT_VERSION}

# echo "Deploying..."
cdk deploy $LAMBDA_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy.ts" \
    --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME} \
    --context componentName=${COMPONENT_NAME} \
    --context componentVersion=${COMPONENT_VERSION}

echo "Lambda deployment process completed."