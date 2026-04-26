using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        [Authorize]
        [Function("GetReport")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "report")] HttpRequest req)
        {
            _logger.LogInformation("GetReport triggered.");

            // Authenticate
            var result = await req.HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                _logger.LogError($"Auth failed: {result.Failure?.Message}");
                return new UnauthorizedResult();
            }

            // Extract user ID from claims
            var userId = await AuthHelper.GetUserIdAsync(req);

            if (string.IsNullOrEmpty(userId))
                return new UnauthorizedResult();

            _logger.LogInformation($"Generating report for UserId = {userId}");

            // Fetch only this user's expenses
            //var expenses = await _dbService.GetExpensesByUserAsync(userId);

            var report = await _reportService.GenerateReportAsync(userId);

            // Generate PDF bytes from the report
            byte[] pdfBytes = _pdfService.GenerateReportPdf(report);

            // Return as FileContentResult
            return new FileContentResult(pdfBytes, "application/pdf")
            {
                FileDownloadName = $"ExpenseReport_{DateTime.UtcNow:yyyyMMdd}.pdf"
            };
            //var response = req.CreateResponse(HttpStatusCode.OK);
            //response.Headers.Add("Content-Type", "application/pdf");
            //await response.WriteBytesAsync(pdfBytes);

            //return response;
        }
    }
}
