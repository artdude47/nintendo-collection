using Collection.Domain;
using Collection.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Collection.Web.Services;

public class InventoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public InventoryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Platform>> GetPlatformsAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Platforms.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<ApiClient.Page<Item>> GetItemsAsync(
        string? platform,
        bool? isCib,
        string? search,
        string? sort,
        string? kind,
        string? region,
        string? genre,
        int pageIndex,
        int pageSize)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Items.Include(i => i.Platform).AsNoTracking();

        // --- Filters ---
        if (!string.IsNullOrWhiteSpace(platform))
            query = query.Where(i => i.Platform != null && i.Platform.Name == platform);

        if (isCib.HasValue)
            query = isCib.Value ? query.Where(i => i.IsCib) : query.Where(i => !i.IsCib);

        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<ItemKind>(kind, true, out var k))
            query = query.Where(i => i.Kind == k);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(i => i.Region == region);

        if (!string.IsNullOrWhiteSpace(genre))
            query = query.Where(i => i.Genre == genre);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var pattern = $"%{s}%";
            query = query.Where(i => EF.Functions.Like(i.Title, pattern) ||
                                     EF.Functions.Like(i.Notes ?? "", pattern));
        }

        // --- COUNT ---
        var total = await query.CountAsync();

        // --- SORTING ---
        query = (sort ?? "title_asc") switch
        {
            "title_asc" => query.OrderBy(i => i.Title),
            "title_desc" => query.OrderByDescending(i => i.Title),
            "value_desc" => query.OrderByDescending(i => i.EstimatedValue ?? 0),
            "created_desc" => query.OrderByDescending(i => i.CreatedAt),
            _ => query.OrderBy(i => i.Title)
        };

        // --- PAGING ---
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ApiClient.Page<Item> { items = items, total = total, page = pageIndex, pageSize = pageSize };
    }

    public async Task SaveItemAsync(Item item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();

        if (item.Id == 0)
        {
            // --- CREATE NEW ---
            item.CreatedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;

            // Ensure we don't try to insert a duplicate Platform object
            // We just want to link by ID
            item.Platform = null;

            db.Items.Add(item);
        }
        else
        {
            // --- UPDATE EXISTING ---
            var existing = await db.Items.FindAsync(item.Id);
            if (existing is null) return; // Or throw exception

            // Update scalar properties
            existing.Title = item.Title;
            existing.PlatformId = item.PlatformId;
            existing.Condition = item.Condition;
            existing.Region = item.Region;
            existing.Kind = item.Kind;
            existing.HasBox = item.HasBox;
            existing.HasManual = item.HasManual;
            existing.PurchasePrice = item.PurchasePrice;
            existing.PurchaseDate = item.PurchaseDate;
            existing.EstimatedValue = item.EstimatedValue;
            existing.Notes = item.Notes;
            existing.Publisher = item.Publisher;
            existing.Developer = item.Developer;
            existing.Genre = item.Genre;
            existing.ReleaseYear = item.ReleaseYear;
            existing.Barcode = item.Barcode;

            existing.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteItemAsync(int id)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var item = await db.Items.FindAsync(id);
        if (item is not null)
        {
            db.Items.Remove(item);
            await db.SaveChangesAsync();
        }
    }
}

