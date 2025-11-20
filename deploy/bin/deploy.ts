#!/usr/bin/env node
import * as cdk from 'aws-cdk-lib';
import { VpcStack } from '../lib/vpc-stack';
import { PREFIXES, VpcCreationParams } from '../lib/models';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { DataStack } from '../lib/data-stack';
import { LambdaStack } from '../lib/lambda-stack';
import { EcrStack } from '../lib/ecr-stack';

const app = new cdk.App();

//get values from command line:
const region = app.node.tryGetContext('region');
const departmentName: string = app.node.tryGetContext('department');
const envName: string = app.node.tryGetContext('env');
const projectName: string = app.node.tryGetContext('project');
const stageName: string = app.node.tryGetContext('stage');
const componentName: string = app.node.tryGetContext('componentName');
const componentVersion: string = app.node.tryGetContext('componentVersion');

if(!departmentName || !envName || !projectName || !stageName || !region || !componentVersion || !componentName){
  console.error("Error: Missing required context parameters.");
  throw new Error("Missing required context parameters.");
}

const ecrStackName = `EcrStack-${departmentName}-${projectName}`;
console.info(`Deploying ECR Stack: ${ecrStackName} for Department: ${departmentName}, Project: ${projectName}`);
// console.info(`Deploying ECR Stack: ${ecrStackName} for Department: ${departmentName}, Environment: ${envName}, Project: ${projectName}, Stage: ${stageName}`);
const ecrStack = new EcrStack(app, ecrStackName, {
  stackName: ecrStackName,
  env: { account: process.env.CDK_DEFAULT_ACCOUNT, region: process.env.CDK_DEFAULT_REGION },
  description: `ECR Stack for Department: ${departmentName}, Project: ${projectName}`,
  terminationProtection: false, // Set to true to prevent stack deletion in critical environments
  departmentName: departmentName,
  projectName: projectName,
});


const vpcStackName = `VpcStack-${departmentName}-${envName}`;
console.info(`Deploying VPC Stack: ${vpcStackName} for Department: ${departmentName}, Environment: ${envName}`);
const vpcStack = new VpcStack(app, vpcStackName, {
  stackName: vpcStackName,
  env: { account: process.env.CDK_DEFAULT_ACCOUNT, region: process.env.CDK_DEFAULT_REGION },
  description: `VPC Stack for Department: ${departmentName}, Environment: ${envName}`,
  terminationProtection: false, // Set to true to prevent stack deletion in critical environments
  departmentName: departmentName, 
  envName: envName 
});


//use the following code if you want to find vpc by lookup instead of creating new
// const vpc = vpcStack.getVpcByLookup(`${'vpc'}-${departmentName}-${envName}`);
//if you are using existing VPC, uncomment the above line and comment out the VpcStack creation
const vpc: ec2.IVpc = vpcStack.getVpc();
const dataStackName = `DataStack-${departmentName}-${envName}-${stageName}-${projectName}`;
console.info(`Deploying Data Stack: ${dataStackName} for Department: ${departmentName}, Environment: ${envName}, Project: ${projectName}, Stage: ${stageName}`);
var dataStack = new DataStack(app, dataStackName, {
  stackName: dataStackName,
  env: { account: process.env.CDK_DEFAULT_ACCOUNT, region: process.env.CDK_DEFAULT_REGION },
  description: `Data Stack for Department: ${departmentName}, Environment: ${envName} , Project: ${projectName}, Stage: ${stageName}`,
  terminationProtection: false, // Set to true to prevent stack deletion in critical environments
  departmentName: departmentName,
  projectName: projectName,
  envName: envName,
  stageName: stageName,
  vpc: vpc,
  tables: [
    'logs' 
  ]
});

const lambdaStackName = `LambdaStack-${departmentName}-${envName}-${stageName}-${projectName}`;
const lambdaStack = new LambdaStack(app, lambdaStackName, {
  stackName: lambdaStackName,
  env: { account: process.env.CDK_DEFAULT_ACCOUNT, region: process.env.CDK_DEFAULT_REGION },
  description: `Lambda Stack for ${departmentName} in ${envName} environment`,
  terminationProtection: false, // Set to true to prevent stack deletion in critical environments
  departmentName: departmentName,
  projectName: projectName,
  envName: envName,
  stageName: stageName,
  vpc: vpc,
  lambdas: [
    // {
    //   isContainerized: false,
    //   componentName: 'pdf2data',
    //   handlerName: 'pdf2data', //for .NET apps using top level statements project templates, this is the assembly name
    //   codePath: '../pdf2data/bin/Release/net8.0/publish',
    //   memorySize: 512,
    //   runtime: cdk.aws_lambda.Runtime.DOTNET_8,
    //   dynamoDbTables: dataStackName ? dataStack.getTables() : [],
    //   environmentVariables: {
    //     REGION: region,
    //     DEPARTMENT_NAME: departmentName,
    //     PROJECT_NAME: projectName,
    //     ENV_NAME: envName,
    //     STAGE_NAME: stageName,
    //     COMPONENT_NAME: 'pdf2data',
    //     EXPOSE_API_EXPLORER: 'true',
    //     ASPNETCORE_ENVIRONMENT: envName === 'prod' ? 'Production' : 'Development',
    //     LAMBDA_LOG_LEVEL: envName === 'prod' ? 'INFO' : 'DEBUG',
    //     // PDF_FOCUS_KEY: process.env.PDF_FOCUS_KEY || '70057226651'
    //   }      
    // }
    {
      isContainerized: true,
      architecture: cdk.aws_lambda.Architecture.X86_64,
      containerCmd: undefined, // Not needed in this case
      workingDirectory: undefined,
      repositoryName: ecrStack.getRepository().repositoryName,
      imageTag: `${componentName}-${componentVersion}`,
      containerEntryPoint: ["dotnet", "pdf2data.dll"],
      componentName: componentName,
      handlerName: '', //not required for containerized Lambdas
      // codePath: '../pdf2data/bin/Release/net8.0/publish', //not required for containerized Lambdas
      memorySize: 1024,
      runtime: cdk.aws_lambda.Runtime.DOTNET_8,
      dynamoDbTables: dataStackName ? dataStack.getTables() : [],
      environmentVariables: {
        REGION: region,
        DEPARTMENT_NAME: departmentName,
        PROJECT_NAME: projectName,
        ENV_NAME: envName,
        STAGE_NAME: stageName,
        COMPONENT_NAME: componentName,
        COMPONENT_VERSION: componentVersion,
        EXPOSE_API_EXPLORER: 'true',
        ASPNETCORE_ENVIRONMENT: envName === 'prod' ? 'Production' : 'Development',
        LAMBDA_LOG_LEVEL: envName === 'prod' ? 'INFO' : 'DEBUG',
        // PDF_FOCUS_KEY: process.env.PDF_FOCUS_KEY || '70057226651'
      }      
    }
  ],
  tags: {
    department: departmentName,
    environment: envName
  },
});





