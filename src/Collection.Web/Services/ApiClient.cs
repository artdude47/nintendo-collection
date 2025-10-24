
using Collection.Domain;
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

        public class Page<T>
        {
            public int total { get; set; }
            public int page { get; set; }
            public int pageSize { get; set; }
            public List<T> items { get; set; } = new();
        }

        public class PlatformDto
        {
            public int id { get; set; }
            public string name { get; set; } = "";
        }

        public class ItemDto
        {
            public int id { get; set; }
            public string title { get; set; } = "";
            public string region { get; set; } = "NTSC-U";
            public string? notes { get; set; }

            // enum as string from API
            public string condition { get; set; } = "Good";

            public bool hasBox { get; set; }
            public bool hasManual { get; set; }

            public decimal? purchasePrice { get; set; }
            public string? purchaseDate { get; set; }
            public decimal? estimatedValue { get; set; }

            public PlatformDto? platform { get; set; }
            public string? publisher { get; set; }
            public string? developer { get; set; }
            public int? releaseYear { get; set; }
            public string? genre { get; set; }
            public string? barcode { get; set; }

            public string? kind { get; set; }
        }

        public class StatsDto
        {
            public int totalItems { get; set; }
            public int totalCib { get; set; }
            public double totalPurchasePrice { get; set; }
            public double totalEstimatedValue { get; set; }
            public double totalEstimatedProfit { get; set; }
            public List<StatsByPlatform> byPlatform { get; set; } = new();
            public class StatsByPlatform
            {
                public string platform { get; set; } = "";
                public int count { get; set; }
                public int cib { get; set; }
                public double value { get; set; }
            }
        }

        public async Task<StatsDto> GetStatsAsync() => await _http.GetFromJsonAsync<StatsDto>("/api/stats") ?? new StatsDto();

        public async Task<List<PlatformDto>> GetPlatformsAsync() => await _http.GetFromJsonAsync<List<PlatformDto>>("/api/platforms") ?? new();
        public async Task<Page<ItemDto>> GetItemsAsync(
            string? platform, bool? isCib, int page, int pageSize, string? q = null, string? sort = null, string? kind = null)
        {
            var qs = new List<string>();
            if (!string.IsNullOrWhiteSpace(platform)) qs.Add($"platform={Uri.EscapeDataString(platform)}");
            if (isCib is not null) qs.Add($"isCib={isCib.ToString()!.ToLower()}");
            if (!string.IsNullOrWhiteSpace(q)) qs.Add($"q={Uri.EscapeDataString(q)}");
            if (!string.IsNullOrWhiteSpace(sort)) qs.Add($"sort={Uri.EscapeDataString(sort)}");
            if (!string.IsNullOrWhiteSpace(kind)) qs.Add($"kind={Uri.EscapeDataString(kind)}");
            qs.Add($"page={page}");
            qs.Add($"pageSize={pageSize}");

            var url = "/api/items" + "?" + string.Join("&", qs);
            var res = await _http.GetFromJsonAsync<Page<ItemDto>>(url);

            return res ?? new Page<ItemDto>
            {
                total = 0,
                page = page,
                pageSize = pageSize,
                items = new List<ItemDto>()
            };
        }

        public async Task<bool> CreateItemAsync(object body)
        {
            var resp = await _http.PostAsJsonAsync("/api/items", body);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateItemsAsync(int id, ItemDto item)
        {
            var resp = await _http.PutAsJsonAsync($"/api/items/{id}", item);
            return resp.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var resp = await _http.DeleteAsync($"/api/items/{id}");
            return resp.IsSuccessStatusCode;
        }

        public string ExportUrl() => "/api/export.csv";

        public async Task<string> ImportCsvAsync(Stream file, string fileName, bool dryRun)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(file), "file", fileName);
            var resp = await _http.PostAsync($"/api/import?dryRun={dryRun.ToString().ToLower()}", content);
            return await resp.Content.ReadAsStringAsync(); // show raw JSON result
        }

        public async Task<ItemDto?> GetItemByIdAsync(int id)
        {
            return await _http.GetFromJsonAsync<ItemDto>($"/api/items/{id}");
        }
    }
}
