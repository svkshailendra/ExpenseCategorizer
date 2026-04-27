using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseCategorizer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            //var builder = WebAssemblyHostBuilder.CreateDefault(args);
            //builder.RootComponents.Add<App>("#app");
            //builder.RootComponents.Add<HeadOutlet>("head::after");

            //////builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7071/api/") });
            ////builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/") });


            //builder.Services.AddScoped<ExpenseService>();
            ////builder.Logging.SetMinimumLevel(LogLevel.Debug);
            ////builder.Logging.AddConsole();

            ////Add MSAL authentication
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    //builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);

            //    // Request the API scope you exposed
            //    //options.ProviderOptions.DefaultAccessTokenScopes.Add(
            //    //    "api://abac5165-ffcf-46dd-9659-98fce7cbc6d8/user_impersonation");

            //    options.ProviderOptions.Authentication.Authority = "https://login.microsoftonline.com/16f6111d-ab26-4e29-85fe-6e700ea29f7f";
            //    options.ProviderOptions.Authentication.ClientId = "a81c2799-74ae-4849-a497-3032d71b34b1";

            //    // options.ProviderOptions.DefaultAccessTokenScopes.Add("api://abac5165-ffcf-46dd-9659-98fec7cbc6d8/user_impersonation");

            //});

            //// Configure HttpClient to use the authenticated handler
            //builder.Services.AddHttpClient("ExpenseCategorizer.API",
            //    client => client.BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/"))
            //    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();

            //// Supply HttpClient instances for injection
            //builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
            //    .CreateClient("ExpenseCategorizer.API"));

            //builder.Services.AddScoped<ExpenseService>();

            //await builder.Build().RunAsync();

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


            // Add MSAL authentication
            builder.Services.AddMsalAuthentication(options =>
            {
                //options.ProviderOptions.Authentication.Authority =
                  // "https://login.microsoftonline.com/16f6111d-ab26-4e29-85fe-6e700ea29f7f";
                options.ProviderOptions.Authentication.Authority =
                    "https://login.microsoftonline.com/common";
                options.ProviderOptions.Authentication.ClientId =
                    "a81c2799-74ae-4849-a497-3032d71b34b1";

                options.ProviderOptions.Authentication.PostLogoutRedirectUri = "https://localhost:7045/authentication/logout-callback";

                // Request the API scope you exposed
                options.ProviderOptions.DefaultAccessTokenScopes.Add(
                    "abac5165-ffcf-46dd-9659-98fce7cbc6d8/user_impersonation");
            });
            //builder.Services.AddMsalAuthentication(options =>
            //{
            //    builder.Configuration.Bind("AzureAd", options.ProviderOptions);

            //    options.ProviderOptions.DefaultAccessTokenScopes.Add(
            //        "api://abac5165-ffcf-46dd-9659-98fec7cbc6d8/user_impersonation");
            //});

            // Configure HttpClient to use the authenticated handler
            //builder.Services.AddHttpClient("ExpenseCategorizer.API",
            //    client => client.BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/"))
            //    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
            builder.Services.AddHttpClient("ExpenseCategorizer.API",
                                client => client.BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/"))
                            .AddHttpMessageHandler(sp => sp.GetRequiredService<AuthorizationMessageHandler>()
                            .ConfigureHandler(
                                authorizedUrls: new[] { "https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net" },
                                scopes: new[] { "abac5165-ffcf-46dd-9659-98fce7cbc6d8/user_impersonation" }));

            ////builder.Services.AddHttpClient("ExpenseCategorizer.API",
            ////    client => client.BaseAddress = new Uri("http://localhost:7071/api/"))
            ////    .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();


            // Supply HttpClient instances for injection
            builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>()
                .CreateClient("ExpenseCategorizer.API"));

            // Your own services
            builder.Services.AddScoped<ExpenseService>();

            await builder.Build().RunAsync();
        }
    }
}
