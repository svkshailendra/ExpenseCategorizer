using ExpenseCategorizer.Shared;
using Microsoft.Azure.Cosmos;

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
        public async Task SaveExpenseAsync(Expense expense)
        {
            try
            {
                //expense.Category = CleanCategory(expense.Category);
                // Ensure Category is not null if your container partition key is /category                
                await _container.UpsertItemAsync(expense, new PartitionKey(expense.Category));
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

        public async Task UpdateExpenseAsync(Expense expense)
        {
            await _container.UpsertItemAsync(expense, new PartitionKey(expense.Category));
        }

        public async Task DeleteExpenseAsync(string id,string category)
        {
            await _container.DeleteItemAsync<Expense>(id, new PartitionKey(category));
        }

        public async Task<List<Expense>> GetMonthlyExpensesAsync(int month)
        {
            var result = _expenses.FindAll(e => e.Date.Month == month);
            return await Task.FromResult(result);
        }
    }

}