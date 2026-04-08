using ExpenseCategorizer.Shared;
using HttpMultipartParser;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

namespace ExpenseCategorizerFunction;

public class UploadExpense
{
    private readonly ILogger _logger;
    private readonly IOcrService _ocrService;
    private readonly IMlNetService _mlNetService;
    private readonly IOpenAiService _openAiService;
    private readonly DatabaseService _dbService;

    public UploadExpense(ILoggerFactory loggerFactory, IOcrService ocrService, IMlNetService mlNetService,
                         IOpenAiService openAiService, DatabaseService dbService)
    {
        _logger = loggerFactory.CreateLogger<UploadExpense>();
        _ocrService = ocrService;
        _mlNetService = mlNetService;
        _openAiService = openAiService;
        _dbService = dbService;
    }

    [Function("UploadExpense")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload")] HttpRequestData req)
    {
        _logger.LogInformation("UploadExpense triggered.");

        var parser = await MultipartFormDataParser.ParseAsync(req.Body);
        var file = parser.Files.FirstOrDefault();

        if (file == null)
        {
            var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badResponse.WriteStringAsync("No file uploaded");
            return badResponse;
        }

        if (file.ContentType == "application/json")
        {
            using var reader = new StreamReader(file.Data);
            var json = await reader.ReadToEndAsync();
            var expenses = System.Text.Json.JsonSerializer.Deserialize<List<Expense>>(json) ?? new List<Expense>();            

            foreach (var expense in expenses)
            {
                if (string.IsNullOrEmpty(expense.Id))
                    expense.Id = Guid.NewGuid().ToString();
                await _dbService.SaveExpenseAsync(expense);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(expenses));
            return response;
        }
        else
        {
            // Otherwise, use OCR + ML + OpenAI pipeline
            string extractedText = await _ocrService.ExtractTextAsync(file.Data);

            // Split OCR output into lines (handles \n or \r\n)
            var lines = extractedText
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            var expensesList = new List<Expense>();

            foreach (var line in lines)
            {
                // Skip obvious metadata
                var skipKeywords = new[] { "Invoice", "Date:", "Invoice No:", "Items:", "Total:" };
                if (skipKeywords.Any(k => line.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
                    continue;

                // Extract amount (default 0 if not found)
                var amountMatch = Regex.Match(line, @"(\p{Sc}?\s*\d+(\.\d{1,2})?)$");
                int amount = amountMatch.Success ? int.Parse(Regex.Replace(amountMatch.Value, @"[^\d]", "")) : 0;

                // Extract date (fallback to current)
                var dateMatch = Regex.Match(line, @"(\d{2}-[A-Za-z]{3}-\d{4}|\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2})");
                DateTime expenseDate = dateMatch.Success
                    ? DateTime.Parse(dateMatch.Value, CultureInfo.InvariantCulture)
                    : DateTime.UtcNow;

                // Clean description
                string description = line;
                if (amountMatch.Success) description = description.Replace(amountMatch.Value, "").Trim();
                if (dateMatch.Success) description = description.Replace(dateMatch.Value, "").Trim();

                // Predict + explain
                string category = _mlNetService.PredictCategory(description);
                string explanation = await _openAiService.GenerateExplanationAsync(description);

                var expense = new Expense
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = description,
                    Category = category,
                    Explanation = explanation,
                    Date = expenseDate,
                    Amount = amount
                };

                await _dbService.SaveExpenseAsync(expense);
                expensesList.Add(expense);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(expensesList));
            return response;
        }
        
    }
}
