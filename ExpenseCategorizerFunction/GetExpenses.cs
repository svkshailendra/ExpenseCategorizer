using ExpenseCategorizer.Shared;
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

        [Function("GetExpenses")]
        public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "expenses")] HttpRequestData req)
        {
            _logger.LogInformation("GetExpenses triggered.");

            var expenses = await _dbService.GetAllExpensesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(JsonSerializer.Serialize(expenses));
            return response;
        }

        [Function("UpdateExpense")]
        public async Task<HttpResponseData> UpdateExpense(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "expenses/{id}/{category}")] HttpRequestData req,string id,string category)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var expense = await JsonSerializer.DeserializeAsync<Expense>(req.Body, options);
            expense.Id = id; // ensure ID consistency
            expense.Category = category;
            //expense.Category = CleanCategory(expense.Category);
            await _dbService.UpdateExpenseAsync(expense);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync("Expense updated");
            return response;
        }


        [Function("DeleteExpense")]
        public async Task<HttpResponseData> DeleteExpense(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "expenses/{id}/{category}")] HttpRequestData req,string id,string category)
        {
            try
            {
                await _dbService.DeleteExpenseAsync(id, category);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteStringAsync("Expense deleted");
                return response;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                var response = req.CreateResponse(HttpStatusCode.NotFound);
                await response.WriteStringAsync("Expense not found");
                return response;
            }
        }

    }

}
