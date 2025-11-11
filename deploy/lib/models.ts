import * as dynamodb from "aws-cdk-lib/aws-dynamodb";
import * as ec2 from 'aws-cdk-lib/aws-ec2';

export const PREFIXES = {
  VPC: 'vpc',
  SUBNET: 'sub',
  LAMBDA: 'lmd',
  ROLE: 'rol',
  DYNAMODB: 'tbl',
  APIGATEWAY: 'agw',
  RDS: 'rds',
  VPCENDPOINT: 'vep'
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
