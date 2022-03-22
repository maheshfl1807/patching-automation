namespace IssueReportService.Consumers
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IConsumer
    {
        /// <summary>
        /// Begins consumption loop.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public Task Consume(CancellationToken cancellationToken);
    }
}