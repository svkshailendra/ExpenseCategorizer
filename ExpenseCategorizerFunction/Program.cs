using ExpenseCategorizerFunction;
using ExpenseCategorizerFunction.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PdfSharp.Fonts;
using System.Configuration;
using System.Net;
using static ExpenseCategorizerFunction.Services.IExpenseServices;

GlobalFontSettings.FontResolver = new CustomFontResolver();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Read from environment variables
        string cvEndpoint = Environment.GetEnvironmentVariable("ComputerVisionEndpoint");
        string cvKey = Environment.GetEnvironmentVariable("ComputerVisionKey");

        // Register your services here
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<ReportService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<IOcrService>(new OcrService(cvEndpoint, cvKey));
        services.AddSingleton<IMlNetService, MlNetService>();
        services.AddSingleton<IOpenAiService, OpenAiService>();

    })
    .Build();

host.Run();
