# AWS SSM Parameter Store Commands

## Current Parameter Path Structure
Based on your `ConfigProvider.cs`, your SSM parameters follow this pattern:
```
/{DEPARTMENT_NAME}/{ENV_NAME}/{STAGE_NAME}/{PROJECT_NAME}/{COMPONENT_NAME}/{parameter_name}
```

## Example Commands to Put Parameters

### 1. Put PDF Focus Key Parameter
```bash
# For development environment
aws ssm put-parameter \
    --name "/reg/loc/loc/cg/pdf2data/pdf_focus_key" \
    --value "your-pdf-focus-license-key-here" \
    --type "SecureString" \
    --description "PDF Focus license key for SautinSoft library"

# For production environment  
aws ssm put-parameter \
    --name "/reg/prod/prod/cg/pdf2data/pdf_focus_key" \
    --value "your-pdf-focus-license-key-here" \
    --type "SecureString" \
    --description "PDF Focus license key for SautinSoft library"
```

### 2. Put Other Configuration Parameters
```bash
# Database connection string
aws ssm put-parameter \
    --name "/reg/loc/loc/cg/pdf2data/db_connection_string" \
    --value "your-database-connection-string" \
    --type "SecureString" \
    --description "Database connection string"

# API rate limit
aws ssm put-parameter \
    --name "/reg/loc/loc/cg/pdf2data/api_rate_limit" \
    --value "100" \
    --type "String" \
    --description "API rate limit per minute"

# External service endpoint
aws ssm put-parameter \
    --name "/reg/loc/loc/cg/pdf2data/external_service_url" \
    --value "https://api.external-service.com" \
    --type "String" \
    --description "External service API endpoint"
```

### 3. Generic Put Parameter Template
```bash
aws ssm put-parameter \
    --name "/reg/{env_name}/{stage_name}/cg/pdf2data/{parameter_name}" \
    --value "{parameter_value}" \
    --type "{String|SecureString|StringList}" \
    --description "{parameter_description}" \
    --overwrite  # Optional: to update existing parameter
```

## Parameter Types

- **String**: Plain text values
- **SecureString**: Encrypted values (recommended for secrets)
- **StringList**: Comma-separated list of values

## Useful SSM Commands

### List all parameters in your namespace
```bash
aws ssm get-parameters-by-path \
    --path "/reg/loc/loc/cg/pdf2data/" \
    --recursive
```

### Get a specific parameter
```bash
aws ssm get-parameter \
    --name "/reg/loc/loc/cg/pdf2data/pdf_focus_key" \
    --with-decryption
```

### Update an existing parameter
```bash
aws ssm put-parameter \
    --name "/reg/loc/loc/cg/pdf2data/pdf_focus_key" \
    --value "new-license-key" \
    --type "SecureString" \
    --overwrite
```

### Delete a parameter
```bash
aws ssm delete-parameter \
    --name "/reg/loc/loc/cg/pdf2data/pdf_focus_key"
```

### Get parameter history
```bash
aws ssm get-parameter-history \
    --name "/reg/loc/loc/cg/pdf2data/pdf_focus_key"
```

## Environment Variables Required

Based on your `ConfigProvider.cs`, ensure these environment variables are set:

```bash
export DEPARTMENT_NAME="reg"
export ENV_NAME="loc"           # or "prod" for production
export STAGE_NAME="loc"         # or "prod" for production  
export PROJECT_NAME="cg"
export COMPONENT_NAME="pdf2data"
export AWS_REGION="us-east-1"   # or your preferred region
```

## Quick Setup Script

Create a script to put all your parameters at once:

```bash
#!/bin/bash

# Set your environment
DEPT="reg"
ENV="loc"
STAGE="loc"
PROJECT="cg"
COMPONENT="pdf2data"
BASE_PATH="/$DEPT/$ENV/$STAGE/$PROJECT/$COMPONENT"

# Put parameters
aws ssm put-parameter \
    --name "$BASE_PATH/pdf_focus_key" \
    --value "YOUR_LICENSE_KEY_HERE" \
    --type "SecureString" \
    --description "PDF Focus license key"

aws ssm put-parameter \
    --name "$BASE_PATH/max_file_size_mb" \
    --value "50" \
    --type "String" \
    --description "Maximum PDF file size in MB"

aws ssm put-parameter \
    --name "$BASE_PATH/processing_timeout_seconds" \
    --value "300" \
    --type "String" \
    --description "PDF processing timeout in seconds"

echo "Parameters created successfully!"
```

## Local Development

For local development, you can either:

1. **Use environment variables** (as shown in your `launchSettings.json`)
2. **Use local SSM parameters** with LocalStack
3. **Use a `.env` file** (your project already supports this)

Example `.env` file:
```
DEPARTMENT_NAME=reg
ENV_NAME=loc
STAGE_NAME=loc
PROJECT_NAME=cg
COMPONENT_NAME=pdf2data
AWS_REGION=us-east-1
PDF_FOCUS_KEY=your-local-license-key
```