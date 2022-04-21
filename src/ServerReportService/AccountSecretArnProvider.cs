namespace ServerReportService
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Dapper;
    using Data;
    using LaunchSharp.AccountAccess;
    using LaunchSharp.AccountAccess.AmazonIAM;
    using ServerReportService.Models;

    internal class AccountSecretArnProvider : IAccountSecretArnProvider<AmazonCredentials>
    {
        private readonly PlatformConnectionFactory _factory;

        public AccountSecretArnProvider(PlatformConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<AccountAccess>> GetAccountAccessArns(string accountId)
        {
            using (var executor = _factory.CreateConnection())
            {
                var results = await executor.QueryAsync<MCSAccountAccess>(@"SELECT 
                            SecretArn AS 'AccessArn',
                            Source 
                            FROM AccountAccessItem 
                                INNER JOIN Account 
                                ON Account.Id = AccountAccessItem.AccountId 
                            WHERE Account.AccountId = @AccountId 
                                AND IsEnabled = 1 
                                AND Source = 'MCSIAMRole'
                            ", new { AccountId = accountId });
                return (IEnumerable<AccountAccess>)results;
            }
        }
    }
}
