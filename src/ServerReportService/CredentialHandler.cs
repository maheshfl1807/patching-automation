namespace ServerReportService
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;

    using Microsoft.Extensions.Logging;

    public class CredentialHandler
    {
        private readonly IAccountSecretArnProvider<AmazonCredentials> _awsAccountSecretArnProvider;
        private readonly ILogger _logger;

        public CredentialHandler(
            IAccountSecretArnProvider<AmazonCredentials> awsAccountSecretArnProvider,
            ILogger logger)
        {
            _awsAccountSecretArnProvider = awsAccountSecretArnProvider;
            _logger = logger;
        }

        public async Task<(string arn, string externalId, Credentials credentials)> GetAccountCredentials(string accountId)
        {
            try
            {
                var accountAccessItem = (await this._awsAccountSecretArnProvider.GetAccountAccessArns(accountId))?.FirstOrDefault();

                if (accountAccessItem is null)
                {
                    throw new ArgumentNullException($"No valid account access item was found for account '{accountId}'.");
                }

                // Below method currently assumes credentials are AWS-specific but long-term we'll want to either overload logic to handle all providers (AWS/Azure/GCP/etc) or provide logic to perform relevant lookup
                var credentialsParseResults = this.ParseAwsRoleCredentials(accountAccessItem, accountId);

                return (credentialsParseResults.arn, credentialsParseResults.externalId, await this.AssumeRole(credentialsParseResults.arn, credentialsParseResults.externalId).ConfigureAwait(false));

            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"An error occurred when assuming role for account {accountId}: {e.Message}");
                return ("", "", null);
            }
        }

        public async Task<Credentials> RefreshCredentialsIfNeeded(string arn, string externalId, Credentials credentials)
        {
            if (credentials.Expiration.ToUniversalTime() <= DateTime.UtcNow.AddMinutes(10))
            {
                credentials = await this.AssumeRole(arn, externalId).ConfigureAwait(false);
            }

            return credentials;
        }

        private async Task<Credentials> AssumeRole(string arn, string externalId)
        {
            var defaultClient = new AmazonSecurityTokenServiceClient();
            // could be useful to pull below rolearn + externalId into app settings for more flexibility
            var platformAutomationAssumeRequest = new AssumeRoleRequest
            {
                RoleArn = $"arn:aws:iam::536269885160:role/2WPlatformAutomationAssumeRoleRole",
                RoleSessionName = "PatchingAutomation",
                ExternalId = "pzf3apb53cqaa27qx9f5mdad2hz63a3nm4th3eeqd6puqx5wpqscwg3w8dy8wy4m",
            };
            var platformAutomationAssumeResponse = await defaultClient.AssumeRoleAsync(platformAutomationAssumeRequest);

            using var securityTokenClient = new AmazonSecurityTokenServiceClient(platformAutomationAssumeResponse.Credentials);
            var customerAssumeRequest = new AssumeRoleRequest
            {
                RoleArn = arn,
                RoleSessionName = "PatchingAutomation",
                DurationSeconds = 3600,
                ExternalId = externalId,
            };
            var customerAssumeReponse = await securityTokenClient.AssumeRoleAsync(customerAssumeRequest);

            return customerAssumeReponse.Credentials;
        }

        private (string arn, string externalId) ParseAwsRoleCredentials(AccountAccess accessItem, string accountId)
        {
            this._logger.LogDebug($"Trying to assume role for {accessItem.Source} access item {accessItem.AccessArn}");
            var split = accessItem.AccessArn.Split('#');
            var externalId = split.Length > 1 ? split[1] : null;
            // example: "arn:aws:iam::{accountId}:role/2ndWatch/2WMSAdminRole#externalId"
            return ($"arn:aws:iam::{accountId}:role/{split[0]}", externalId);
        }
    }
}