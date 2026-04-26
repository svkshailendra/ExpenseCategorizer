using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace ExpenseCategorizer.Shared
{
    public class Expense
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();   // Unique identifier
        public string Description { get; set; } = string.Empty;  // Extracted text from receipt
        public string Category { get; set; } = string.Empty;   // Predicted category (Travel, Medical, etc.)
        public string Explanation { get; set; } = string.Empty;  // AI-generated plain English summary
        public DateTime Date { get; set; }       // When the expense was added
        public decimal Amount { get; set; }
        [JsonProperty("userId")]
        public string UserId { get; set; } // To associate expense with a user
    }
}