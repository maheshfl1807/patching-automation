namespace ImportService.Exporters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.EC2;
    using Amazon.SecurityToken.Model;
    using ImportService.Data;
    using ImportService.Entities;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using Microsoft.EntityFrameworkCore;
    using Tag = Amazon.EC2.Model.Tag;

    public class AwsExporter : IExporter
    {
        private const string ProviderName = "AWS";

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
            return await context.CloudAccount
                    .Where(a => a.CloudProviderId == awsProvider.Id && a.CloudProviderAccountId == "061165946885")
                    .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CloudServer>> GetCloudServers(CloudAccount account)
        {
            var awsCredentials = (await _amazonCredentialsProvider.Credentials(account.CloudProviderAccountId).ToListAsync())
                .FirstOrDefault();

            if (awsCredentials == null)
            {
                throw new Exception($"Missing credentials for {ProviderName} account {account}.");
            }

            using var ec2Client = new AmazonEC2Client(new Credentials
            {
                AccessKeyId = awsCredentials.AccessKeyId,
                SecretAccessKey = awsCredentials.SecretAccessKey,
                SessionToken = awsCredentials.SessionToken,
            });

            var awsDescribeInstancesResponse = await ec2Client.DescribeInstancesAsync();
            var cloudServers = new List<CloudServer>();
            foreach (var awsReservation in awsDescribeInstancesResponse.Reservations)
            {
                foreach (var awsInstance in awsReservation.Instances)
                {
                    cloudServers.Add(new CloudServer
                    {
                        ServerId = awsInstance.InstanceId,
                        ProfileId = awsInstance.IamInstanceProfile?.Arn,
                        CloudServerTags = ConvertAwsTagsToCloudServerTags(awsInstance.Tags),
                    });
                }
            }

            return cloudServers;
        }

        private static IEnumerable<CloudServerTag> ConvertAwsTagsToCloudServerTags(IEnumerable<Tag> awsTags)
        {
            return awsTags
                .Select(awsTag => new CloudServerTag { Key = awsTag.Key, Value = awsTag.Value, IsCustom = false })
                .ToList();
        }

        private async Task<CloudProvider> GetAwsProvider(ImportServiceContext context)
        {
            _cachedAwsProvider ??= await context.CloudProvider.FirstOrDefaultAsync(
                p => p.Name == ProviderName);

            return _cachedAwsProvider;
        }
    }
}