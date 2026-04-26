using ExpenseCategorizerFunction;
using ExpenseCategorizerFunction.Services;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using PdfSharp.Fonts;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

GlobalFontSettings.FontResolver = new CustomFontResolver();

var host = new HostBuilder()
    // 1. MUST use ConfigureFunctionsWebApplication for ASP.NET Core integration
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        // 2. Services remain the same
        string cvEndpoint = Environment.GetEnvironmentVariable("ComputerVisionEndpoint");
        string cvKey = Environment.GetEnvironmentVariable("ComputerVisionKey");

        services.AddSingleton<DatabaseService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<IOcrService>(new OcrService(cvEndpoint, cvKey));
        services.AddSingleton<IMlNetService, MlNetService>();
        services.AddSingleton<IOpenAiService, OpenAiService>();

        // 3. Register Authentication services
        //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //    .AddMicrosoftIdentityWebApi(context.Configuration.GetSection("AzureAd"));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(
        jwtBearerOptions =>
        {
            // Bind JwtBearerOptions from configuration
            Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(context.Configuration.GetSection("AzureAd"), jwtBearerOptions);

            // Accept tokens from multiple issuers (multi-tenant)
            //jwtBearerOptions.TokenValidationParameters.ValidIssuers = new[]
            //{
            //    "https://login.microsoftonline.com/common/v2.0",
            //    "https://login.microsoftonline.com/organizations/v2.0"
            //};

            jwtBearerOptions.Authority = "https://login.microsoftonline.com/common/v2.0";

            jwtBearerOptions.TokenValidationParameters.ValidateIssuer = false;

            // 🔎 Add logging hooks
            jwtBearerOptions.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    // Use the logger from the service provider
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError($"❌ Auth Failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogInformation("✅ Token Validated!");
                    return Task.CompletedTask;
                }
            };
        },
        microsoftIdentityOptions =>
        {
            // Bind MicrosoftIdentityOptions from configuration
            Microsoft.Extensions.Configuration.ConfigurationBinder.Bind(context.Configuration.GetSection("AzureAd"), microsoftIdentityOptions);
        });

        // Required to enable the [Authorize] attribute
        services.AddAuthorization();
    })
    .Build();

await host.RunAsync();