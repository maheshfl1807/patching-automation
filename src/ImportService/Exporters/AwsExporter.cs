namespace ImportService.Exporters
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.EC2;
    using Amazon.EC2.Model;
    using Amazon.SecurityToken.Model;
    using ImportService.Data;
    using ImportService.Entities;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using Microsoft.EntityFrameworkCore;
    using Tag = Amazon.EC2.Model.Tag;

    public class AwsExporter : AbstractExporter
    {
        private const string c_providerName = "AWS";

        private static readonly List<InstanceStateName> s_invalidStates = new ()
        {
            InstanceStateName.ShuttingDown,
            InstanceStateName.Terminated,
        };

        private readonly ICredentialsProvider<AmazonCredentials> _amazonCredentialsProvider;

        private readonly IDbContextFactory<ImportServiceContext> _contextFactory;

        public AwsExporter(
            ICredentialsProvider<AmazonCredentials> amazonCredentialsProvider,
            IDbContextFactory<ImportServiceContext> contextFactory)
            : base(contextFactory, c_providerName)
        {
            _amazonCredentialsProvider = amazonCredentialsProvider;
            _contextFactory = contextFactory;
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<CloudAccount>> GetCloudAccounts(IEnumerable<string> accountIdsFilter)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var awsProvider = await GetCloudProvider();
            var baseQuery = context.CloudAccount
                .Where(a => a.CloudProviderId == awsProvider.Id);

            if (accountIdsFilter != null)
            {
                baseQuery = baseQuery.Where(a => accountIdsFilter.Contains(a.CloudProviderAccountId));
            }

            return await baseQuery.ToListAsync();
        }

        /// <inheritdoc />
        public override async Task<IEnumerable<CloudServer>> GetCloudServers(
            CloudAccount account,
            IEnumerable<string> serverIdsFilter)
        {
            var awsCredentials = (await System.Linq.AsyncEnumerable.ToListAsync(_amazonCredentialsProvider.Credentials(account.CloudProviderAccountId)))
                .FirstOrDefault();

            if (awsCredentials == null)
            {
                throw new Exception($"Missing credentials for {c_providerName} account {account}.");
            }

            using var ec2Client = new AmazonEC2Client(new Credentials
            {
                AccessKeyId = awsCredentials.AccessKeyId,
                SecretAccessKey = awsCredentials.SecretAccessKey,
                SessionToken = awsCredentials.SessionToken,
            });

            var awsDescribeInstancesRequest = new DescribeInstancesRequest();
            if (serverIdsFilter != null)
            {
                awsDescribeInstancesRequest.InstanceIds = serverIdsFilter.ToList();
            }

            var awsDescribeInstancesResponse = await ec2Client.DescribeInstancesAsync(awsDescribeInstancesRequest);

            return await ConvertResponseToCloudServersAsync(awsDescribeInstancesResponse, account);
        }

        private static IEnumerable<CloudServerTag> ConvertAwsTagsToCloudServerTags(IEnumerable<Tag> awsTags)
        {
            return awsTags
                .Select(awsTag => new CloudServerTag { Key = awsTag.Key, Value = awsTag.Value, IsCustom = false })
                .ToList();
        }

        private async Task<IEnumerable<CloudServer>> ConvertResponseToCloudServersAsync(DescribeInstancesResponse describeInstancesResponse, CloudAccount account)
        {
            var pageTasks = new List<Task>();
            var awsDescribeInstancesRequest = new DescribeInstancesRequest();
            var cloudServers = new ConcurrentBag<CloudServer>();

            await using var context = await _contextFactory.CreateDbContextAsync();

            // TODO: Investigate if this runs slowly enough for AWS API to not get throttled. If it doesn't, add retry logic.
            do
            {
                var pageTask = Task.Factory.StartNew(() => describeInstancesResponse.Reservations.AsParallel().ForEach(awsReservation =>
                {
                    awsReservation.Instances.AsParallel().ForEach(awsInstance =>
                    {
                        if (!s_invalidStates.Contains(awsInstance.State.Name))
                        {
                            var newCloudServer = new CloudServer
                            {
                                CloudAccountId = account.Id,
                                ProfileId = awsInstance.IamInstanceProfile?.Arn,
                                ServerId = awsInstance.InstanceId,
                                CloudServerTags = ConvertAwsTagsToCloudServerTags(awsInstance.Tags),
                            };

                            cloudServers.Add(newCloudServer);
                        }
                    });
                }));

                pageTasks.Add(pageTask);

                awsDescribeInstancesRequest.NextToken = describeInstancesResponse.NextToken;
            }
            while (awsDescribeInstancesRequest.NextToken != null);

            await Task.WhenAll(pageTasks.ToArray());

            return cloudServers;
        }
    }
}