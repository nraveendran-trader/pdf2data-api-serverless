#!/bin/bash

set -e # Exit immediately if a command exits with a non-zero status
source ./parameters.sh

docker run -p 8080:8080 \
  -e REGION=${REGION} \
  -e DEPARTMENT_NAME=${DEPARTMENT_NAME} \
  -e ENV_NAME=loc \
  -e STAGE_NAME=loc \
  -e PROJECT_NAME=${PROJECT_NAME} \
  -e COMPONENT_NAME=${COMPONENT_NAME} \
  -e EXPOSE_API_EXPLORER=true \
  -e PDF_FOCUS_KEY=your-local-license-key \
  -e LOCAL_DYNAMODB_ENDPOINT=http://localhost:8000 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  pdf2data:${COMPONENT_VERSION}