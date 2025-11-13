import * as dynamodb from "aws-cdk-lib/aws-dynamodb";
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as iam from 'aws-cdk-lib/aws-iam';

export const PREFIXES = {
  VPC: 'vpc',
  SUBNET: 'sub',
  LAMBDA: 'lmd',
  ROLE: 'rol',
  DYNAMODB: 'tbl',
  APIGATEWAY: 'agw',
  APIGATEWAY_RESTAPI: 'api',
  RDS: 'rds',
  VPCENDPOINT: 'vep',
  SECURITYGROUP : 'sgr',
  LOGGROUP: 'lgp',
  CLOUDWATCH_DASHBOARD: 'cdd',
  ECR : 'ecr'
}

export interface ProjectParameters {
  departmentName: string,
  projectName: string;
  envName: string;
  stageName: string;
}

export interface VpcCreationParams{
  departmentName: string,
  envName: string;  
} 

export interface DynamoDbCreationParams extends ProjectParameters {
  tables: string[];
  vpc: ec2.IVpc;
  // vpcEndpoint: ec2.GatewayVpcEndpoint;
}

export interface DynamoDbTableInfo {
    tableName: string;
    table: dynamodb.Table;
}

export interface LambdaCreationParams extends ProjectParameters {
  vpc: ec2.IVpc;
  lambdas: LambdaCreationInput[];
}

export interface LambdaCreationInput {
  componentName: string;
  dynamoDbTables?: DynamoDbTableInfo[]; //optional
  memorySize: number;
  environmentVariables: { [key: string]: string }; 
  runtime: lambda.Runtime; //eg. lambda.Runtime.DOTNET_8, not required for containers
  handlerName: string; // not required for containers
  codePath?: string; // Path to Lambda function code, not required for containers
  
  // Container-specific properties
  isContainerized?: boolean;
  repositoryName: string; // ECR repository URI
  imageTag: string; // ECR image tag
  containerCmd?: string[]; // CMD instruction
  containerEntryPoint?: string[]; // ENTRYPOINT instruction
  workingDirectory?: string;
  architecture?: lambda.Architecture; // X86_64 or ARM64
}

export interface LambdaCreationOutput extends ProjectParameters {
  componentName: string;
  function: lambda.Function;
  role: iam.Role;
  securityGroup: ec2.SecurityGroup;
}

export interface EcrCreationParams {
  // Additional ECR-specific parameters can be added here if needed
  // componentName: string;
  departmentName: string,
  projectName: string;
}
