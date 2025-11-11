#!/bin/bash

#this script deploys the VPC stack using AWS CDK.  Run it from the 'scripts' directory.

set -e # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting Data deployment process..."
cd ../deploy

DATA_STACK_NAME="DataStack-${DEPARTMENT_NAME}-${ENV_NAME}-${STAGE_NAME}-${PROJECT_NAME}"

echo "Synthesizing..."
cdk synth $DATA_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy-data.ts" \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME}

echo "Deploying..."
cdk deploy $DATA_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy-data.ts" \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME}

echo "Data deployment process completed."