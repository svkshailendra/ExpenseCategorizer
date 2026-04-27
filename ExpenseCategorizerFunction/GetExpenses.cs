using ExpenseCategorizer.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ExpenseCategorizerFunction
{
    public class GetExpenses
    {
        private readonly ILogger _logger;
        private readonly DatabaseService _dbService;
        
        // This is a normal constructor — no 'this' initializer needed
        public GetExpenses(ILoggerFactory loggerFactory, DatabaseService dbService)
        {
            _logger = loggerFactory.CreateLogger<GetExpenses>();
            _dbService = dbService;
        }

        [Authorize] // Keeping this so we know it's a protected endpoint.Authorization is handled by extension method 
        [Function("GetExpenses")]        
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "expenses")] HttpRequest req)
        {
            // This manually triggers the logic in Program.cs and captures the error
            //var result = await req.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

            // if (!result.Succeeded)
            // {
            //     // THIS IS THE KEY: It will log EXACTLY why the token was rejected
            //     _logger.LogError($"Detailed Failure: {result.Failure?.Message}");
            //     return new UnauthorizedResult();
            // }

            // _logger.LogInformation("✅ Success! Token is valid.");
            var result = await req.HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                _logger.LogError($"Auth failed: {result.Failure?.Message}");
                return new UnauthorizedResult();
            }

            //// 🔎 Dump all claims from the authenticated principal
            //foreach (var claim in result.Principal.Claims)
            //{
            //    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
            //}
            //if (!await req.IsAuthorizedAsync(_logger)) return new UnauthorizedResult();

            _logger.LogInformation("Success! Token is valid. Fetching expenses...");
            _logger.LogInformation("GetExpenses triggered.");


            //var userId = req.HttpContext.User.FindFirst("oid")?.Value ?? req.HttpContext.User.FindFirst("sub")?.Value ?? req.HttpContext.User.FindFirst("preferred_username")?.Value;

            // Extract userId from the principal, not HttpContext.User
            //var userId = result.Principal.FindFirst("oid")?.Value
            //          ?? result.Principal.FindFirst("sub")?.Value
            //          ?? result.Principal.FindFirst("preferred_username")?.Value;
            var userId = await AuthHelper.GetUserIdAsync(req);
            _logger.LogInformation($"Extracted UserId = {userId ?? "NULL"}");
            if (string.IsNullOrEmpty(userId))
                return new UnauthorizedResult();

            var expenses = await _dbService.GetExpensesByUserAsync(userId);
            return new OkObjectResult(expenses);
            ////var expenses = await _dbService.GetAllExpensesAsync();


            ////return new OkObjectResult(expenses);
            //var response = req.CreateResponse(HttpStatusCode.OK);
            //await response.WriteStringAsync(JsonSerializer.Serialize(expenses));
            //return response;
        }

        [Function("UpdateExpense")]
        public async Task<IActionResult> UpdateExpense(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "expenses/{id}")] HttpRequest req,string id)
        {
            //var options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //};

            // Authenticate
            var result = await req.HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
                return new UnauthorizedResult();

            var userId = await AuthHelper.GetUserIdAsync(req);

            if (string.IsNullOrEmpty(userId))
                return new UnauthorizedResult();

            var expense = await JsonSerializer.DeserializeAsync<Expense>(req.Body, JsonHelper.SafeOptions);
            expense.Id = id; // ensure ID consistency 
            expense.UserId = userId; // enforce partition key
            await _dbService.UpdateExpenseAsync(expense, userId);

            return new OkObjectResult("Expense updated");
            //var response = req.CreateResponse(HttpStatusCode.OK);
            //await response.WriteStringAsync("Expense updated");
            //return response;
        }


        [Function("DeleteExpense")]
        public async Task<IActionResult> DeleteExpense(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "expenses/{id}")] HttpRequest req,string id)
        {
            var userId = await AuthHelper.GetUserIdAsync(req);
            if (string.IsNullOrEmpty(userId))
                return new UnauthorizedResult();
            try
            {
                await _dbService.DeleteExpenseAsync(id, userId);
                return new OkObjectResult("Expense deleted");
                //var response = req.CreateResponse(HttpStatusCode.OK);
                //await response.WriteStringAsync("Expense deleted");
                //return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                //var response = req.CreateResponse(HttpStatusCode.NotFound);
                //await response.WriteStringAsync("Expense not found");
                //return response;
                return new NotFoundObjectResult("Expense not found");
            }
        }

    }

}
