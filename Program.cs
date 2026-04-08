using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ExpenseCategorizer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            //builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:7071/api/") });
            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("https://expensefunctiondemo-h4afcqhnfqbceqht.australiaeast-01.azurewebsites.net/api/") });
        
            builder.Services.AddScoped<ExpenseService>();

            await builder.Build().RunAsync();
        }
    }
}
