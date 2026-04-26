using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExpenseCategorizerFunction
{
    public static class AuthExtensions
    {
        
        public static async Task<bool> IsAuthorizedAsync(this HttpRequest req,ILogger logger)
        {
            // This triggers the logic defined in your Program.cs
            var result = await req.HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
            {
                logger.LogError($"Auth failed: {result.Failure?.Message}");
            }
            else
            {
                logger.LogInformation("Auth succeeded");
            }
            return result.Succeeded;
        }
    }
}
