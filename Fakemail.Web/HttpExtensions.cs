using System.Text.Json;

namespace Fakemail.Web
{
    internal static class HttpExtensions
    {
        private static readonly JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static async Task<T> FromJsonAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync() ?? throw new Exception();
            return JsonSerializer.Deserialize<T>(json, options) ?? throw new Exception();
        }
    }
}