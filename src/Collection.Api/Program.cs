using Collection.Infrastructure;          // our DbContext lives here
using Microsoft.EntityFrameworkCore;      // UseSqlite, DbContext
using Microsoft.AspNetCore.RateLimiting;  // (cheap-mode safety)
using System.Threading.RateLimiting;

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
builder.Services.AddSwaggerGen();

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

//Items: filtering + pagination
api.MapGet("/items", async (AppDbContext db, string? platform, bool? isCib, int page = 1, int pageSize = 50) =>
{
    page = page < 1 ? 1 : page;
    pageSize = Math.Clamp(pageSize, 1, 100);

    var q = db.Items.Include(i => i.Platform).AsNoTracking();

    if (!string.IsNullOrWhiteSpace(platform)) q = q.Where(i => i.Platform!.Name == platform);

    if (isCib is not null) q = q.Where(i => (i.HasBox && i.HasManual) == isCib);

    var total = await q.CountAsync();

    var items = await q
        .OrderByDescending(i => i.EstimatedValue)
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

app.Run();
