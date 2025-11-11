#!/bin/bash

#this script deploys the VPC stack using AWS CDK.  Run it from the 'scripts' directory.

set -e # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting VPC deployment process..."
cd ../deploy

VPC_STACK_NAME="VpcStack-${DEPARTMENT_NAME}-${ENV_NAME}"

echo "Synthesizing..."
cdk synth $VPC_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy-vpc.ts" \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME}

echo "Deploying..."
cdk deploy $VPC_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy-vpc.ts" \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME}

echo "VPC deployment process completed."