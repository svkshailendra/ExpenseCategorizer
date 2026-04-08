using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;

namespace ExpenseCategorizerFunction
{
    public class GetReport
    {

        private readonly ILogger _logger;
        private readonly ReportService _reportService;
        private readonly PdfService _pdfService;

        public GetReport(ILoggerFactory loggerFactory, ReportService reportService, PdfService pdfService)
        {
            _logger = loggerFactory.CreateLogger<GetReport>();
            _reportService = reportService;
            _pdfService = pdfService;
        }
        [Function("GetReport")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "report")] HttpRequestData req)
        {
            _logger.LogInformation("GetReport triggered.");

            var report = await _reportService.GenerateReportAsync();

            // Generate PDF bytes from the report
            byte[] pdfBytes = _pdfService.GenerateReportPdf(report);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/pdf");
            await response.WriteBytesAsync(pdfBytes);

            return response;
        }
    }
}
