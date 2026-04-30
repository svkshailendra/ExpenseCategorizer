using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
using BlazorApplicationInsights;

namespace ExpenseCategorizer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");
            // Force safe JSON options globally
            builder.Services.AddSingleton(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            //Register App Insights
            builder.Services.AddBlazorApplicationInsights(config =>
            {
                config.ConnectionString = builder.Configuration["AppInsightsConnectionString"];
            });

            // Add MSAL authentication
            builder.Services.AddMsalAuthentication(options =>
            {
                //options.ProviderOptions.Authentication.Authority =
                  // "https://login.microsoftonline.com/16f6111d-ab26-4e29-85fe-6e700ea29f7f";
                options.ProviderOptions.Authentication.Authority =
                    "https://login.microsoftonline.com/common";
                options.ProviderOptions.Authentication.ClientId =
                    "a81c2799-74ae-4849-a497-3032d71b34b1";

                options.ProviderOptions.Authentication.PostLogoutRedirectUri = "https://purple-water-04d7b3300.7.azurestaticapps.net/";
                // Request the API scope you exposed
                options.ProviderOptions.DefaultAccessTokenScopes.Add("abac5165-ffcf-46dd-9659-98fce7cbc6d8/user_impersonation");
            });
            
            builder.Services.AddHttpClient("ExpenseCategorizer.API",
                                client => client.BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/"))
                            .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
                            .ConfigureHandler(
                                authorizedUrls: new[] { "https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net" },
                                scopes: new[] { "abac5165-ffcf-46dd-9659-98fce7cbc6d8/user_impersonation" }));

            
            // Supply HttpClient instances for injection
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient("ExpenseCategorizer.API"));

            // Your own services
            builder.Services.AddScoped<ExpenseService>();

            await builder.Build().RunAsync();
        }
    }
}
