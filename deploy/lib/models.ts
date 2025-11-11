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

export const PREFIXES = {
  VPC: 'vpc',
  SUBNET: 'sub',
  LAMBDA: 'lmd',
  ROLE: 'rol',
  DYNAMODB: 'ddb',
  APIGATEWAY: 'agw',
  RDS: 'rds'
}

