import * as cdk from 'aws-cdk-lib';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import { Construct } from 'constructs';
import { PREFIXES, VpcCreationParams } from './models';

export class VpcStack extends cdk.Stack {

  private readonly _vpc: ec2.Vpc; // Store VPC reference

  constructor(scope: Construct, id: string, private props: cdk.StackProps & VpcCreationParams ) {
    super(scope, id, props);

    this._vpc = this.createVpcAndSubnets();
  }

  public getVpc(): ec2.Vpc{
    return this._vpc;
  }

  private createVpcAndSubnets() : ec2.Vpc {
    const vpcName: string = `${PREFIXES.VPC}-${this.props.departmentName}-${this.props.envName}`;

    const vpc = new ec2.Vpc(this, vpcName, {
      maxAzs: 1, // Default is all AZs in region
      ipAddresses: ec2.IpAddresses.cidr('10.0.0.0/16'),
      subnetConfiguration: [
        {
          cidrMask: 24,
          name: `${PREFIXES.SUBNET}-${this.props.departmentName}-${this.props.envName}-public`,
          subnetType: ec2.SubnetType.PUBLIC,
          mapPublicIpOnLaunch: true,
        },
        {
          cidrMask: 24,
          name: `${PREFIXES.SUBNET}-${this.props.departmentName}-${this.props.envName}-private`,
          subnetType: ec2.SubnetType.PRIVATE_WITH_EGRESS,
        },
        {
          cidrMask: 24,
          name: `${PREFIXES.SUBNET}-${this.props.departmentName}-${this.props.envName}-data`,
          subnetType: ec2.SubnetType.PRIVATE_ISOLATED,          
        }
      ]
    });

    //Add tags only to VPC (not inherited by subnets)
    const cfnVpc = vpc.node.defaultChild as ec2.CfnVPC;
    cfnVpc.addPropertyOverride('Tags', [
      { Key: 'Name', Value: vpcName },
      { Key: 'resource-type', Value: 'vpc' }
      // Note: project and environment tags already applied via stack-level default tags
    ]);

    return vpc;
  } 

}
