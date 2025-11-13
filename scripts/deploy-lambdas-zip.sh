#!/bin/bash

#this script deploys the VPC stack using AWS CDK.  Run it from the 'scripts' directory.

set -euo pipefail # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting Lambda deployment process..."
echo "Cleaning previous build..."

if test -d "../pdf2data/bin/Release/net8.0/publish"; then
    echo "Removing existing publish directory..."
    rm -rf ../pdf2data/bin/Release/net8.0/publish
fi

echo "Publishing .NET project..."
dotnet publish ../pdf2data/pdf2data.csproj \
        --configuration Release \
        --framework net8.0 \
        --runtime linux-x64 \
        --output ../pdf2data/bin/Release/net8.0/publish
    
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