using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication; 

namespace ExpenseCategorizerFunction
{
    public static class AuthHelper
    {
        public static async Task<string?> GetUserIdAsync(HttpRequest req)
        {
            var result = await req.HttpContext.AuthenticateAsync();
            if (!result.Succeeded)
                return null;

            var principal = result.Principal;
            return principal?.FindFirst("oid")?.Value
                ?? principal?.FindFirst("sub")?.Value
                ?? principal?.FindFirst("preferred_username")?.Value;
        }
    }
}
