namespace ServerReportService.Consumers.Commands
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Transfer;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;
    using Confluent.Kafka;
    using Csv;
    using CsvHelper;
    using CsvHelper.Configuration;
    using Dasync.Collections;
    using Exporters;
    using Importers;
    using ServerReportService.Entities;
    using LaunchSharp;
    using LaunchSharp.Settings;
    using Messages;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Settings;
    using SmartFormat;

    public class ServerReportCommandConsumer : AbstractConsumer<Ignore, string>
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<IExporter> _exporters;
        private readonly IEnumerable<IImporter> _importers;
        private readonly IFactory<ITransferUtility> _s3Factory;
        private readonly string _s3AccessPoint;
        private readonly string _s3ReportKey;
        private readonly string _snsReportTopicArn;
        private readonly string _snsReportMessageTemplate;
        private readonly string _snsReportSubject;

        public ServerReportCommandConsumer(
            ISettings<RootSettings> rootSettings,
            ISettings<KafkaSettings> kafkaSettings,
            ILogger logger,
            IEnumerable<IExporter> exporters,
            IEnumerable<IImporter> importers,
            IFactory<ITransferUtility> s3Factory)
            : base(kafkaSettings)
        {
            _logger = logger;
            _exporters = exporters;
            _importers = importers;
            _s3Factory = s3Factory;
            var groupId = kafkaSettings.GetRequired(s => s.ServerReportCommandConsumerGroupId);
            SetConfigGroupId(groupId);

            _s3AccessPoint = rootSettings.GetRequired(s => s.S3AccessPoint);
            _s3ReportKey = rootSettings.GetRequired(s => s.S3ReportKey);
            _snsReportTopicArn = rootSettings.GetRequired(s => s.SnsReportTopicArn);
            _snsReportSubject = rootSettings.GetRequired(s => s.SnsReportSubject);
            _snsReportMessageTemplate = rootSettings.GetRequired(s => s.SnsReportMessageTemplate);
        }

        /// <inheritdoc />
        public override async Task Consume(CancellationToken cancellationToken)
        {
            using var consumer = GetConsumer();
            consumer.Subscribe(Topics.PublicServerReportServiceCommandsServerReportV1);

            // while (!cancellationToken.IsCancellationRequested)
            // {
                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    var importCommandMessage = JsonConvert.DeserializeObject<ServerReportCommandMessage>(consumeResult.Message.Value);
                    var accountsWithCredentialIssues = new ConcurrentDictionary<string, bool>();
                    var serverReports = new ConcurrentBag<ServerReport>();

                    _logger.LogInformation("Server Report Command found. Consuming now...");

                    if (importCommandMessage != null)
                    {
                        await _exporters.ParallelForEachAsync(
                            async exporter =>
                            {
                                var exporterProvider = await exporter.GetCloudProvider();
                                if (importCommandMessage.ProviderNames == null ||
                                    importCommandMessage.ProviderNames.Contains(exporterProvider.Name))
                                {
                                    var cloudAccounts =
                                        await exporter.GetCloudAccounts(importCommandMessage.AccountIds);

                                    await cloudAccounts.ParallelForEachAsync(
                                        async cloudAccount =>
                                        {
                                            _logger.LogInformation(
                                                "Running importers for {exporterProvider.Name} account {cloudAccount.CloudProviderAccountId}",
                                                exporterProvider.Name,
                                                cloudAccount.CloudProviderAccountId);

                                            var cloudServers = await exporter.GetCloudServers(cloudAccount,
                                                importCommandMessage.ServerIds, accountsWithCredentialIssues);

                                            if (!cloudServers.IsEmpty())
                                            {
                                                await _importers.ParallelForEachAsync(
                                                    async importer => await importer.Import(
                                                        cloudServers,
                                                        cloudAccount,
                                                        exporterProvider,
                                                        serverReports,
                                                        accountsWithCredentialIssues),
                                                    cancellationToken);
                                            }
                                        },
                                        cancellationToken);
                                }
                            },
                            cancellationToken);
                    }

                    var sortedServerReports = serverReports.OrderBy(serverReport => serverReport.AccountId);

                    // Write out compressed CSV of server reports to memory stream.
                    await using var s3Stream = new MemoryStream();
                    await WriteCompressedCsvToMemoryStreamAsync<ServerReport, ServerReportMap>(s3Stream, sortedServerReports);

                    // Upload stream to S3.
                    var reportKey = string.Format(_s3ReportKey, $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss}");
                    await WriteMemoryStreamToS3(s3Stream, reportKey);

                    // Send downloadable report URL to SNS topic.
                    var urlToReport = GetS3PreSignedUrl(reportKey);
                    await SendReportToSns(serverReports, accountsWithCredentialIssues, urlToReport);

                    _logger.LogInformation("Completed Consumption of Server Report Command");
                }
                catch (Exception e)
                {
                    _logger.LogError("{Message}\n{Stack}", e.Message, e.StackTrace);
                }
            // }

            consumer.Close();
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

        private async Task SendReportToSns(
            ConcurrentBag<ServerReport> serverReports,
            ConcurrentDictionary<string, bool> accountsWithCredentialIssues,
            string reportUrl)
        {
            var client = new AmazonSimpleNotificationServiceClient(RegionEndpoint.USWest2);

            var request = new PublishRequest
            {
                Subject = _snsReportSubject,
                Message = Smart.Format(_snsReportMessageTemplate, new
                {
                    serverReports,
                    accountsWithCredentialIssues = accountsWithCredentialIssues.Keys.OrderBy(accountId => accountId).ToList(),
                    reportUrl,
                }),
                TopicArn = _snsReportTopicArn,
            };

            try
            {
                await client.PublishAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError("Caught exception publishing to SNS topic: {Message}", ex.Message);
            }
        }
    }
}