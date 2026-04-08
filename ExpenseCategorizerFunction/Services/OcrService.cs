using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Text;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

namespace ExpenseCategorizerFunction.Services
{
    public class OcrService : IOcrService
    {
        private readonly ComputerVisionClient _client;

        public OcrService(string endpoint, string key)
        {
            _client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
            {
                Endpoint = endpoint
            };
        }

        public async Task<string> ExtractTextAsync(Stream fileStream)
        {
            var result = await _client.ReadInStreamAsync(fileStream);
            string operationId = result.OperationLocation.Split('/').Last();

            ReadOperationResult readResult;
            do
            {
                await Task.Delay(1000);
                readResult = await _client.GetReadResultAsync(Guid.Parse(operationId));
            } while (readResult.Status == OperationStatusCodes.Running);

            var text = new StringBuilder();
            foreach (var page in readResult.AnalyzeResult.ReadResults)
            {
                foreach (var line in page.Lines)
                {
                    text.AppendLine(line.Text);
                }
            }
            return text.ToString();
        }
    }

}