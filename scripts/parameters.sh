#!/bin/bash

#this file holds parameters for deployment scripts.  Modify as needed per environment.  
#It simulates GitHub Actions environment variables. In GitHub Actions, these can be sourced from environment-specific variables.
export ACCOUNT_ID="442042535335"
export REGION="ca-central-1"
export DEPARTMENT_NAME="reg"
export ENV_NAME="dv"
export STAGE_NAME="dv1"
export PROJECT_NAME="cg"

# FOR DOCKER
export COMPONENT_NAME="pdf2data"
export COMPONENT_VERSION="v7.9"
