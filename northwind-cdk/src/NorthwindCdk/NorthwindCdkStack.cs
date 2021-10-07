using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.RDS;
using Amazon.CDK.AWS.SSM;

namespace NorthwindCdk
{
    public class NorthwindCdkStack : Stack
    {
        internal NorthwindCdkStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
           
            var vpc = Vpc.FromLookup(this, "NorthwindVPC", new VpcLookupOptions { VpcId = "vpc-xxxxxxx" });
            
            // SQL Server
            var sg = new SecurityGroup(this, "NorthwindDatabaseSecurityGroup", new SecurityGroupProps
            {
                Vpc = vpc,

                SecurityGroupName = "Northwind-DB-SG",
                AllowAllOutbound = false
            });

            // PostgreSQL setup

            var vpcCidrBlock = vpc.VpcCidrBlock;
            // !!!!!!!!!! add 2 rules when you use provided VM, add 1 rule when you use your computer
            sg.AddIngressRule(Peer.Ipv4(vpcCidrBlock), Port.Tcp(5432)); // PostgreSQL
            // !!!!!!!!!! 

            var postgreSql = new DatabaseCluster(this, "NorthwindPostgreSQL", new DatabaseClusterProps
            {
                InstanceProps = new Amazon.CDK.AWS.RDS.InstanceProps
                {
                    Vpc = vpc,
                    // t3.medium
                    InstanceType = InstanceType.Of(InstanceClass.BURSTABLE3, InstanceSize.MEDIUM),
                    SecurityGroups = new ISecurityGroup[] { sg },
                    // you need to access database from your developer PC
                    VpcSubnets = new SubnetSelection() { SubnetType = SubnetType.PUBLIC },
                    ParameterGroup = ParameterGroup.FromParameterGroupName(this, "DBInstanceParameterGroup", "default.aurora-postgresql11"),
                },
                ParameterGroup = ParameterGroup.FromParameterGroupName(this, "DBClusterParameterGroup", "default.aurora-postgresql11"),
                ClusterIdentifier = "northwind-postgresql",

                // Aurora PostgreSQL 11.9
                Engine = DatabaseClusterEngine.AuroraPostgres(
                        new AuroraPostgresClusterEngineProps
                        {
                            Version = AuroraPostgresEngineVersion.VER_11_9
                        }),

                Credentials = Credentials.FromPassword(
                    username: "",
                    password: new SecretValue("")),

                Instances = 1,
                Port = 5432,

                Backup = new BackupProps
                {
                    Retention = Duration.Days(1) // minimum is 1
                },

                DefaultDatabaseName = "NorthwindTraders",
                InstanceIdentifierBase = "northwind-postgresql-instance",

                RemovalPolicy = RemovalPolicy.DESTROY // you need to be able to delete database,               
            });
        }
    }
}

