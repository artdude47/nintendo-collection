using Collection.Domain;
using Collection.Infrastructure;
using Collection.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Collection.Web.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        // Define the group with common prefix and security
        var group = app.MapGroup("/api").RequireRateLimiting("tight");

        // Public Routes
        group.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));
        group.MapGet("/platforms", GetPlatforms);
        group.MapGet("/genres", () => Results.Ok(new[] { "Action", "Adventure", "RPG", "Platformer", "Puzzle", "Sports", "Racing", "Strategy", "Fighting", "Shooter", "Simulation", "Party", "Music", "Other" }));
        group.MapGet("/meta/kinds", () => Results.Ok(Enum.GetNames(typeof(ItemKind))));

        // Item CRUD
        group.MapGet("/items", GetItems);
        group.MapGet("/items/{id:int}", GetItemById);

        // Secured Routes (Require "CanEdit" policy)
        var secure = group.MapGroup("/").RequireAuthorization("CanEdit");

        secure.MapPost("/items", CreateItem);
        secure.MapPut("/items/{id:int}", UpdateItem);
        secure.MapDelete("/items/{id:int}", DeleteItem);

        // Import / Export
        group.MapGet("/export.csv", ExportCsv);
        secure.MapPost("/import", ImportCsv).DisableAntiforgery(); // Import uses FormFile
    }

    // --- Endpoint Handlers ---

    static async Task<IResult> GetPlatforms(AppDbContext db) =>
        Results.Ok(await db.Platforms.AsNoTracking().OrderBy(p => p.Id).ToListAsync());

    static async Task<IResult> GetItems(
        AppDbContext db,
        string? platform,
        bool? isCib,
        string? q,
        string? sort,
        string? kind,
        string? region,
        string? genre,
        int page = 1,
        int pageSize = 50)
    {
        // ... PASTE YOUR EXISTING GET /items LOGIC HERE ...
        // (For brevity, I'm assuming you copy-paste the logic from your old Program.cs query block)
        // If you need me to write this out fully, let me know!
        return Results.Ok(new { total = 0, page, pageSize, items = new List<Item>() }); // Placeholder
    }

    static async Task<IResult> GetItemById(int id, AppDbContext db)
    {
        var item = await db.Items.Include(i => i.Platform).AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    static async Task<IResult> CreateItem(AppDbContext db, Item item)
    {
        if (string.IsNullOrWhiteSpace(item.Title)) return Results.BadRequest("Title required");
        item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return Results.Created($"/api/items/{item.Id}", item);
    }

    static async Task<IResult> UpdateItem(int id, AppDbContext db, Item dto)
    {
        var entity = await db.Items.FindAsync(id);
        if (entity is null) return Results.NotFound();

        // Map updates
        entity.Title = dto.Title;
        entity.PlatformId = dto.PlatformId;
        entity.Condition = dto.Condition;
        // ... (Map rest of fields)

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    static async Task<IResult> DeleteItem(int id, AppDbContext db)
    {
        var entity = await db.Items.FindAsync(id);
        if (entity is null) return Results.NotFound();
        db.Items.Remove(entity);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    // --- The Clean Import Handler ---
    static async Task<IResult> ImportCsv(
        IFormFile file,
        bool dryRun,
        CsvImportService csvService) // <--- Inject Service Here
    {
        if (file is null || file.Length == 0) return Results.BadRequest("File required");

        using var stream = file.OpenReadStream();
        var report = await csvService.ProcessAsync(stream, dryRun);

        return Results.Ok(report);
    }

    static async Task<IResult> ExportCsv(AppDbContext db)
    {
        // ... Paste your existing export logic ...
        return Results.Ok();
    }
}