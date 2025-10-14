using Collection.Domain;
using Collection.Infrastructure;          // our DbContext lives here
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;  // (cheap-mode safety)
using Microsoft.EntityFrameworkCore;      // UseSqlite, DbContext
using Microsoft.OpenApi.Models;
using System.Globalization;
using Microsoft.AspNetCore.Components;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence: SQLite ---
// For Dev: keeps a file named app.db next to the API.
// For Fly.io: we’ll point this to /data/app.db (mounted volume).
var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(cs));

// --- Safety: simple per-IP rate limiter (prevents abuse on free tier) ---
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("tight", o =>
    {
        o.PermitLimit = 60;                 // 60 requests/minute
        o.Window = TimeSpan.FromMinutes(1);
        o.QueueLimit = 0;
    }));

// --- Swagger so you can test endpoints easily ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date" });
    c.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date" });
});

builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});
builder.Services.AddScoped<Collection.Web.Services.ApiClient>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Minimal API endpoints (MVP) ---
var api = app.MapGroup("/api").RequireRateLimiting("tight");

// Health-check
api.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));

// Platforms: seeded list 
api.MapGet("/platforms", async (AppDbContext db) =>
    await db.Platforms.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

//Items: filtering + search + sorting + pagination
api.MapGet("/items", async (
    AppDbContext db, 
    string? platform, 
    bool? isCib, 
    string? q,
    string? sort,
    int page = 1, 
    int pageSize = 50) =>
{
    page = page < 1 ? 1 : page;
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = db.Items.Include(i => i.Platform).AsNoTracking();

    if (!string.IsNullOrWhiteSpace(platform)) query = query.Where(i => i.Platform!.Name == platform);

    if (isCib is not null) query = query.Where(i => (i.HasBox && i.HasManual) == isCib);

    if (!string.IsNullOrWhiteSpace(q))
    {
        var pattern = $"%{q.Trim()}%";
        query = query.Where(i => EF.Functions.Like(i.Title, pattern));
    }

    var total = await query.CountAsync();

    query = (sort ?? "value_desc") switch
    {
       "title_asc" => query.OrderBy(i => i.Title),
        "title_desc" => query.OrderByDescending(i => i.Title),
        "value_asc" => query.OrderBy(i => i.EstimatedValue ?? 0),
        "value_desc" => query.OrderByDescending(i => i.EstimatedValue ?? 0),
        "created_asc" => query.OrderBy(i => i.CreatedAt),
        "created_desc" => query.OrderByDescending(i => i.CreatedAt),
        _ => query.OrderByDescending(i => i.EstimatedValue ?? 0),
    };

    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return Results.Ok(new { total, page, pageSize, items });
});

// Create Item: minimal validation
api.MapPost("/items", async (AppDbContext db, Collection.Domain.Item item) =>
{
    if (string.IsNullOrWhiteSpace(item.Title)) return Results.BadRequest("Title required");
    item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
    db.Items.Add(item);
    await db.SaveChangesAsync();
    return Results.Created($"/api/items/{item.Id}", item);
});

api.MapGet("/export.csv", async (AppDbContext db) =>
{
    var items = await db.Items.Include(i => i.Platform).AsNoTracking().ToListAsync();

    static string Csv(string? s) => "\"" + (s ?? string.Empty).Replace("\"", "\"\"") + "\"";
    static string D(decimal? d) => d?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "";
    static string Dt(DateOnly? d) => d?.ToString("yyyy-MM-dd") ?? "";

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("Id,Title,Platform,Region,Condition,HasBox,HasManual,PurchasePrice,PurchaseDate,EstimatedValue,Notes,CreatedAt,UpdatedAt");

    foreach (var i in items)
    {
        sb.AppendLine(string.Join(",",
            i.Id,
            Csv(i.Title),
            Csv(i.Platform?.Name),
            Csv(i.Region),
            Csv(i.Condition.ToString()),
            i.HasBox ? "Yes" : "No",
            i.HasManual ? "Yes" : "No",
            D(i.PurchasePrice),
            Dt(i.PurchaseDate),
            D(i.EstimatedValue),
            Csv(i.Notes),
            i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            i.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        ));
    }

    var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    return Results.File(bytes, "text/csv", "collection.csv");
});

api.MapPost("/import", async (
    IFormFile file,       
    bool dryRun,         
    AppDbContext db) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest("Form field 'file' is required and must not be empty.");

    using var stream = file.OpenReadStream();
    using var reader = new StreamReader(stream);

    string? header = await reader.ReadLineAsync();
    if (header is null)
        return Results.BadRequest("CSV appears empty.");

    var report = new Collection.Api.ImportReport { DryRun = dryRun };
    int line = 1;
    string? lineText;

    var platformMap = await db.Platforms.AsNoTracking()
        .ToDictionaryAsync(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

    while ((lineText = await reader.ReadLineAsync()) is not null)
    {
        line++;
        report.RowsRead++;
        var cells = SplitCsv(lineText);
        var row = new Collection.Api.ItemImportRow
        {
            LineNumber = line,
            Title = Get(cells, 0),
            Platform = Get(cells, 1),
            Region = Get(cells, 2),
            Condition = Get(cells, 3),
            HasBox = Get(cells, 4),
            HasManual = Get(cells, 5),
            PurchasePrice = Get(cells, 6),
            PurchaseDate = Get(cells, 7),
            EstimatedValue = Get(cells, 8),
            Notes = Get(cells, 9)
        };

        var rr = new Collection.Api.ImportReport.RowResult
        {
            LineNumber = row.LineNumber,
            Title = row.Title,
            Platform = row.Platform
        };

        // ---- validation ----
        if (string.IsNullOrWhiteSpace(row.Title))
            rr.Errors.Add("Title is required.");

        int? platformId = null;
        if (string.IsNullOrWhiteSpace(row.Platform))
            rr.Errors.Add("Platform is required.");
        else if (!platformMap.TryGetValue(row.Platform.Trim(), out var pid))
            rr.Errors.Add($"Unknown platform '{row.Platform}'.");
        else
            platformId = pid;

        Condition condition = Condition.VeryGood;
        if (!string.IsNullOrWhiteSpace(row.Condition) &&
            !Enum.TryParse<Condition>(row.Condition.Trim(), true, out condition))
            rr.Errors.Add($"Invalid condition '{row.Condition}'. Allowed: {string.Join(", ", Enum.GetNames(typeof(Condition)))}");

        bool hasBox = ParseBool(row.HasBox);
        bool hasManual = ParseBool(row.HasManual);
        decimal? purchasePrice = ParseDecimal(row.PurchasePrice, rr.Errors, "PurchasePrice");
        decimal? estimatedValue = ParseDecimal(row.EstimatedValue, rr.Errors, "EstimatedValue");
        DateOnly? purchaseDate = ParseDate(row.PurchaseDate, rr.Errors, "PurchaseDate");

        if (rr.Errors.Count == 0 && !dryRun)
        {
            db.Items.Add(new Item
            {
                Title = row.Title!.Trim(),
                PlatformId = platformId!.Value,
                Region = string.IsNullOrWhiteSpace(row.Region) ? "NTSC-U" : row.Region!.Trim(),
                Condition = condition,
                HasBox = hasBox,
                HasManual = hasManual,
                PurchasePrice = purchasePrice,
                PurchaseDate = purchaseDate,
                EstimatedValue = estimatedValue,
                Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes!.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            report.RowsInserted++;
        }

        report.Rows.Add(rr);
    }

    if (!dryRun) await db.SaveChangesAsync();
    return Results.Ok(report);

    // --- helpers ---
    static string? Get(string[] a, int i) => i < a.Length ? a[i] : null;
    static bool ParseBool(string? s) => s is not null && s.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
    static decimal? ParseDecimal(string? s, List<string> errors, string name)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d))
        {
            if (d < 0) errors.Add($"{name} must be >= 0.");
            return d;
        }
        errors.Add($"{name} must be a number (use '.' as decimal separator).");
        return null;
    }
    static DateOnly? ParseDate(string? s, List<string> errors, string name)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (DateOnly.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        errors.Add($"{name} must be yyyy-MM-dd.");
        return null;
    }
    static string[] SplitCsv(string line)
    {
        var result = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"') { cur.Append('\"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes) { result.Add(cur.ToString()); cur.Clear(); }
            else { cur.Append(c); }
        }
        result.Add(cur.ToString());
        return result.ToArray();
    }
}).Accepts<IFormFile>("multipart/form-data").Produces<Collection.Api.ImportReport>(StatusCodes.Status200OK).Produces(StatusCodes.Status400BadRequest).DisableAntiforgery();

app.UseStaticFiles();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
//app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
