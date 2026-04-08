namespace ExpenseCategorizerFunction.Services
{
    public interface IExpenseServices
    {
        public interface IOcrService
        {
            Task<string> ExtractTextAsync(Stream fileStream);
        }

        public interface IMlNetService
        {
            string PredictCategory(string text);
        }

        public interface IOpenAiService
        {
            Task<string> GenerateExplanationAsync(string text);
        }

    }
}