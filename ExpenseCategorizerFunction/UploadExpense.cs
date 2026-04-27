using ExpenseCategorizer.Shared;
using HttpMultipartParser;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Security.Claims;
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

    [Authorize]
    [Function("UploadExpense")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "upload")] HttpRequest req)
    {
        _logger.LogInformation("UploadExpense triggered.");

        // Extract user ID from token
        //var userId = req.HttpContext.User.FindFirst("oid")?.Value ?? req.HttpContext.User.FindFirst("sub")?.Value ?? req.HttpContext.User.FindFirst("preferred_username")?.Value;

        // Trigger authentication manually
        var result = await req.HttpContext.AuthenticateAsync();
        if (!result.Succeeded)
        {
            _logger.LogError($"Auth failed: {result.Failure?.Message}");
            return new UnauthorizedResult();
        }

        foreach (var claim in result.Principal.Claims)
        {
            _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
        }
        // Extract user ID from claims
        var userId = await AuthHelper.GetUserIdAsync(req);
        _logger.LogInformation($"Extracted UserId = {userId ?? "NULL"}");
        //if (string.IsNullOrEmpty(userId))
        //{
        //    var unauthorized = req.CreateResponse(HttpStatusCode.Unauthorized);
        //    await unauthorized.WriteStringAsync("User not authenticated");
        //    return unauthorized;
        //}
        if (string.IsNullOrEmpty(userId))
            return new UnauthorizedResult();

        var parser = await MultipartFormDataParser.ParseAsync(req.Body);
        var file = parser.Files.FirstOrDefault();

        //if (file == null)
        //{
        //    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        //    await badResponse.WriteStringAsync("No file uploaded");
        //    return badResponse;
        //}
        if (file == null)
            return new BadRequestObjectResult("No file uploaded");

        if (file.ContentType == "application/json")
        {
            using var reader = new StreamReader(file.Data);
            var json = await reader.ReadToEndAsync();
            var expenses = System.Text.Json.JsonSerializer.Deserialize<List<Expense>>(json) ?? new List<Expense>();

            foreach (var expense in expenses)
            {
                if (string.IsNullOrEmpty(expense.Id))
                    expense.Id = Guid.NewGuid().ToString();
                // Attach user ID
                expense.UserId = userId;
                await _dbService.SaveExpenseAsync(expense, userId);
            }

            //var response = req.CreateResponse(HttpStatusCode.OK);
            //await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(expenses));
            //return response;
            return new OkObjectResult(expenses);
        }
        else
        {
            //// Otherwise, use OCR + ML + OpenAI pipeline
            //string extractedText = await _ocrService.ExtractTextAsync(file.Data);

            //// Split OCR output into lines (handles \n or \r\n)
            //var lines = extractedText
            //    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            //    .Select(l => l.Trim())
            //    .Where(l => !string.IsNullOrWhiteSpace(l))
            //    .ToList();

            //var expensesList = new List<Expense>();

            //foreach (var line in lines)
            //{
            //    // Skip obvious metadata
            //    var skipKeywords = new[] { "Invoice", "Date:", "Invoice No:", "Items:", "Total:" };
            //    if (skipKeywords.Any(k => line.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
            //        continue;

            //    // Extract amount (default 0 if not found)
            //    var amountMatch = Regex.Match(line, @"(\p{Sc}?\s*\d+(\.\d{1,2})?)$");
            //    int amount = amountMatch.Success ? int.Parse(Regex.Replace(amountMatch.Value, @"[^\d]", "")) : 0;

            //    // Extract date (fallback to current)
            //    var dateMatch = Regex.Match(line, @"(\d{2}-[A-Za-z]{3}-\d{4}|\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2})");
            //    DateTime expenseDate = dateMatch.Success
            //        ? DateTime.Parse(dateMatch.Value, CultureInfo.InvariantCulture)
            //        : DateTime.UtcNow;

            //    // Clean description
            //    string description = line;
            //    if (amountMatch.Success) description = description.Replace(amountMatch.Value, "").Trim();
            //    if (dateMatch.Success) description = description.Replace(dateMatch.Value, "").Trim();

            //    // Predict + explain
            //    string category = _mlNetService.PredictCategory(description);
            //    //category = CleanCategory(category);

            //    string explanation = await _openAiService.GenerateExplanationAsync(description);

            //    var expense = new Expense
            //    {
            //        Id = Guid.NewGuid().ToString(),
            //        Description = description,
            //        Category = category,
            //        Explanation = explanation,
            //        Date = expenseDate,
            //        Amount = amount,
            //        UserId = userId
            //    };

            //    await _dbService.SaveExpenseAsync(expense, userId);
            //    expensesList.Add(expense);
            //}

            ////var response = req.CreateResponse(HttpStatusCode.OK);
            ////response.Headers.Add("Content-Type", "application/json");
            ////await response.WriteStringAsync(JsonSerializer.Serialize(expensesList));
            ////return response;

            //return new OkObjectResult(expensesList);
            string text;

            if (file.ContentType == "text/plain")
            {
                using var reader = new StreamReader(file.Data);
                text = await reader.ReadToEndAsync();
            }
            else
            {
                // OCR for images/PDFs
                text = await _ocrService.ExtractTextAsync(file.Data);
            }

            var expensesList = await ParseTextAsync(text, userId);
            return new OkObjectResult(expensesList);
        }

    }


    // Shared parser for TXT and OCR
    private async Task<List<Expense>> ParseTextAsync(string text, string userId)
    {
        var lines = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        var expensesList = new List<Expense>();

        foreach (var line in lines)
        {
            var skipKeywords = new[] { "Invoice", "Date:", "Invoice No:", "Items:", "Total:" , "Subtotal", "Taxes", "Total", "Invoice Total" };
            if (skipKeywords.Any(k => line.StartsWith(k, StringComparison.OrdinalIgnoreCase)))
                continue;

            //var amountMatch = Regex.Match(line, @"(\p{Sc}?\s*\d+(\.\d{1,2})?)$");
            //int amount = amountMatch.Success ? int.Parse(Regex.Replace(amountMatch.Value, @"[^\d]", "")) : 0;

            // Extract amount safely as decimal
            //var amountMatch = Regex.Match(line, @"(\p{Sc}?\s*\d+(\.\d{1,2})?)$");
            //decimal amount = 0;
            //if (amountMatch.Success)
            //{
            //    var cleaned = Regex.Replace(amountMatch.Value, @"[^\d.]", "");
            //    decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
            //}

            // Use helper to extract amount
            decimal amount = ExtractAmount(line);

            var dateMatch = Regex.Match(line, @"(\d{2}-[A-Za-z]{3}-\d{4}|\d{2}/\d{2}/\d{4}|\d{4}-\d{2}-\d{2})");
            DateTime expenseDate = dateMatch.Success
                ? DateTime.Parse(dateMatch.Value, CultureInfo.InvariantCulture)
                : DateTime.UtcNow;

            //clean Description
            string description = line;
            //if (amountMatch.Success) description = description.Replace(amountMatch.Value, "").Trim();
            if (dateMatch.Success) description = description.Replace(dateMatch.Value, "").Trim();

            // Skip if description is empty after cleaning
            if (string.IsNullOrWhiteSpace(description))
                continue;

            //Predict + Explain
            string category = _mlNetService.PredictCategory(description);
            string explanation = await _openAiService.GenerateExplanationAsync(description);

            var expense = new Expense
            {
                Id = Guid.NewGuid().ToString(),
                Description = description,
                Category = category,
                Explanation = explanation,
                Date = expenseDate,
                Amount = amount,
                UserId = userId
            };

            await _dbService.SaveExpenseAsync(expense, userId);
            expensesList.Add(expense);
        }

        return expensesList;
    }

    // Helper: extract last numeric value safely
    private decimal ExtractAmount(string line)
    {
        var matches = Regex.Matches(line, @"\d+(\.\d{1,2})?");
        if (matches.Count == 0) return 0;

        var lastMatch = matches[matches.Count - 1].Value;
        if (decimal.TryParse(lastMatch, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
        {
            if (amount > 0 && amount < 1000000) // guard against IDs
                return amount;
        }
        return 0;
    }
}
