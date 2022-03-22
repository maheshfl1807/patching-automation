namespace IssueReportService
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Runtime.CredentialManagement;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;
    using IssueReportService.Settings;
    using LaunchSharp.Settings;
    using Microsoft.Extensions.Logging;

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

        /*
        printf "AWS_ACCESS_KEY_ID=%s AWS_SECRET_ACCESS_KEY=%s AWS_SESSION_TOKEN=%s" \
          $(aws sts assume-role --role-arn arn:aws:iam::099476554548:role/2ndWatch/2WMSAdminRole \
          --role-session-name PatchingAutomation --profile mcs --region us-east-1 \
          --external-id cb4e8345fc4a467eabfe1b4dff2148ca896d64b23b26eec8a52c46b9ff9c7dfa \
          --query "Credentials.[AccessKeyId,SecretAccessKey,SessionToken]" \
          --output text)
        */
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