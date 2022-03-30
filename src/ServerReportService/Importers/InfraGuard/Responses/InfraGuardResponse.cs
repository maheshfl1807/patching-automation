namespace ServerReportService.Importers.InfraGuard.Responses
{
    public class InfraGuardResponse<TData>
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public TData Data { get; set; }
    }
}