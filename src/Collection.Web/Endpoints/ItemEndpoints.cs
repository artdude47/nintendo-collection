using Collection.Domain;
using Collection.Infrastructure;
using Collection.Web.Services;
using CsvHelper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Collection.Web.Endpoints;

public static class ItemEndpoints
{
    public static void MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        // Define the group with common prefix
        var group = app.MapGroup("/api").RequireRateLimiting("tight");

        // Public Routes 
        group.MapGet("/health", () => Results.Ok(new { ok = true, time = DateTime.UtcNow }));
        group.MapGet("/platforms", GetPlatforms);
        group.MapGet("/genres", () => Results.Ok(new[] { "Action", "Adventure", "RPG", "Platformer", "Puzzle", "Sports", "Racing", "Strategy", "Fighting", "Shooter", "Simulation", "Party", "Music", "Other" }));
        group.MapGet("/meta/kinds", () => Results.Ok(Enum.GetNames(typeof(ItemKind))));

        // Item CRUD
        group.MapGet("/items", GetItems);
        group.MapGet("/items/{id:int}", GetItemById);


        group.MapPost("/items", CreateItem);
        group.MapPut("/items/{id:int}", UpdateItem);
        group.MapDelete("/items/{id:int}", DeleteItem);

        // Import / Export
        group.MapGet("/export.csv", async (InventoryService inv) => await ExportCsv(inv));
        group.MapPost("/import", ImportCsv).DisableAntiforgery();
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

        return Results.Ok(new { total = 0, page, pageSize, items = new List<Item>() }); 
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
        CsvImportService csvService) 
    {
        if (file is null || file.Length == 0) return Results.BadRequest("File required");

        using var stream = file.OpenReadStream();
        var report = await csvService.ProcessAsync(stream, dryRun);

        return Results.Ok(report);
    }

    private static async Task<IResult> ExportCsv(InventoryService inventory)
    {
        var items = await inventory.GetAllItemsAsync();

        using var memory = new MemoryStream();
        using var writer = new StreamWriter(memory);
        using var csv = new CsvHelper.CsvWriter(writer, System.Globalization.CultureInfo.InvariantCulture);

        var exportRows = items.Select(i => new
        {
            i.Id,
            i.Title,
            Platform = i.Platform?.Name,
            i.Kind,
            i.Condition,
            i.Region,
            CIB = (i.HasBox && i.HasManual) ? "Yes" : "No",
            Value = i.EstimatedValue,
            Paid = i.PurchasePrice,
            Bought = i.PurchaseDate?.ToString("yyyy-MM-dd")
        });

        await csv.WriteRecordsAsync(exportRows);
        await writer.FlushAsync();

        return Results.File(
            memory.ToArray(),
            "text/csv",
            $"collection-export-{DateTime.UtcNow:yyyyMMdd}.csv");
    }
}