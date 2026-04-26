using ExpenseCategorizer.Shared;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace ExpenseCategorizerFunction
{
    public class DatabaseService
    {
        private readonly List<Expense> _expenses = new();
        private readonly Container _container;

        public DatabaseService()
        {
            var connectionString = Environment.GetEnvironmentVariable("COSMOS_CONNECTION");
            var databaseName = Environment.GetEnvironmentVariable("COSMOS_DATABASE");
            var containerName = Environment.GetEnvironmentVariable("COSMOS_CONTAINER");

            var client = new CosmosClient(connectionString);
            _container = client.GetContainer(databaseName, containerName);
        }
        public async Task SaveExpenseAsync(Expense expense, string userId)
        {
            try
            {
                //expense.Category = CleanCategory(expense.Category);
                // Ensure Category is not null if your container partition key is /category
                expense.UserId = userId;
                //await _container.UpsertItemAsync(expense, new PartitionKey(expense.Category));
                await _container.UpsertItemAsync(expense, new PartitionKey(expense.UserId));
            }
            catch (CosmosException ex)
            {
                // Log Cosmos errors for visibility
                Console.WriteLine($"Cosmos insert failed: {ex.StatusCode} - {ex.Message}");
                throw;
            }
        }

        public async Task<List<Expense>> GetAllExpensesAsync()
        {
            var query = _container.GetItemQueryIterator<Expense>("SELECT * FROM c");
            var results = new List<Expense>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<List<Expense>> GetAllExpensesByUserAsync(string userId)
        {
            var query = _container.GetItemQueryIterator<Expense>(
                new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                    .WithParameter("@userId", userId));

            var results = new List<Expense>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
        public async Task<List<Expense>> GetExpensesByUserAsync(string userId)
        {
            var query = _container.GetItemLinqQueryable<Expense>(true)
                                  .Where(e => e.UserId == userId)
                                  .ToFeedIterator();

            var results = new List<Expense>();
            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }
            return results;
        }

        public async Task UpdateExpenseAsync(Expense expense, string userId)
        {
            expense.UserId = userId;
            await _container.UpsertItemAsync(expense, new PartitionKey(expense.UserId));
        }

        public async Task DeleteExpenseAsync(string id,string userId)
        {
            await _container.DeleteItemAsync<Expense>(id, new PartitionKey(userId));
        }

        public async Task<List<Expense>> GetMonthlyExpensesAsync(int month)
        {
            var result = _expenses.FindAll(e => e.Date.Month == month);
            return await Task.FromResult(result);
        }
    }

}