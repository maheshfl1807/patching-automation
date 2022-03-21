namespace IssueReportService.Importers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon.EC2;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;
    using Amazon.SecurityToken.Model;
    using Csv;
    using Entities;
    using InfraGuard;
    using InfraGuard.Entities;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.Settings;
    using Settings;

    /// <summary>
    /// Import cloud servers into InfraGuard as clusters.
    /// </summary>
    public class InfraGuardImporter : IImporter
    {
        private readonly IEnumerable<string> _validTagKeys;
        private readonly IEnumerable<string> _invalidTagValues;
        private readonly string _excludeProjectName;
        private readonly InfraGuardApi _api;
        private readonly ICredentialsProvider<AmazonCredentials> _amazonCredentialsProvider;

        public InfraGuardImporter(
            ISettings<RootSettings> rootSettings,
            ISettings<InfraGuardSettings> infraGuardSettings,
            InfraGuardApi infraGuardApi,
            ICredentialsProvider<AmazonCredentials> amazonCredentialsProvider)
        {
            _validTagKeys = rootSettings.GetRequired(s => s.ValidPatchTagKeys);
            _invalidTagValues = rootSettings.GetRequired(s => s.InvalidPatchTagValues);
            _excludeProjectName = infraGuardSettings.GetRequired(s => s.ExcludeProjectName);
            _amazonCredentialsProvider = amazonCredentialsProvider;
            _api = infraGuardApi;
        }

        /// <inheritdoc />
        public async Task Import(
            IEnumerable<CloudServer> cloudServers,
            CloudAccount cloudAccount,
            CloudProvider cloudProvider,
            ConcurrentBag<ServerIssueReport> serverIssues,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues)
        {
            var orphanedServers = new List<CloudServer>();
            var missingTagServers = new List<CloudServer>();
            var excludedServers = new List<CloudServer>();
            var profileGroupedServers = GetProfileGroupedServers(
                cloudServers, orphanedServers, missingTagServers, excludedServers);

            // Only add to InfraGuard if there is at least one valid server.
            if (orphanedServers.Count + excludedServers.Count < cloudServers.Count())
            {
                foreach (var profileGroup in profileGroupedServers)
                {
                    if (cloudProvider.Name == "AWS")
                    {
                        var instanceProfileName = profileGroup.Key[(profileGroup.Key.LastIndexOf('/') + 1) ..];

                        // TODO: Figure out where to get ExternalId
                        var awsCluster = new AwsCluster
                        {
                            Name = "imported-by-automation",
                            RoleArn = await GetRoleArnFromInstanceProfile(instanceProfileName, cloudAccount.CloudProviderAccountId),
                            ExternalId = "pa-test-external-id",
                            Provider = "AWS",
                        };

                        var awsClusterResponse = await _api.CreateClusterAsync(awsCluster);
                        if (!awsClusterResponse.Success &&
                            !awsClusterResponse.Message.Contains(InfraGuardApi.ClusterAlreadyExistsMessage))
                        {
                            // TODO: error, cluster creation failed for uncaught reason.
                        }
                    }
                    else
                    {
                        // TODO: error, provider not supported.
                    }
                }

                var projectsResponse = await _api.GetProjectsAsync();

                Project excludeProject = null;
                foreach (var project in projectsResponse.Data)
                {
                    if (project.Name == _excludeProjectName)
                    {
                        excludeProject = project;
                        break;
                    }
                }

                if (excludeProject == null)
                {
                    // TODO: If exclude project doesn't exist, do we create it or throw error?
                    throw new Exception($"Project '{_excludeProjectName}' doesn't exist");
                }

                // TODO: Figure out if servers are immediately imported upon cluster creation.
                // TODO: If not, kick off sync and wait until complete before running the following
                var excludedServerIds = excludedServers.Select(server => server.ServerId);
                var serversResponse = await _api.GetServersAsync();
                var serverIds = serversResponse.Data
                    .Where(igServer => excludedServerIds.Contains(igServer.InstanceId))
                    .Select(igServer => igServer.ServerId);

                await _api.AssignServersToProjectAsync(excludedServerIds, excludeProject.ProjectId);
            }
        }

        private async Task<string> GetRoleArnFromInstanceProfile(string awsInstanceProfileName, string awsAccountId)
        {
            var awsCredentials = (await System.Linq.AsyncEnumerable.ToListAsync(_amazonCredentialsProvider.Credentials(awsAccountId)))
                .FirstOrDefault();

            if (awsCredentials == null)
            {
                throw new Exception($"Missing credentials for AWS account {awsAccountId}.");
            }

            using var iamClient = new AmazonIdentityManagementServiceClient(new Credentials
            {
                AccessKeyId = awsCredentials.AccessKeyId,
                SecretAccessKey = awsCredentials.SecretAccessKey,
                SessionToken = awsCredentials.SessionToken,
            });

            var instanceProfileRequest = new GetInstanceProfileRequest
            {
                InstanceProfileName = awsInstanceProfileName,
            };
            var instanceProfileResponse = await iamClient.GetInstanceProfileAsync(instanceProfileRequest);

            return instanceProfileResponse.InstanceProfile.Roles.First().Arn;
        }

        /// <summary>
        /// Groups IEnumberable of CloudServer into Dictionary keyed by ProfileId.
        /// </summary>
        /// <param name="cloudServers">A list of servers exported from a cloud provider.</param>
        /// <param name="orphanedServers">A list of servers that are missing a profile id.</param>
        /// <param name="missingTagServers">A list of servers missing the patch tag.</param>
        /// <param name="excludedServers">A list of servers not meeting criteria to be patchable.</param>
        /// <returns>A dictionary of lists of servers keyed to their profile ids.</returns>
        private Dictionary<string, List<CloudServer>> GetProfileGroupedServers(
            IEnumerable<CloudServer> cloudServers,
            List<CloudServer> orphanedServers,
            List<CloudServer> missingTagServers,
            List<CloudServer> excludedServers)
        {
            var profileGroups = new Dictionary<string, List<CloudServer>>();

            foreach (var cloudServer in cloudServers)
            {
                if (cloudServer.ProfileId != null)
                {
                    if (profileGroups.TryGetValue(cloudServer.ProfileId, out var profileCloudServers) == false)
                    {
                        profileCloudServers = new List<CloudServer> { cloudServer };
                        profileGroups.Add(cloudServer.ProfileId, profileCloudServers);
                    }
                    else
                    {
                        profileCloudServers.Add(cloudServer);
                    }

                    CheckServerTags(cloudServer, missingTagServers, excludedServers);
                }
                else
                {
                    orphanedServers.Add(cloudServer);
                }
            }

            return profileGroups;
        }

        /// <summary>
        /// Determines if server should appear in missing tag report or should be marked as excluded.
        /// </summary>
        /// <param name="cloudServer">The server being checked.</param>
        /// <param name="missingTagServers">A list of servers missing the patch tag.</param>
        /// <param name="excludedServers">A list of servers not meeting criteria to be patchable.</param>
        private void CheckServerTags(CloudServer cloudServer, List<CloudServer> missingTagServers, List<CloudServer> excludedServers)
        {
            var validTagKeyFound = false;
            var validTagValueFound = false;

            foreach (var cloudServerTag in cloudServer.CloudServerTags)
            {
                var isValidTagKey = _validTagKeys.Any(validTagKey => string.Equals(
                    validTagKey, cloudServerTag.Key, StringComparison.OrdinalIgnoreCase));
                if (isValidTagKey)
                {
                    validTagKeyFound = true;

                    var isInvalidTagValue = _invalidTagValues.Any(invalidTagValue => string.Equals(
                        invalidTagValue, cloudServerTag.Value, StringComparison.OrdinalIgnoreCase));
                    if (!isInvalidTagValue)
                    {
                        validTagValueFound = true;
                    }
                }
            }

            if (!validTagKeyFound)
            {
                missingTagServers.Add(cloudServer);
            }

            if (!validTagKeyFound || !validTagValueFound)
            {
                excludedServers.Add(cloudServer);
            }
        }
    }
}