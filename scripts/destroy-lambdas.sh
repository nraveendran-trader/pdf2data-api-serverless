#!/bin/bash

#this script deploys the VPC stack using AWS CDK.  Run it from the 'scripts' directory.

set -euo pipefail # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting to destroy Lambda deployment..."    
cd ../deploy
npm run build

LAMBDA_STACK_NAME="LambdaStack-${DEPARTMENT_NAME}-${ENV_NAME}-${STAGE_NAME}-${PROJECT_NAME}"

cdk destroy $LAMBDA_STACK_NAME \
    --app "npx ts-node --prefer-ts-exts bin/deploy.ts" \
    --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME} \
    --context componentName=${COMPONENT_NAME} \
    --context componentVersion=${COMPONENT_VERSION} \
    --force

echo "⏳ Waiting for stack deletion to complete..."
aws cloudformation wait stack-delete-complete \
    --stack-name $LAMBDA_STACK_NAME \
    --region ${REGION}


echo "Lambda deployment process completed."