namespace ImportService.Producers
{
    using System.Threading.Tasks;

    public interface IProducer
    {
        public Task Produce();
    }
}