namespace IssueReportService.Importers.InfraGuard
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;
    using Entities;
    using LaunchSharp.Settings;
    using Responses;
    using RestSharp;
    using RestSharp.Serializers.Json;
    using Settings;

    public class InfraGuardApi
    {
        /// <summary>
        /// Identifying string for "cluster already exists" error message.
        /// </summary>
        public const string ClusterAlreadyExistsMessage = "already exist in company";

        private readonly string _apiDomain;
        private readonly string _apiKey;
        private readonly string _apiUserEmail;
        private readonly string _apiUserPassword;
        private readonly string _apiOriginLicense;
        private readonly Dictionary<string, string> _headers = new ();
        private bool _isAuthenticated = false;

        public InfraGuardApi(ISettings<InfraGuardSettings> infraGuardSettings)
        {
            _apiOriginLicense = infraGuardSettings.GetRequired(s => s.ApiOriginLicense);
            _apiDomain = infraGuardSettings.GetRequired(s => s.ApiDomain);
            _apiKey = infraGuardSettings.GetRequired(s => s.ApiKey);
            _apiUserEmail = infraGuardSettings.GetRequired(s => s.ApiUserEmail);
            _apiUserPassword = infraGuardSettings.GetRequired(s => s.ApiUserPassword);
        }

        public async Task<InfraGuardResponse<ClusterData>> GetClustersAsync()
        {
            var request = new RestRequest("cluster");

            return await MakeRequest<InfraGuardResponse<ClusterData>>(request);
        }

        public async Task<InfraGuardResponse<ClusterData>> CreateClusterAsync(AbstractCluster cluster)
        {
            var request = new RestRequest("cluster", Method.Post)
                .AddJsonBody(cluster);

            return await MakeRequest<InfraGuardResponse<ClusterData>>(request);
        }

        public async Task<InfraGuardResponse<IEnumerable<Server>>> GetServersAsync()
        {
            var request = new RestRequest("cluster/servers", Method.Post);

            return await MakeRequest<InfraGuardResponse<IEnumerable<Server>>>(request);
        }

        public async Task<InfraGuardResponse<IEnumerable<Project>>> GetProjectsAsync()
        {
            var request = new RestRequest("project");

            return await MakeRequest<InfraGuardResponse<IEnumerable<Project>>>(request);
        }

        public async Task<InfraGuardResponse<string>> AssignServersToProjectAsync(IEnumerable<string> serverIds, string projectId)
        {
            var request = new RestRequest("project/assignservers/{project_id}", Method.Put)
                .AddUrlSegment("project_id", projectId)
                .AddJsonBody(new { servers = serverIds });

            return await MakeRequest<InfraGuardResponse<string>>(request);
        }

        private async Task AuthenticateAsync()
        {
            _headers.TryAdd("requested-origin-license", _apiOriginLicense);

            // TODO: Handle refresh token.
            var client = GetClient();

            var authenticateRequest = new RestRequest("authenticate")
                .AddHeaders(_headers)
                .AddJsonBody(new
                {
                    type = "Basic",
                    email = _apiUserEmail,
                    password = _apiUserPassword,
                    apikey = _apiKey,
                    source = "infraguard",
                });
            var authenticateResponse =
                await client.PostAsync<InfraGuardResponse<AuthenticateData>>(authenticateRequest);

            if (authenticateResponse != null && authenticateResponse.Success)
            {
                var authVerificationRequest = new RestRequest("authverification")
                    .AddHeaders(_headers)
                    .AddJsonBody(authenticateResponse.Data);
                var authVerificationResponse = await client.PostAsync<InfraGuardResponse<AuthVerificationData>>(authVerificationRequest);

                if (authVerificationResponse != null && authVerificationResponse.Success)
                {
                    _headers.Remove("Authorization");
                    _headers.Add("Authorization", authVerificationResponse.Data.Token);
                    _isAuthenticated = true;
                }
                else
                {
                    // TODO: Handle error.
                }
            }
            else
            {
                // TODO: Handle error.
            }
        }

        private async Task<TResponse> MakeRequest<TResponse>(RestRequest request)
        {
            if (!_isAuthenticated)
            {
                await AuthenticateAsync();
            }

            request.AddHeaders(_headers);

            var client = GetClient();
            var jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            };
            client.UseSystemTextJson(jsonSerializerOptions);

            var restResponse = await client.ExecuteAsync<TResponse>(request);

            return restResponse.Data;
        }

        private RestClient GetClient() => new (_apiDomain);
    }
}