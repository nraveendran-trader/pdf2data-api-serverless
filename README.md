# pdf2data api serverless demo app

### Configuration management
All configuration must be either environment variable based or from SSM Parameter.  For local development, the configuration values are stored and read from .env file at the root of the repo folder structure. Make sure that this .env file is added to .gitignore file so that it does not get checked in to source code repo. 

In each .NET project, include the following nuget package, which reads the environment values from the .env file:

```xml
<PackageReference Include="DotNetEnv" Version="3.1.1" />
```

Then, in the code, you can load the environment variables like the following:

```csharp
if(builder.Environment.IsDevelopment())
{
    DotNetEnv.Env.TraversePath().Load(); // Load environment variables from .env file in development
}
```

Any subsequent calls to ``` Environment.GetEnvironmentVariable() ```, will automatically read the environment variables loaded by DotEnv.  When the application runs, any calls to ``` Environment.GetEnvironmentVariable() ``` will read from the actual environment on the compute box it runs on (eg. Lambda, Kubernetes, etc).


### Deployment
Deployment is done using AWS CDK (in deploy folder).  The CDK project was created using the following command (issued from within deploy folder):

```bash
cdk init --language=typescript

```


### Resource Naming
Resource naming rules to follow:
- Each resource name should be globally unique within an AWS account and region
- Avoid using sensitive information in resource names
- Use lowercase letters, numbers, and hyphens (-) only
- Avoid using underscores (_), periods (.), or other special characters
- Keep names concise but descriptive
- Avoid using AWS reserved words
- length of each segment should be within limits (usually 2-3 characters per segment) except for the component name.
- Total must not exceed 32 characters
- Stage is optional - can be omitted if resource is used by all stages in an environment

**Resource naming syntax:** 

{type}-reg-{env}-[{stage}]-[{project}]-[{component}]

Example (ALB resource): alb-reg-dv

Example (Infinity): inf-reg-dv-dv1-cg-publicapi 

Example (Lambda): lmd-reg-qa-ut1-mw-autoclient


**Recommended prefixes for resources:**
- alb: app load balancer
- inf: infinity container
- lmd: lambdas
- nlb: network load balancer
- asg: auto scaling group 
- rol: iam role
- pol: policy
- s3b: s3 bucket
- tbl: dynamodb table
- rds: rds db
- sgr: security group
- vpc: vpc
- sub: subnet
- rut: route table
- igw: internet gateway
- ngw: nat gateway
- ec2: ec2 instance
- agw: api gateway
- cdw: cloudwatch
- sns: sns topic
- sqs: sqs queue,
- clf: cloudfront

**Recommended prefixes for environments:**
- dv: development
- qa: QA
- ut: UAT
- pt: PAT
- pr: Production

**Recommended prefixes for environments:**
- dv1, dv2, etc
- qa1, qa2, etc
- ut1, ut2, etc
- pt1, pt2, etc
- pr1, pr2 (for blue-green deployments)

**Project name samples:**
- cg: collateral guard
- ams = asset monitoring solutions
- rc = registry connect

**Component name samples:**
- uiapi, publicapi, restapi, autoclient, pdf2data, emailapi, authsvr, etc.





