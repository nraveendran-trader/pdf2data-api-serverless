import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { Construct } from 'constructs';
import { DynamoDbTableInfo, LambdaCreationInput, LambdaCreationOutput, LambdaCreationParams, PREFIXES, VpcCreationParams } from './models';
import * as iam from 'aws-cdk-lib/aws-iam';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as logs from 'aws-cdk-lib/aws-logs';
import * as cloudwatch from 'aws-cdk-lib/aws-cloudwatch';
import * as apigateway from 'aws-cdk-lib/aws-apigateway';
import { CfnACL } from 'aws-cdk-lib/aws-memorydb';
import * as ecr from 'aws-cdk-lib/aws-ecr';


export class LambdaStack extends cdk.Stack {

    private readonly _vpc: ec2.Vpc; // Store VPC reference
    private readonly _lambdas: LambdaCreationOutput[];

    constructor(scope: Construct, id: string, private props: cdk.StackProps & LambdaCreationParams ) {
        super(scope, id, props);

        this._lambdas = this.createAllLambdaFunctions();
        this.createLogGroupForLambdas(this._lambdas);
        this.createApiGatewayIntegrations(this._lambdas);
        this.createCloudWatchDashboardForLambdas(this._lambdas);
    }

    public get getLambdas(): LambdaCreationOutput[] {
        return this._lambdas;
    }

    public createApiGatewayIntegrations(lambdas : LambdaCreationOutput[] ): void {
        lambdas.forEach(lambda => {
            this.createApiGatewayIntegration(lambda);
        });
    }


    private createAllLambdaFunctions() : LambdaCreationOutput[] {
        const lambdas: LambdaCreationOutput[] = [];

        this.props.lambdas.forEach(lambdaInput => {
            const iamRole = this.createLambdaExecutionRole(lambdaInput, lambdaInput.dynamoDbTables || []);
            const sg = this.createSecurityGroupForLambda(lambdaInput);
            const lambdaFunction = this.createLambdaFunction(lambdaInput, iamRole, sg);
            
            let lambdaOutput: LambdaCreationOutput = {
                departmentName: this.props.departmentName,
                envName: this.props.envName,
                projectName: this.props.projectName,
                stageName: this.props.stageName,
                componentName: lambdaInput.componentName,
                function: lambdaFunction,
                role: iamRole,
                securityGroup: sg
            }

            lambdas.push(lambdaOutput);
        });

        return lambdas;
    }


    private createLambdaExecutionRole(lamndaInput: LambdaCreationInput, dynamodbTables?: DynamoDbTableInfo[]): iam.Role {
        const roleName = `${PREFIXES.ROLE}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}-${lamndaInput.componentName}`;

        const role = new iam.Role(this, roleName, {
            assumedBy: new iam.ServicePrincipal('lambda.amazonaws.com'),
            managedPolicies: [
                iam.ManagedPolicy.fromAwsManagedPolicyName('service-role/AWSLambdaBasicExecutionRole'),
                iam.ManagedPolicy.fromAwsManagedPolicyName('service-role/AWSLambdaVPCAccessExecutionRole'),
                iam.ManagedPolicy.fromAwsManagedPolicyName('AWSXRayDaemonWriteAccess'),
                iam.ManagedPolicy.fromAwsManagedPolicyName('AmazonSSMReadOnlyAccess'),
                iam.ManagedPolicy.fromAwsManagedPolicyName('AmazonBedrockFullAccess')
                // iam.ManagedPolicy.fromAwsManagedPolicyName('AmazonDynamoDBFullAccess') // Example managed policy
            ],
            inlinePolicies: {
                DynamoDbAccess: new iam.PolicyDocument({
                    statements: [
                        new iam.PolicyStatement({
                            actions: [
                                'dynamodb:GetItem',
                                'dynamodb:PutItem',
                                'dynamodb:UpdateItem',
                                'dynamodb:DeleteItem',
                                'dynamodb:Query',
                                'dynamodb:Scan'
                            ],
                            resources: dynamodbTables ? dynamodbTables.map(table => table.table.tableArn) : [],
                        }),
                    ],
                }),
                CloudWatchLogGroups: new iam.PolicyDocument({
                    statements: [
                        new iam.PolicyStatement({
                            actions: [
                                'logs:CreateLogGroup',
                                'logs:CreateLogStream',
                                'logs:PutLogEvents',
                                'logs:DescribeLogGroups',
                                'logs:DescribeLogStreams'
                            ],
                            resources: [`arn:aws:logs:${this.region}:${this.account}:log-group:/aws/lambda/*`],
                        }),
                    ],
                }),
            },
        });

        // Output Role Name
        new cdk.CfnOutput(this, `${roleName}-Name`, {
            value: role.roleName,
            description: `IAM Role Name for ${lamndaInput.componentName} Lambda function`,
            exportName: `${roleName}-Name`
        });

        // Output Role ARN
        new cdk.CfnOutput(this, `${roleName}-ARN`, {
            value: role.roleArn,
            description: `IAM Role ARN for ${lamndaInput.componentName} Lambda function`,
            exportName: `${roleName}-ARN`
        });
        
    
        return role;
    }

    private createSecurityGroupForLambda(lambdaInput: LambdaCreationInput): ec2.SecurityGroup {
        const securityGroupName = `${PREFIXES.SECURITYGROUP}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}-${lambdaInput.componentName}`;

        const sg = new ec2.SecurityGroup(this, securityGroupName, {
            vpc: this.props.vpc,
            securityGroupName: securityGroupName,
            description: `Security group for Lambda Department: ${this.props.departmentName}, Environment: ${this.props.envName}, Project: ${this.props.projectName}, Stage: ${this.props.stageName}, Component: ${lambdaInput.componentName}`,
            allowAllOutbound: false   // Deny outbound traffic by default
        }); 

        // sg.addIngressRule(ec2.Peer.ipv4(this.vpc.vpcCidrBlock), ec2.Port.tcp(443), 'Allow HTTPS traffic from this VPC only'); // Example rule
        sg.addEgressRule(ec2.Peer.anyIpv4(), ec2.Port.tcp(443), 'Allow outbound HTTPS traffic to anywhere'); // Example rule

        // Add tags to the security group
        cdk.Tags.of(sg).add('project', this.props.projectName);
        cdk.Tags.of(sg).add('environment', this.props.envName);
        cdk.Tags.of(sg).add('stage', this.props.stageName);
        cdk.Tags.of(sg).add('vpc', this.props.vpc.vpcId);
        cdk.Tags.of(sg).add('Name', securityGroupName);
        cdk.Tags.of(sg).add('resource-type', 'security-group');

        //output secrity group id
        new cdk.CfnOutput(this, `${securityGroupName}-ID`, {
            value: sg.securityGroupId,
            description: `Security Group ID for ${lambdaInput.componentName} Lambda function`,
            exportName: `${securityGroupName}-ID`
        });


        return sg;
    }

    private createLambdaFunction(lambdaInput: LambdaCreationInput, iamRole: iam.Role, securityGroup: ec2.SecurityGroup): lambda.Function {
        //Add individual environment variable for each DynamoDB table
        // if(lambdaInput.dynamoDbTables && lambdaInput.dynamoDbTables.length > 0) {
        //     lambdaInput.dynamoDbTables.forEach((tableInfo, index) => {
        //         environmentVars[`DYNAMODB_TABLE_ARN_${tableInfo.tableName}`] = tableInfo.table.tableArn;
        //     });
        // }

        const functionName = `${PREFIXES.LAMBDA}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}-${lambdaInput.componentName}`;

        let lambdaFunction: lambda.Function;
        if(lambdaInput.isContainerized) {
            // const repositoryName = `${this.props.departmentName}-${this.props.projectName}`;
            // const repository = ecr.Repository.fromRepositoryName(this, repositoryName, repositoryName);
            const repository = ecr.Repository.fromRepositoryName(this, lambdaInput.repositoryName, lambdaInput.repositoryName);
        
            lambdaFunction = new lambda.Function(this, functionName, {
                functionName: functionName,
                memorySize: lambdaInput.memorySize,
                timeout: cdk.Duration.seconds(30),
                code: lambda.Code.fromEcrImage(repository, {
                    cmd: lambdaInput.containerCmd,
                    tagOrDigest: lambdaInput.imageTag,
                    entrypoint: lambdaInput.containerEntryPoint,
                    workingDirectory: lambdaInput.workingDirectory
                }),
                handler: lambda.Handler.FROM_IMAGE, // Required for container images
                runtime: lambda.Runtime.FROM_IMAGE,  // Required for container images
                architecture: lambdaInput.architecture || lambda.Architecture.X86_64,
                vpc: this.props.vpc,
                vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
                securityGroups: [securityGroup],
                role: iamRole,
                tracing: lambda.Tracing.ACTIVE,
                logRetention: this.props.envName === 'prod' ? logs.RetentionDays.ONE_MONTH : logs.RetentionDays.ONE_WEEK,
                logFormat: lambda.LogFormat.JSON,
                systemLogLevel: lambda.SystemLogLevel.INFO,
                applicationLogLevel: this.props.envName === 'prod' ? lambda.ApplicationLogLevel.INFO : lambda.ApplicationLogLevel.DEBUG,
                environment: lambdaInput.environmentVariables ?? {}
            });
        } else {
            lambdaFunction = new lambda.Function(this, functionName, {
                functionName: functionName,
                runtime: lambdaInput.runtime,
                memorySize: lambdaInput.memorySize,
                handler: lambdaInput.handlerName,
                timeout: cdk.Duration.seconds(30),
                code: lambda.Code.fromAsset(lambdaInput.codePath!), // Path to your Lambda function code
                vpc: this.props.vpc,
                vpcSubnets: { subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS },
                securityGroups: [securityGroup],
                role: iamRole, // create IAM role
                tracing: lambda.Tracing.ACTIVE,
                logRetention: this.props.envName === 'prod' ? logs.RetentionDays.ONE_MONTH : logs.RetentionDays.ONE_WEEK, 
                logFormat: lambda.LogFormat.JSON, // Structured logging
                systemLogLevel: lambda.SystemLogLevel.INFO,
                applicationLogLevel: this.props.envName === 'prod' ? lambda.ApplicationLogLevel.INFO : lambda.ApplicationLogLevel.DEBUG,
                environment: lambdaInput.environmentVariables ?? {}
            });
        }

        // Add comprehensive tags to the Lambda function for easy identification
        cdk.Tags.of(lambdaFunction).add('Department', this.props.departmentName);
        cdk.Tags.of(lambdaFunction).add('Environment', this.props.envName);
        cdk.Tags.of(lambdaFunction).add('Project', this.props.projectName);
        cdk.Tags.of(lambdaFunction).add('Stage', this.props.stageName);
        cdk.Tags.of(lambdaFunction).add('Component', lambdaInput.componentName);
        cdk.Tags.of(lambdaFunction).add('ResourceType', 'lambda-function');
        cdk.Tags.of(lambdaFunction).add('Name', `${this.props.departmentName}-${this.props.envName}-${lambdaInput.componentName}`);

        // // Create custom log group with specific naming and tags for easy identification
        // const logGroupName = `/aws/lambda/${functionName}`;
        // const logGroup = new logs.LogGroup(this, `log-group-${functionName}`, {
        //     logGroupName: logGroupName,
        //     retention: this.props.envName === 'prod' ? logs.RetentionDays.ONE_MONTH : logs.RetentionDays.ONE_WEEK,
        //     removalPolicy: this.props.envName === 'prod' ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
        // });

        // // Add comprehensive tags to the log group for easy identification
        // cdk.Tags.of(logGroup).add('Department', this.props.departmentName);
        // cdk.Tags.of(logGroup).add('Environment', this.props.envName);
        // cdk.Tags.of(logGroup).add('Project', this.props.projectName);
        // cdk.Tags.of(logGroup).add('Stage', this.props.stageName);
        // cdk.Tags.of(logGroup).add('Component', lambdaInput.componentName);
        // cdk.Tags.of(logGroup).add('ResourceType', 'lambda-log-group');
        // cdk.Tags.of(logGroup).add('Name', `${this.props.departmentName}-${this.props.envName}-${lambdaInput.componentName}-logs`);
        
        // // Output the log group name for easy reference
        // new cdk.CfnOutput(this, `LogGroup-${functionName}`, {
        //     value: logGroupName,
        //     description: `CloudWatch Log Group for ${lambdaInput.componentName} Lambda function`,
        //     exportName: `${this.stackName}-${lambdaInput.componentName}-LogGroup`
        // });

        //output function name, arn
        new cdk.CfnOutput(this, `${functionName}-Name`, {
            value: lambdaFunction.functionName,
            description: `Lambda Function Name for ${lambdaInput.componentName}`,
            exportName: `${functionName}-Name`
        });

        new cdk.CfnOutput(this, `${functionName}-ARN`, {
            value: lambdaFunction.functionArn,
            description: `Lambda Function ARN for ${lambdaInput.componentName}`,
            exportName: `${functionName}-ARN`
        });

        return lambdaFunction;
    }


    private createLogGroupForLambdas(lambdas: LambdaCreationOutput[]): void {
        lambdas.forEach(lambda => {
            // Create custom log group with specific naming and tags for easy identification
            const logGroupName = `/aws/lambda/${lambda.function.functionName}`;
            const logGroupConstructId = `${PREFIXES.LOGGROUP}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}-${lambda.componentName}`;

            const logGroup = new logs.LogGroup(this, logGroupConstructId, {
                logGroupName: logGroupName,
                retention: this.props.envName === 'prod' ? logs.RetentionDays.ONE_MONTH : logs.RetentionDays.ONE_WEEK,
                removalPolicy: this.props.envName === 'prod' ? cdk.RemovalPolicy.RETAIN : cdk.RemovalPolicy.DESTROY,
            });

            // Add comprehensive tags to the log group for easy identification
            cdk.Tags.of(logGroup).add('Department', this.props.departmentName);
            cdk.Tags.of(logGroup).add('Environment', this.props.envName);
            cdk.Tags.of(logGroup).add('Project', this.props.projectName);
            cdk.Tags.of(logGroup).add('Stage', this.props.stageName);
            cdk.Tags.of(logGroup).add('Component', lambda.componentName);
            cdk.Tags.of(logGroup).add('ResourceType', 'lambda-log-group');
            cdk.Tags.of(logGroup).add('Name', `${this.props.departmentName}-${this.props.envName}-${lambda.componentName}-logs`);
            
            // Output the log group name for easy reference
            new cdk.CfnOutput(this, `LogGroup-${logGroupConstructId}`, {
                value: logGroupName,
                description: `CloudWatch Log Group for ${lambda.componentName} Lambda function`,
                exportName: `${this.stackName}-${lambda.componentName}-LogGroup`
            });
           

        });
    }



    private createApiGatewayIntegration(lambda: LambdaCreationOutput): apigateway.RestApi {
        const apigwName = `${PREFIXES.APIGATEWAY}-${lambda.departmentName}-${lambda.envName}`;

        const apigw = new apigateway.RestApi(this, apigwName, {
            restApiName: `${PREFIXES.APIGATEWAY_RESTAPI}-${lambda.departmentName}-${lambda.envName}`, // ✅ Same API name for all stages
            description: `API Gateway for ${lambda.departmentName}-${lambda.envName}`,
            deploy: true,
            deployOptions: {
                stageName: `${lambda.stageName}`, // Default stage name
                throttlingBurstLimit: 100,
                throttlingRateLimit: 50,
            },
            binaryMediaTypes: [
                'application/json',
                'application/octet-stream',
                'application/pdf',
                'image/png',
                'image/jpeg',
                'multipart/form-data',
                '*/*'
            ],
            defaultCorsPreflightOptions: {
                allowOrigins: apigateway.Cors.ALL_ORIGINS,
                allowMethods: apigateway.Cors.ALL_METHODS,
                allowHeaders: [
                    'Content-Type', 
                    'X-Amz-Date', 
                    'Authorization', 
                    'X-Api-Key',
                    'Content-Length',           // ✅ Required for file uploads
                    'Content-Disposition',      // ✅ Required for multipart/form-data
                    'Accept',
                    'Accept-Encoding'
                ],
            }
        });

        const lambdaIntegration = new apigateway.LambdaIntegration(lambda.function, {
            requestTemplates: { 
                'application/json': '{ "statusCode": "200" }',
                'multipart/form-data': '$input.body',
                'application/x-www-form-urlencoded': '$input.body'
            }
        });

        apigw.root.addProxy({
            defaultIntegration: lambdaIntegration,
        });

        //output api gateway url, id
        new cdk.CfnOutput(this, `${apigwName}-Url`, {
            value: apigw.url,
            description: `API Gateway URL for ${lambda.departmentName}-${lambda.envName}`,
            exportName: `${apigwName}-Url`
        });

        new cdk.CfnOutput(this, `${apigwName}-InvokeUrl`, {
            value: `${apigw.url}${lambda.stageName}`,
            description: `API Gateway Invoke URL for ${lambda.departmentName}-${lambda.envName} Stage: ${lambda.stageName}`,
            exportName: `${apigwName}-InvokeUrl`
        });

        new cdk.CfnOutput(this, `${apigwName}-RestApiId`, {
            value: apigw.restApiId,
            description: `API Gateway Rest API ID for ${lambda.departmentName}-${lambda.envName}`,
            exportName: `${apigwName}-RestApiId`
        });
        
        return apigw;
    }


    private createCloudWatchDashboardForLambdas(lambdas: LambdaCreationOutput[]): void {
        const dashboardName = `${PREFIXES.CLOUDWATCH_DASHBOARD}-${this.props.departmentName}-${this.props.envName}-${this.props.stageName}-${this.props.projectName}`;
        
        const dashboard = new cloudwatch.Dashboard(this, dashboardName, {
            dashboardName: dashboardName,
        });

        // Create widgets for each Lambda function
        lambdas.forEach((lambdaOutput, index) => {
            const functionName = lambdaOutput.function.functionName;
            
            // Lambda metrics widget
            const lambdaMetrics = new cloudwatch.GraphWidget({
                title: `${lambdaOutput.componentName} Lambda Metrics`,
                left: [
                    lambdaOutput.function.metricInvocations({ label: 'Invocations' }),
                    lambdaOutput.function.metricErrors({ label: 'Errors' }),
                    lambdaOutput.function.metricDuration({ label: 'Duration' }),
                ],
                width: 12,
                height: 6,
            });

            // Log insights widget
            const logInsights = new cloudwatch.LogQueryWidget({
                title: `${lambdaOutput.componentName} Recent Logs`,
                logGroupNames: [`/aws/lambda/${functionName}`],
                view: cloudwatch.LogQueryVisualizationType.TABLE,
                queryString: `fields @timestamp, @message | sort @timestamp desc | limit 20`,
                width: 12,
                height: 6,
            });

            dashboard.addWidgets(lambdaMetrics, logInsights);
        });

        // Output the dashboard URL
        new cdk.CfnOutput(this, `${dashboardName}-URL`, {
            value: `https://${this.region}.console.aws.amazon.com/cloudwatch/home?region=${this.region}#dashboards:name=${dashboard.dashboardName}`,
            description: `CloudWatch Dashboard URL for ${this.props.departmentName}-${this.props.envName}-${this.props.projectName}`,
            exportName: `${dashboardName}-URL`
        });
     
    }

}