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
    using Dasync.Collections;
    using ImportService.Data;
    using ImportService.Entities;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using Microsoft.EntityFrameworkCore;
    using Tag = Amazon.EC2.Model.Tag;

    public class AwsExporter : IExporter
    {
        private const string c_providerName = "AWS";
        private static readonly List<InstanceStateName> s_invalidStates = new ()
        {
            InstanceStateName.ShuttingDown,
            InstanceStateName.Terminated,
        };

        private readonly ICredentialsProvider<AmazonCredentials> _amazonCredentialsProvider;
        private readonly IDbContextFactory<ImportServiceContext> _contextFactory;
        private CloudProvider _cachedAwsProvider;

        public AwsExporter(
            ICredentialsProvider<AmazonCredentials> amazonCredentialsProvider,
            IDbContextFactory<ImportServiceContext> contextFactory)
        {
            _amazonCredentialsProvider = amazonCredentialsProvider;
            _contextFactory = contextFactory;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CloudAccount>> GetCloudAccounts()
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            var awsProvider = await GetAwsProvider(context);

            // TODO: REMOVE TEST ACCOUNT ID
            return await context.CloudAccount
                .Where(a => a.CloudProviderId == awsProvider.Id && a.CloudProviderAccountId == "061165946885")
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CloudServer>> GetCloudServers(CloudAccount account)
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

            // TODO: Investigate if this runs slowly enough for AWS API / do retry logic.
            do
            {
                var pageTask = describeInstancesResponse.Reservations.ParallelForEachAsync(async awsReservation =>
                {
                    await awsReservation.Instances.ParallelForEachAsync(awsInstance =>
                    {
                        return Task.Factory.StartNew(() =>
                        {
                            if (!s_invalidStates.Contains(awsInstance.State.Name))
                            {
                                cloudServers.Add(new CloudServer
                                {
                                    CloudAccount = account,
                                    CloudAccountId = account.Id,
                                    CloudServerTags = ConvertAwsTagsToCloudServerTags(awsInstance.Tags),
                                    ProfileId = awsInstance.IamInstanceProfile?.Arn,
                                    ServerId = awsInstance.InstanceId,
                                });
                            }
                        });
                    });
                });

                pageTasks.Add(pageTask);

                awsDescribeInstancesRequest.NextToken = describeInstancesResponse.NextToken;
            }
            while (awsDescribeInstancesRequest.NextToken != null);

            await Task.WhenAll(pageTasks.ToArray());

            return cloudServers;
        }

        private async Task<CloudProvider> GetAwsProvider(ImportServiceContext context)
        {
            _cachedAwsProvider ??= await context.CloudProvider.FirstOrDefaultAsync(
                p => p.Name == c_providerName);

            return _cachedAwsProvider;
        }
    }
}