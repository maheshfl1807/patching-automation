namespace ImportService.Importers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Transfer;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;
    using Amazon.SimpleSystemsManagement;
    using Amazon.SimpleSystemsManagement.Model;
    using CsvHelper;
    using CsvHelper.Configuration;
    using ImportService.Csv;
    using ImportService.Data;
    using ImportService.Entities;
    using ImportService.Settings;
    using LaunchSharp;
    using LaunchSharp.Settings;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public class ImportServiceImporter : IImporter
    {
        private const string c_profileIssue = "Server is missing profile.";
        private const string c_missingTagIssue = "Server is missing patching tag.";
        private const string c_ssmConnectionIssue = "Cannot connect to server with SSM.";
        private readonly IEnumerable<string> _validTagKeys;
        private readonly string _s3AccessPoint;
        private readonly string _s3ReportKey;
        private readonly IDbContextFactory<ImportServiceContext> _contextFactory;
        private readonly ILogger _logger;
        private readonly IFactory<ITransferUtility> _s3Factory;
        private readonly string _snsReportTopicArn;
        private readonly string _snsReportMessageTemplate;
        private readonly string _snsReportSubject;

        public ImportServiceImporter(
            ISettings<RootSettings> rootSettings,
            ILogger logger,
            IDbContextFactory<ImportServiceContext> contextFactory,
            IFactory<ITransferUtility> s3Factory)
        {
            _validTagKeys = rootSettings.GetRequired(s => s.ValidPatchTagKeys);
            _s3AccessPoint = rootSettings.GetRequired(s => s.S3AccessPoint);
            _s3ReportKey = rootSettings.GetRequired(s => s.S3ReportKey);
            _snsReportTopicArn = rootSettings.GetRequired(s => s.SnsReportTopicArn);
            _snsReportSubject = rootSettings.GetRequired(s => s.SnsReportSubject);
            _snsReportMessageTemplate = rootSettings.GetRequired(s => s.SnsReportMessageTemplate);
            _logger = logger;
            _contextFactory = contextFactory;
            _s3Factory = s3Factory;
        }

        /// <inheritdoc />
        public async Task Import(IEnumerable<CloudServer> cloudServers, CloudAccount cloudAccount, CloudProvider cloudProvider)
        {
            var cloudServersByServerId = new ConcurrentDictionary<string, CloudServer>();
            var serverIssues = new ConcurrentBag<ServerIssueReport>();

            // Build dictionary of cloud servers keyed by ServerId. Also populate
            // list of any server configuration issues.
            var serverIssuesTasks = new ConcurrentBag<Task>();
            cloudServers.AsParallel()
                .ForEach(cloudServer =>
                {
                    cloudServersByServerId.TryAdd(cloudServer.ServerId, cloudServer);

                    serverIssuesTasks.Add(UpdateServerIssues(serverIssues, cloudServer, cloudAccount, cloudProvider));
                });
            await Task.WhenAll(serverIssuesTasks);

            // Write out compressed CSV to memory stream.
            await using var s3Stream = new MemoryStream();
            await WriteCompressedCsvToMemoryStreamAsync<ServerIssueReport, ServerIssueReportMap>(s3Stream, serverIssues);

            // Upload stream to S3.
            var reportKey = string.Format(_s3ReportKey, $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}");
            await WriteMemoryStreamToS3(s3Stream, reportKey);

            // Send downloadable report URL to SNS topic.
            var urlToReport = GetS3PreSignedUrl(reportKey);
            await SendReportToSns(serverIssues.Count, urlToReport);

            // Track any changes to servers and publish messages to
            await TrackAndPublishServerChanges(cloudServersByServerId, cloudAccount);
        }

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
            CloudServer cloudServer,
            CloudAccount cloudAccount,
            CloudProvider cloudProvider)
        {
            if (cloudServer.ProfileId == null)
            {
                serverIssues.Add(new ServerIssueReport
                {
                    AccountId = cloudAccount.CloudProviderAccountId,
                    ServerId = cloudServer.ServerId,
                    Issue = c_profileIssue,
                    ProviderName = cloudProvider.Name,
                });
            }
            else if (cloudProvider.Name == "AWS")
            {
                var awsSsmClient = new AmazonSimpleSystemsManagementClient();
                var getConnectionStatusRequest = new GetConnectionStatusRequest
                {
                    Target = cloudServer.ServerId,
                };
                var getConnectionStatusResponse = await awsSsmClient.GetConnectionStatusAsync(getConnectionStatusRequest);

                if (getConnectionStatusResponse.Status == ConnectionStatus.NotConnected)
                {
                    serverIssues.Add(new ServerIssueReport
                    {
                        AccountId = cloudAccount.CloudProviderAccountId,
                        ServerId = cloudServer.ServerId,
                        Issue = c_ssmConnectionIssue,
                        ProviderName = cloudProvider.Name,
                    });
                }
            }

            var validTagKeyFound = false;
            cloudServer.CloudServerTags.AsParallel().ForEach(cloudServerTag =>
            {
                var isValidTagKey = _validTagKeys.Any(validTagKey => string.Equals(
                    validTagKey, cloudServerTag.Key, StringComparison.OrdinalIgnoreCase));
                if (isValidTagKey)
                {
                    validTagKeyFound = true;
                }
            });

            if (!validTagKeyFound)
            {
                serverIssues.Add(new ServerIssueReport
                {
                    AccountId = cloudAccount.CloudProviderAccountId,
                    ServerId = cloudServer.ServerId,
                    Issue = c_missingTagIssue,
                    ProviderName = cloudProvider.Name,
                });
            }
        }

        private async Task WriteCompressedCsvToMemoryStreamAsync<TCsvRow, TCsvRowMap>(
            MemoryStream stream,
            IEnumerable<TCsvRow> rows)
            where TCsvRowMap : ClassMap<TCsvRow>
        {
            await using var csvStream = new MemoryStream();
            await using var writer = new StreamWriter(csvStream);
            await using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csvWriter.Context.RegisterClassMap<TCsvRowMap>();

            await csvWriter.WriteRecordsAsync(rows);
            await csvWriter.FlushAsync();

            csvStream.Position = 0;
            await using var compressed = new GZipStream(stream, CompressionMode.Compress, true);
            await csvStream.CopyToAsync(compressed);
            await compressed.FlushAsync();
        }

        private async Task WriteMemoryStreamToS3(MemoryStream stream, string reportKey)
        {
            if (stream.Length != 0)
            {
                using var transferUtility = _s3Factory.Create();
                var uploadRequest = new TransferUtilityUploadRequest
                {
                    BucketName = _s3AccessPoint,
                    Key = reportKey,
                    InputStream = stream,
                    AutoCloseStream = false,
                };
                await transferUtility.UploadAsync(uploadRequest);
            }
        }

        private string GetS3PreSignedUrl(string filekey)
        {
            var url = string.Empty;

            if (!string.IsNullOrEmpty(filekey) && !string.IsNullOrWhiteSpace(filekey))
            {
                using var client = new AmazonS3Client(RegionEndpoint.USWest2);
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _s3AccessPoint,
                    Key = filekey.TrimEnd('/'),
                    Verb = HttpVerb.GET,
                    Expires = DateTime.UtcNow.AddHours(24),
                };
                url = client.GetPreSignedURL(request);
            }

            return url;
        }

        private async Task SendReportToSns(int issueCount, string reportUrl)
        {
            var client = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USWest2);

            var request = new PublishRequest
            {
                Subject = _snsReportSubject,
                Message = string.Format(_snsReportMessageTemplate, issueCount, reportUrl),
                TopicArn = _snsReportTopicArn,
            };

            try
            {
                var response = await client.PublishAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("Caught exception publishing to SNS topic: {Message}", ex.Message);
            }
        }
    }
}