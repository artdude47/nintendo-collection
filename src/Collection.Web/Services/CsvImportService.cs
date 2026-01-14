using Collection.Domain;
using Collection.Infrastructure;
using CsvHelper; 
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;
using System.Globalization;

namespace Collection.Web.Services;

public class CsvImportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CsvImportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public record ImportRow(bool IsValid, string Message, Item? Item);
    public record ImportReport(List<ImportRow> Rows);

    public async Task<ImportReport> ProcessAsync(Stream fileStream, bool dryRun)
    {
        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower(),
            MissingFieldFound = null
        });

        var records = csv.GetRecords<CsvItemDto>().ToList();
        var rows = new List<ImportRow>();

        using var db = await _dbFactory.CreateDbContextAsync();
        var platforms = await db.Platforms.ToListAsync();

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Title))
            {
                rows.Add(new ImportRow(false, "Missing Title", null));
                continue;
            }

            var platformName = string.IsNullOrWhiteSpace(record.Platform) ? "Unknown" : record.Platform;
            var platform = platforms.FirstOrDefault(p => p.Name.Equals(platformName, StringComparison.OrdinalIgnoreCase));

            if (platform == null)
            {
                platform = new Platform { Name = platformName };
                db.Platforms.Add(platform);
                platforms.Add(platform); 
            }

            var item = new Item
            {
                Title = record.Title,
                Platform = platform,

                Condition = Enum.TryParse<Condition>(record.Condition, true, out var c) ? c : Condition.Good,
                Kind = Enum.TryParse<ItemKind>(record.Kind, true, out var k) ? k : ItemKind.Game,
                Region = record.Region ?? "NTSC-U",
                HasBox = record.HasBox == "Yes" || record.HasBox == "true",
                HasManual = record.HasManual == "Yes" || record.HasManual == "true",
                EstimatedValue = decimal.TryParse(record.EstimatedValue, out var ev) ? ev : null,
                PurchasePrice = decimal.TryParse(record.PurchasePrice, out var pp) ? pp : null,
                PurchaseDate = DateOnly.TryParse(record.PurchaseDate, out var pd) ? pd : null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            rows.Add(new ImportRow(true, "OK", item));

            if (!dryRun)
            {
                db.Items.Add(item);
            }
        }

        if (!dryRun)
        {
            await db.SaveChangesAsync();
        }

        return new ImportReport(rows);
    }

    // Helper class to match CSV headers
    public class CsvItemDto
    {
        public string? Title { get; set; }
        public string? Platform { get; set; }
        public string? Condition { get; set; }
        public string? Kind { get; set; }
        public string? Region { get; set; }
        public string? HasBox { get; set; }
        public string? HasManual { get; set; }
        public string? EstimatedValue { get; set; }
        public string? PurchasePrice { get; set; }
        public string? PurchaseDate { get; set; }
    }
}