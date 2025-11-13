import * as cdk from 'aws-cdk-lib';
import * as ecr from 'aws-cdk-lib/aws-ecr';
import * as iam from 'aws-cdk-lib/aws-iam';
import { Construct } from 'constructs';
import { EcrCreationParams, PREFIXES } from '../lib/models';

export class EcrStack extends cdk.Stack {
    public readonly repository: ecr.Repository;

    constructor(scope: Construct, id: string, private props: cdk.StackProps & EcrCreationParams) {
        super(scope, id, props);

        // Create ECR repository
        this.repository = this.createEcrRepository();
        
        // Add lifecycle policy to manage image retention
        this.addLifecyclePolicy();
        
        // Create IAM role for GitHub Actions (optional)
        // this.createGitHubActionsRole();
    }

    public getRepository(): ecr.Repository {
        return this.repository;
    }

    private createEcrRepository(): ecr.Repository {
        const repositoryName = `${this.props.departmentName}-${this.props.projectName}`;
        
        const repository = new ecr.Repository(this, repositoryName, {
            repositoryName: repositoryName,
            removalPolicy: cdk.RemovalPolicy.DESTROY,
            imageScanOnPush: true,
            imageTagMutability: ecr.TagMutability.MUTABLE,
            encryption: ecr.RepositoryEncryption.AES_256,
        });

        // Add comprehensive tags
        cdk.Tags.of(repository).add('Department', this.props.departmentName);
        cdk.Tags.of(repository).add('Project', this.props.projectName);
        cdk.Tags.of(repository).add('ResourceType', 'ecr-repository');
        cdk.Tags.of(repository).add('Name', `${this.props.departmentName}-${this.props.projectName}`);

        // Output repository details
        new cdk.CfnOutput(this, `${repositoryName}-URI`, {
            value: repository.repositoryUri,
            description: `ECR Repository URI for ${this.props.projectName}`,
            exportName: `${repositoryName}-URI`
        });

        new cdk.CfnOutput(this, `${repositoryName}-Name`, {
            value: repository.repositoryName,
            description: `ECR Repository Name for ${this.props.projectName}`,
            exportName: `${repositoryName}-Name`
        });

        new cdk.CfnOutput(this, `${repositoryName}-ARN`, {
            value: repository.repositoryArn,
            description: `ECR Repository ARN for ${this.props.projectName}`,
            exportName: `${repositoryName}-ARN`
        });

        return repository;
    }

    private addLifecyclePolicy(): void {
        this.repository.addLifecycleRule({
            description: 'Keep only latest 10 images',
            maxImageCount: 10,
            tagStatus: ecr.TagStatus.ANY,
        });

        this.repository.addLifecycleRule({
            description: 'Delete untagged images after 1 day',
            maxImageAge: cdk.Duration.days(1),
            tagStatus: ecr.TagStatus.UNTAGGED,
        });
    }

    // private createGitHubActionsRole(): iam.Role {
    //     const roleName = `${this.props.departmentName}-${this.props.envName}-${this.props.projectName}-github-ecr-role`;
        
    //     const githubRole = new iam.Role(this, roleName, {
    //         roleName: roleName,
    //         assumedBy: new iam.FederatedPrincipal(
    //             `arn:aws:iam::${this.account}:oidc-provider/token.actions.githubusercontent.com`,
    //             {
    //                 StringEquals: {
    //                     'token.actions.githubusercontent.com:aud': 'sts.amazonaws.com',
    //                 },
    //                 StringLike: {
    //                     'token.actions.githubusercontent.com:sub': 'repo:your-github-org/pdf2data-api-serverless:*'
    //                 }
    //             },
    //             'sts:AssumeRoleWithWebIdentity'
    //         ),
    //         inlinePolicies: {
    //             ECRAccess: new iam.PolicyDocument({
    //                 statements: [
    //                     new iam.PolicyStatement({
    //                         sid: 'AllowECRAccess',
    //                         actions: [
    //                             'ecr:BatchCheckLayerAvailability',
    //                             'ecr:BatchGetImage',
    //                             'ecr:CompleteLayerUpload',
    //                             'ecr:GetDownloadUrlForLayer',
    //                             'ecr:InitiateLayerUpload',
    //                             'ecr:PutImage',
    //                             'ecr:UploadLayerPart',
    //                             'ecr:GetAuthorizationToken'
    //                         ],
    //                         resources: [
    //                             this.repository.repositoryArn,
    //                             '*' // GetAuthorizationToken requires * resource
    //                         ]
    //                     })
    //                 ]
    //             })
    //         }
    //     });

    //     // Output GitHub Actions role ARN
    //     new cdk.CfnOutput(this, `${roleName}-ARN`, {
    //         value: githubRole.roleArn,
    //         description: `GitHub Actions Role ARN for ECR access`,
    //         exportName: `${roleName}-ARN`
    //     });

    //     return githubRole;
    // }
}