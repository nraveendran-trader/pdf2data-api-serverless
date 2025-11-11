import * as cdk from 'aws-cdk-lib';
import * as dynamodb from "aws-cdk-lib/aws-dynamodb";
import { Construct } from 'constructs';
import { DynamoDbCreationParams, DynamoDbTableInfo, PREFIXES } from './models';
import * as iam from "aws-cdk-lib/aws-iam";
import * as ec2 from "aws-cdk-lib/aws-ec2";

export class DataStack extends cdk.Stack {
    private _vpcEndpoint: ec2.GatewayVpcEndpoint;
    private _dynamoDbTables: DynamoDbTableInfo[] = [];

    constructor(scope: Construct, id: string, private props: cdk.StackProps & DynamoDbCreationParams ) {
        super(scope, id, props);

        this._vpcEndpoint = this.createDynamoDbVpcEndpoint();
        this._dynamoDbTables = this.createDynamoDbTables();
        this.applyVpcOnlyAccessPolicy();
    }

    public getTables(): DynamoDbTableInfo[] {
        return this._dynamoDbTables;
    }

    private createDynamoDbVpcEndpoint(): ec2.GatewayVpcEndpoint {
        const vpcEndpointName = `${PREFIXES.VPCENDPOINT}-${this.props.departmentName}-${this.props.envName}`;
        const vpcEndpoint = new ec2.GatewayVpcEndpoint(this, vpcEndpointName, {
            vpc: this.props.vpc,
            service: ec2.GatewayVpcEndpointAwsService.DYNAMODB,
            subnets: [
                { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
                { subnetType: ec2.SubnetType.PRIVATE_ISOLATED }
            ]
        });

        // Add tags
        cdk.Tags.of(vpcEndpoint).add('Name', vpcEndpointName);
        cdk.Tags.of(vpcEndpoint).add('resource-type', 'vpc-endpoint');

        return vpcEndpoint;
    }

    private createDynamoDbTables (): DynamoDbTableInfo[] {
        const tables: DynamoDbTableInfo[] = [];

        this.props.tables.forEach(tbl => {
            //table naming pattern: reg-dv-dv1-cg-logs
            const tableName = `${PREFIXES.DYNAMODB}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}-${tbl}`;
                        
            const table = new dynamodb.Table(this, tableName, {
                tableName: tableName,
                billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
                partitionKey: { name: "id", type: dynamodb.AttributeType.STRING },
                // encryption: this.props.envName === 'prod' ? dynamodb.TableEncryption.CUSTOMER_MANAGED : dynamodb.TableEncryption.AWS_MANAGED,
                encryption: dynamodb.TableEncryption.AWS_MANAGED,
                removalPolicy: this.props.envName === 'prod' ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
            });

            // Add tags to the table
            cdk.Tags.of(table).add('department', this.props.departmentName);
            cdk.Tags.of(table).add('project', this.props.projectName);
            cdk.Tags.of(table).add('environment', this.props.envName);
            cdk.Tags.of(table).add('stage', this.props.stageName);
            cdk.Tags.of(table).add('table-name', tbl);
            cdk.Tags.of(table).add('resource-type', 'dynamodb-table');

            tables.push({tableName: tbl, table: table});
        });

        return tables;
    }

    private applyVpcOnlyAccessPolicy(): void {
        this._dynamoDbTables.forEach(tableInfo => {
            // Create resource policy that only allows access from the VPC
            const vpcOnlyPolicy = new iam.PolicyStatement({
                sid: 'VpcOnlyAccess',
                effect: iam.Effect.DENY,
                principals: [new iam.AnyPrincipal()],
                actions: [
                    'dynamodb:GetItem',
                    'dynamodb:PutItem',
                    'dynamodb:Query',
                    'dynamodb:Scan',
                    'dynamodb:UpdateItem',
                    'dynamodb:DeleteItem',
                    'dynamodb:BatchGetItem',
                    'dynamodb:BatchWriteItem'
                ],
                resources: [tableInfo.table.tableArn],
                conditions: {
                    StringNotEquals: {
                        'aws:sourceVpce': this._vpcEndpoint.vpcEndpointId
                    }
                }
            });

            // Apply the policy to the table
            tableInfo.table.addToResourcePolicy(vpcOnlyPolicy);
        });
    }

}