using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

namespace ExpenseCategorizerFunction.Services
{
    public class OpenAiService : IOpenAiService
    {
        private readonly ChatClient _chatClient;
        private readonly IMlNetService _mlNetService;
        private readonly ILogger<OpenAiService> _logger;

        public OpenAiService(IMlNetService mlNetService, IConfiguration configuration, ILogger<OpenAiService> logger)
        {
            var apiKey = configuration["OPENAI_API_KEY"] ?? System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new ArgumentNullException(nameof(apiKey), "OpenAI API Key is missing. Check your local.settings.json or Azure App Settings.");
            }
            _chatClient = new ChatClient(model: "gpt-4o-mini", apiKey: apiKey);
            _mlNetService = mlNetService;
            _logger = logger;
        }
        public async Task<string> GenerateExplanationAsync(string text)
        {
            var category = _mlNetService.PredictCategory(text);

            try
            {
                List<ChatMessage> messages = new()
            {
                new SystemChatMessage("You explain expense categories."),
                new UserChatMessage($"The ML model predicted '{category}' for this expense: {text}. Write a short explanation why.")
            };

                // Call CompleteChatAsync directly from the chat client
                ChatCompletion completion = await _chatClient.CompleteChatAsync(messages);

                var result = completion.Content[0].Text;
                _logger.LogInformation($"OpenAI explanation generated: {result}");

                // Access content via the Content property
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ OpenAI call failed: {ex.Message}");
                // Fallback if credits are exhausted or API fails
                return $"This expense was categorized as {category} based on the description: {text}.";
            }
        }
    }


}