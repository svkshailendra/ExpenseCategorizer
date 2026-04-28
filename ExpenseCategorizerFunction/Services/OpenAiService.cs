using Azure;
using OpenAI;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

namespace ExpenseCategorizerFunction.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly IMlNetService _mlNetService;

        public OpenAiService(IMlNetService mlNetService)
        {
            _mlNetService = mlNetService;
        }
        public Task<string> GenerateExplanationAsync(string text)
        {
            // Very simple rule-based explanation
            string explanation;

            var category = _mlNetService.PredictCategory(text);
            explanation = $"This expense was categorized as {category} based on the description: {text}.";
            return Task.FromResult(explanation);

            //if (text.Contains("coffee", StringComparison.OrdinalIgnoreCase) ||
            //    text.Contains("pizza", StringComparison.OrdinalIgnoreCase) ||
            //    text.Contains("restaurant", StringComparison.OrdinalIgnoreCase))
            //{
            //    explanation = $"This expense looks like food or dining based on the text: {text}.";
            //}
            //else if (text.Contains("uber", StringComparison.OrdinalIgnoreCase) ||
            //         text.Contains("taxi", StringComparison.OrdinalIgnoreCase) ||
            //         text.Contains("flight", StringComparison.OrdinalIgnoreCase))
            //{
            //    explanation = $"This expense appears to be travel related, inferred from: {text}.";
            //}
            //else if (text.Contains("bill", StringComparison.OrdinalIgnoreCase) ||
            //         text.Contains("electricity", StringComparison.OrdinalIgnoreCase) ||
            //         text.Contains("internet", StringComparison.OrdinalIgnoreCase))
            //{
            //    explanation = $"This expense is likely a utility payment, based on: {text}.";
            //}
            //else
            //{
            //    explanation = $"This expense could not be clearly categorized, but the text was: {text}.";
            //}

            //return Task.FromResult(explanation);
        }
    }


}