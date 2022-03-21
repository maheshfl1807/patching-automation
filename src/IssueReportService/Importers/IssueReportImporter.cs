namespace IssueReportService.Importers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.EC2;
    using Amazon.IdentityManagement;
    using Amazon.IdentityManagement.Model;
    using Amazon.SecurityToken.Model;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;
    using Csv;
    using Data;
    using Entities;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Settings;

    public class IssueReportImporter : IImporter
    {
        private const string c_profileIssue = "Server is missing profile.";
        private const string c_missingTagIssue = "Server is missing patching tag.";
        private const string c_ssmConnectionIssue = "Cannot connect to server with SSM.";
        private readonly IEnumerable<string> _validTagKeys;
        private readonly IEnumerable<string> _invalidTagValues;
        private readonly IDbContextFactory<IssueReportServiceContext> _contextFactory;
        private readonly ILogger _logger;
        private readonly CredentialHandler _credentialHandler;

        public IssueReportImporter(
            ISettings<RootSettings> rootSettings,
            ILogger logger,
            IDbContextFactory<IssueReportServiceContext> contextFactory,
            CredentialHandler credentialHandler)
        {
            _validTagKeys = rootSettings.GetRequired(s => s.ValidPatchTagKeys);
            _invalidTagValues = rootSettings.GetRequired(s => s.InvalidPatchTagValues);
            _logger = logger;
            _contextFactory = contextFactory;
            _credentialHandler = credentialHandler;
        }

        /// <inheritdoc />
        public async Task Import(
            IEnumerable<CloudServer> cloudServers,
            CloudAccount cloudAccount,
            CloudProvider cloudProvider,
            ConcurrentBag<ServerIssueReport> serverIssues,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues)
        {
            var cloudServersByServerId = new ConcurrentDictionary<string, CloudServer>();

            // Build dictionary of cloud servers keyed by ServerId. Also populate
            // list of any server configuration issues.
            var serverIssuesTasks = new ConcurrentBag<Task>();
            cloudServers.AsParallel()
                .ForEach(cloudServer =>
                {
                    cloudServersByServerId.TryAdd(cloudServer.ServerId, cloudServer);

                    serverIssuesTasks.Add(UpdateServerIssues(serverIssues, accountsWithCredentialIssues, cloudServer, cloudAccount, cloudProvider));
                });
            await Task.WhenAll(serverIssuesTasks);

            // await TrackAndPublishServerChanges(cloudServersByServerId, cloudAccount);
        }

        // private async Task GetCredentials(CloudAccount awsCloudAccount)
        // {
        //     var awsCredentials = (await System.Linq.AsyncEnumerable.ToListAsync(_amazonCredentialsProvider.Credentials(awsCloudAccount.CloudProviderAccountId)))
        //         .FirstOrDefault();
        //
        //     if (awsCredentials == null)
        //     {
        //         throw new Exception($"Missing credentials for AWS account {awsCloudAccount.CloudProviderAccountId}.");
        //     }
        //
        //     using var ec2Client = new AmazonEC2Client(new Credentials
        //     {
        //         AccessKeyId = awsCredentials.AccessKeyId,
        //         SecretAccessKey = awsCredentials.SecretAccessKey,
        //         SessionToken = awsCredentials.SessionToken,
        //     });
        // }

        private async Task TrackAndPublishServerChanges(ConcurrentDictionary<string, CloudServer> cloudServersByServerId, CloudAccount cloudAccount)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();

            // Get already existing servers for account.
            var servers = await context.CloudServer.Where(cloudServer =>
                cloudServer.CloudAccount.CloudProviderAccountId == cloudAccount.CloudProviderAccountId
                && cloudServersByServerId.Keys.Contains(cloudServer.ServerId))
                .Include(cloudServer => cloudServer.CloudServerTags)
                .ToListAsync();

            // Iterate servers in context and update them based on newly imported servers.
            foreach (var server in servers)
            {
                if (cloudServersByServerId.ContainsKey(server.ServerId))
                {
                    var newServer = cloudServersByServerId[server.ServerId];
                    newServer.Id = server.Id;

                    // Update scalar properties.
                    context.Entry(server).CurrentValues.SetValues(newServer);

                    // Update tags.
                    foreach (var tag in server.CloudServerTags)
                    {
                        var newTag = newServer.CloudServerTags.FirstOrDefault(newTag => newTag.Key == tag.Key);
                        if (newTag != null)
                        {
                            tag.Value = newTag.Value;
                        }
                        else
                        {
                            context.CloudServerTag.Remove(tag);
                        }
                    }

                    cloudServersByServerId.Remove(server.ServerId, out _);
                }
                else
                {
                    context.CloudServer.Remove(server);
                }
            }

            // Add any remaining imported servers to context.
            await context.CloudServer.AddRangeAsync(cloudServersByServerId.Values);

            if (context.ChangeTracker.HasChanges())
            {
                // Build list of tasks to produce events after saving changes.
                var produceTasks = new ConcurrentBag<Task>();

                context.ChangeTracker.Entries()
                    .AsParallel()
                    .ForEach(entity =>
                    {
                        if (entity.State == EntityState.Added)
                        {
                            // TODO: Create "added cloud server" event.
                            // TODO: Change task to be producing the above event.
                            produceTasks.Add(new Task(() => { _logger.LogInformation("added"); }));
                        }
                        else if (entity.State == EntityState.Modified)
                        {
                            // TODO: Create "modified cloud server" event.
                            // TODO: Change task to be producing the above event.
                            produceTasks.Add(new Task(() => { _logger.LogInformation("modified"); }));
                        }
                        else if (entity.State == EntityState.Deleted)
                        {
                            // TODO: Create "deleted cloud server" event.
                            // TODO: Change task to be producing the above event.
                            produceTasks.Add(new Task(() => { _logger.LogInformation("deleted"); }));
                        }
                    });

                await context.SaveChangesAsync();

                produceTasks.AsParallel().ForEach(task => task.Start());
            }
        }

        private async Task UpdateServerIssues(
            ConcurrentBag<ServerIssueReport> serverIssues,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues,
            CloudServer cloudServer,
            CloudAccount cloudAccount,
            CloudProvider cloudProvider)
        {
            var validTagKeyFound = false;
            var validTagValueFound = false;
            cloudServer.CloudServerTags.AsParallel().ForEach(cloudServerTag =>
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
            });

            if (!validTagKeyFound)
            {
                serverIssues.Add(new ServerIssueReport
                {
                    AccountId = cloudAccount.CloudProviderAccountId,
                    Region = cloudServer.Region,
                    Profile = cloudServer.ProfileId,
                    ServerId = cloudServer.ServerId,
                    Issue = c_missingTagIssue,
                    Status = cloudServer.IsRunning ? "Online" : "Offline",
                    ProviderName = cloudProvider.Name,
                });
            }
            else if (cloudServer.ProfileId == null && validTagValueFound)
            {
                serverIssues.Add(new ServerIssueReport
                {
                    AccountId = cloudAccount.CloudProviderAccountId,
                    Region = cloudServer.Region,
                    Profile = cloudServer.ProfileId,
                    ServerId = cloudServer.ServerId,
                    Issue = c_profileIssue,
                    Status = cloudServer.IsRunning ? "Online" : "Offline",
                    ProviderName = cloudProvider.Name,
                });
            }
            else if (cloudProvider.Name == "AWS" && validTagValueFound)
            {
                var regionEndpoint = RegionEndpoint.GetBySystemName(cloudServer.Region);
                var credentials = await _credentialHandler.GetAccountCredentials(cloudAccount.CloudProviderAccountId, regionEndpoint);

                if (credentials != null)
                {
                    var awsSsmClient = new AmazonSimpleSystemsManagementClient(credentials, regionEndpoint);
                    var getConnectionStatusRequest = new GetConnectionStatusRequest
                    {
                        Target = cloudServer.ServerId,
                    };
                    var getConnectionStatusResponse = await awsSsmClient.GetConnectionStatusAsync(getConnectionStatusRequest);

                    if (getConnectionStatusResponse.Status == ConnectionStatus.NotConnected)
                    {
                        var issueMessage = c_ssmConnectionIssue;
                        var describeInstanceInformationRequest = new DescribeInstanceInformationRequest
                        {
                            Filters = new List<InstanceInformationStringFilter>
                            {
                                new ()
                                {
                                    Key = "InstanceIds",
                                    Values = new List<string> { cloudServer.ServerId },
                                },
                            },
                        };
                        var describeInstanceInformationResponse =
                            await awsSsmClient.DescribeInstanceInformationAsync(describeInstanceInformationRequest);

                        if (!describeInstanceInformationResponse.InstanceInformationList.IsEmpty())
                        {
                            var instanceInformation = describeInstanceInformationResponse.InstanceInformationList[0];
                            var agentVersion = new Version(instanceInformation.AgentVersion);
                            var minVersion = new Version("2.3.0.0");
                            if (agentVersion.CompareTo(minVersion) < 0)
                            {
                                issueMessage += " Agent version does not support Session Manager.";
                            }
                            else if (instanceInformation.PingStatus != PingStatus.Online)
                            {
                                issueMessage += " SSM agent is not reachable.";
                            }
                            else
                            {
                                issueMessage += " Cause is unknown. SSM agent version and reachability are both valid.";
                            }
                        }
                        else
                        {
                            // TODO: Once we have appropriate permissions to iam:GetInstanceProfile and iam:ListRolePolicies,
                            // TODO: remove the below issueMessage line and uncomment the commented code.
                            issueMessage += " Server is not showing as managed. Most likely cause is AmazonSSMManagedInstanceCore " +
                                            "policy is not attached to instance profile role. Alternate causes include: Instance " +
                                            "is deregistered, SSM agent is not installed.";

                            // issueMessage += " Server is not showing as managed. ";
                            // using var iamClient = new AmazonIdentityManagementServiceClient(credentials, regionEndpoint);
                            //
                            // if (cloudServer.ProfileId != null)
                            // {
                            //     var instanceProfileName =
                            //         cloudServer.ProfileId[(cloudServer.ProfileId.LastIndexOf('/') + 1) ..];
                            //
                            //     var role = await GetRoleFromInstanceProfile(iamClient, instanceProfileName);
                            //
                            //     var listRolePoliciesRequest = new ListRolePoliciesRequest
                            //     {
                            //         RoleName = role.RoleName,
                            //     };
                            //     var listRolePoliciesResponse = await iamClient.ListRolePoliciesAsync(listRolePoliciesRequest);
                            //
                            //     if (!listRolePoliciesResponse.PolicyNames.Contains("AmazonSSMManagedInstanceCore"))
                            //     {
                            //         issueMessage += " Instance profile role does not have AmazonSSMManagedInstanceCore policy assigned.";
                            //     }
                            //     else
                            //     {
                            //         issueMessage += " Server is deregistered, or SSM agent is stopped or not installed.";
                            //     }
                            // }
                        }

                        serverIssues.Add(new ServerIssueReport
                        {
                            AccountId = cloudAccount.CloudProviderAccountId,
                            Region = cloudServer.Region,
                            Profile = cloudServer.ProfileId,
                            ServerId = cloudServer.ServerId,
                            Issue = issueMessage,
                            Status = cloudServer.IsRunning ? "Online" : "Offline",
                            ProviderName = cloudProvider.Name,
                        });
                    }
                }
                else
                {
                    accountsWithCredentialIssues[cloudAccount.CloudProviderAccountId] = true;
                }
            }
        }

        private async Task<Role> GetRoleFromInstanceProfile(AmazonIdentityManagementServiceClient iamClient, string awsInstanceProfileName)
        {
            var instanceProfileRequest = new GetInstanceProfileRequest
            {
                InstanceProfileName = awsInstanceProfileName,
            };
            var instanceProfileResponse = await iamClient.GetInstanceProfileAsync(instanceProfileRequest);

            return instanceProfileResponse.InstanceProfile.Roles.First();
        }
    }
}