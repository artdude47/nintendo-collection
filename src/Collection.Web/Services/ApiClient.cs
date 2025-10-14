
using System.Net.Http.Json;

namespace Collection.Web.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        public ApiClient(HttpClient http)
        {
            _http = http;
        }

        public record Page<T>(int total, int page, int pageSize, List<T> items);
        public record PlatformDto(int id, string name);
        public record ItemDto(
            int id, string title, string region, string? notes,
            string condition, bool hasBox, bool hasManual,
            decimal? purchasePrice, string? purchaseDate, decimal? estimatedValue,
            PlatformDto? platform);

        public async Task<List<PlatformDto>> GetPlatformsAsync() => await _http.GetFromJsonAsync<List<PlatformDto>>("/api/platforms") ?? new();
        public async Task<Page<ItemDto>> GetItemsAsync(string? platform, bool? isCib, int page, int pageSize)
        {
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(platform)) qs.Add($"platform={Uri.EscapeDataString(platform)}");
            if (isCib is not null) qs.Add($"isCib={isCib.ToString()!.ToLower()}");
            qs.Add($"page={page}");
            qs.Add($"pageSize={pageSize}");
            var url = "/api/items" + "?" + string.Join("&", qs);
            var res = await _http.GetFromJsonAsync<Page<ItemDto>>(url);
            return res ?? new Page<ItemDto>(0, page, pageSize, new());
        }

        public string ExportUrl() => "/api/export.csv";

        public async Task<string> ImportCsvAsync(Stream file, string fileName, bool dryRun)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(file), "file", fileName);
            var resp = await _http.PostAsync($"/api/import?dryRun={dryRun.ToString().ToLower()}", content);
            return await resp.Content.ReadAsStringAsync(); // show raw JSON result
        }
    }
}
