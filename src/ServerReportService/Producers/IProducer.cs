namespace ServerReportService.Producers
{
    using System.Threading.Tasks;

    public interface IProducer
    {
        public Task Produce();
    }
}