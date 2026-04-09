using ExpenseCategorizer.Shared;
using ExpenseCategorizerFunction;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public class ExpenseService
{
    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public ExpenseService(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task<bool> UploadExpense(IBrowserFile file)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10 MB limit
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            content.Add(fileContent, "file", file.Name);

            var response = await _http.PostAsync("upload", content); // matches UploadExpense function route
            response.EnsureSuccessStatusCode();

            // Optionally deserialize the returned Expense object
            var json = await response.Content.ReadAsStringAsync();
            var expenses = JsonSerializer.Deserialize<List<Expense>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // You could store or return this expense if needed
            return expenses != null && expenses.Count > 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<List<Expense>> GetExpensesAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<Expense>>("expenses");
            return result ?? new List<Expense>();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return new List<Expense>();
        }
    }

    public async Task UpdateExpenseAsync(Expense expense)
    {
        await _http.PutAsJsonAsync($"expenses/{expense.Id}/{expense.Category}", expense);
    }

    public async Task DeleteExpenseAsync(string id, string category)
    {
        await _http.DeleteAsync($"expenses/{id}/{category}");
    }


    public async Task<bool> PreviewReport()
    {
        try
        {
            var response = await _http.GetAsync("report");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                //var fileName = $"ExpenseReport_{DateTime.Now:yyyyMMdd}.pdf";
                //await File.WriteAllBytesAsync(fileName, bytes);
                var base64 = Convert.ToBase64String(bytes);

                //var url = "data:application/pdf;base64," + base64;
                await _js.InvokeVoidAsync("openPdf", base64);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public async Task<bool> DownloadReport()
    {
        try
        {
            var response = await _http.GetAsync("report");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                var fileName = $"ExpenseReport_{DateTime.Now:yyyyMMdd}.pdf";

                // Call JS function defined in site.js
                await _js.InvokeVoidAsync("downloadFile", fileName, base64);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }
}
