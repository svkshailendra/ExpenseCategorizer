using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExpenseCategorizer.Shared
{
    public static class JsonHelper
    {
        public static JsonSerializerOptions SafeOptions => new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }
}
