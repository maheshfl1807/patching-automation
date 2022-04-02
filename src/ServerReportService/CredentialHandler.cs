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
            Credentials customerCredentials = null;
            try
            {

                var defaultClient = new AmazonSecurityTokenServiceClient();
                var platformAutomationAssumeRequest = new AssumeRoleRequest
                {
                    RoleArn = $"arn:aws:iam::536269885160:role/2WPlatformAutomationAssumeRoleRole",
                    RoleSessionName = "PatchingAutomation",
                    ExternalId = "pzf3apb53cqaa27qx9f5mdad2hz63a3nm4th3eeqd6puqx5wpqscwg3w8dy8wy4m",
                };
                var platformAutomationAssumeResponse = await defaultClient.AssumeRoleAsync(platformAutomationAssumeRequest);

                using var securityTokenClient = new AmazonSecurityTokenServiceClient(platformAutomationAssumeResponse.Credentials, region);
                var customerAssumeRequest = new AssumeRoleRequest
                {
                    RoleArn = $"arn:aws:iam::{accountId}:role/2ndWatch/2WMSAdminRole",
                    RoleSessionName = "PatchingAutomation",
                    ExternalId = _accountToExternalIdMap[accountId],
                };
                var customerAssumeReponse = await securityTokenClient.AssumeRoleAsync(customerAssumeRequest);

                customerCredentials = customerAssumeReponse.Credentials;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"An error occurred when assuming role for account {accountId}, region {region.DisplayName}: {e.Message}");
            }

            return customerCredentials;
        }
    }
}