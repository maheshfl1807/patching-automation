namespace ServerReportService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Runtime.CredentialManagement;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;
    using Settings;

    public class CredentialHandler
    {
        private readonly Dictionary<string, string> _accountToExternalIdMap;
        private readonly ILogger _logger;

        public CredentialHandler(
            ISettings<RootSettings> rootSettings,
            ILogger logger)
        {
            _accountToExternalIdMap = rootSettings.GetRequired(s => s.AccountToExternalIdMap);
            _logger = logger;
        }

        public string GetCommandMessage()
        {
            return "{\"AccountIds\":[\"" + string.Join("\",\"", _accountToExternalIdMap.Keys) + "\"]}";
        }

        public async Task<Credentials> GetAccountCredentials(string accountId, RegionEndpoint region)
        {
            Credentials credentials = null;

            try
            {
                var chain = new CredentialProfileStoreChain();

                if (chain.TryGetAWSCredentials("mcs", out var mcsCredentials))
                {
                    using var securityTokenClient = new AmazonSecurityTokenServiceClient(mcsCredentials, region);
                    var assumeRoleRequest = new AssumeRoleRequest
                    {
                        RoleArn = $"arn:aws:iam::{accountId}:role/2ndWatch/2WMSAdminRole",
                        RoleSessionName = "PatchingAutomation",
                        ExternalId = _accountToExternalIdMap[accountId],
                    };
                    var assumeRoleResponse = await securityTokenClient.AssumeRoleAsync(assumeRoleRequest);

                    credentials = assumeRoleResponse.Credentials;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"An error occurred when assuming role for account {accountId}, region {region.DisplayName}: {e.Message}");
            }

            return credentials;
        }
    }
}