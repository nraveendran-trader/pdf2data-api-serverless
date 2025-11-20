#!/bin/bash

#this script destroys all deployed stacks using AWS CDK.  Run it from the 'scripts' directory.

set -euo pipefail # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

echo "Starting to destroy all..."
cd ../deploy


echo "Destroying..."
cdk destroy --context region=${REGION} \
    --context department=${DEPARTMENT_NAME} \
    --context env=${ENV_NAME} \
    --context stage=${STAGE_NAME} \
    --context project=${PROJECT_NAME} \
    --context componentName=${COMPONENT_NAME} \
    --context componentVersion=${COMPONENT_VERSION} \
    --all 

echo "Destroy process completed."