namespace ImportService.Importers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using ImportService.Entities;

    /// <summary>
    /// Import cloud servers into InfraGuard as clusters.
    /// </summary>
    public class InfraGuardImporter : IImporter
    {
        private static readonly string[] s_validTagKeys = { "Patch Group", "2W_Patch", "2W_Patched" };
        private static readonly string[] s_invalidTagValues = { "Exclude", "Exempt", "Not patch", "None", "False" };

        /// <inheritdoc />
        public void Import(IEnumerable<CloudServer> cloudServers)
        {
            var orphanedServers = new List<CloudServer>();
            var missingTagServers = new List<CloudServer>();
            var excludedServers = new List<CloudServer>();
            var profileGroupedServers = GetProfileGroupedServers(
                cloudServers, orphanedServers, missingTagServers, excludedServers);

            foreach (var profileGroup in profileGroupedServers)
            {
                // InfraGuard API Docs: https://patching-api.2ndwatch.com/api-docs/#/

                // Build request for InfraGuard /cluster route POST method

                // POST /authenticate to get auth token.

                // (IF NECESSARY) GET /cluster and iterate to see if cluster already exists,
                // skip following POST if does.

                // POST /cluster to create cluster.

                // GET /project to get projects and determine which is Default and which is Exclude
            }

            // PUT /project/assignservers/{exclude project id} using list excludedServers
            // (there may be some delay between creating cluster and servers existing, keep in mind).

            // Send orphanedServers and missingTagServers report, destination TBD.

            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Groups IEnumberable of CloudServer into Dictionary keyed by ProfileId.
        /// </summary>
        /// <param name="cloudServers"></param>
        /// <param name="orphanedServers"></param>
        /// <returns></returns>
        private static Dictionary<string, List<CloudServer>> GetProfileGroupedServers(
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
        /// <param name="cloudServer"></param>
        /// <param name="missingTagServers"></param>
        /// <param name="excludedServers"></param>
        private static void CheckServerTags(CloudServer cloudServer, List<CloudServer> missingTagServers, List<CloudServer> excludedServers)
        {
            var validTagKeyFound = false;
            var validTagValueFound = false;

            foreach (var cloudServerTag in cloudServer.CloudServerTags)
            {
                var isValidTagKey = s_validTagKeys.Any(validTagKey => string.Equals(
                    validTagKey, cloudServerTag.Key, StringComparison.OrdinalIgnoreCase));
                if (isValidTagKey)
                {
                    validTagKeyFound = true;

                    var isInvalidTagValue = s_invalidTagValues.Any(invalidTagValue => string.Equals(
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