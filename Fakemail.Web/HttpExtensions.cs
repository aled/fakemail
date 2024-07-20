using System.Text.Json;

namespace Fakemail.Web
{
    internal static class HttpExtensions
    {
        public static async Task<T> FromJsonAsync<T>(this HttpContent content)
        {
            var json = await content.ReadAsStringAsync() ?? throw new Exception();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception();
        }
    }
}