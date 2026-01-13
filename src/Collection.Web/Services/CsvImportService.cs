using Collection.Domain;
using Collection.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Collection.Web.Services;

public class CsvImportReport
{
    public int RowsRead { get; set; }
    public int RowsInserted { get; set; }
    public bool DryRun { get; set; }
    public List<RowResult> Rows { get; set; } = new();

    public class RowResult
    {
        public int LineNumber { get; set; }
        public string? Title { get; set; }
        public string? Platform { get; set; }
        public List<string> Errors { get; set; } = new();
        public bool IsValid => Errors.Count == 0;
    }
}

public class CsvImportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CsvImportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<CsvImportReport> ProcessAsync(Stream stream, bool dryRun)
    {
        var report = new CsvImportReport { DryRun = dryRun };

        using var reader = new StreamReader(stream);

        // 1. Skip Header Row
        string? header = await reader.ReadLineAsync();
        if (header is null) return report;

        // 2. Create Context
        using var db = await _dbFactory.CreateDbContextAsync();

        // 3. Cache Platforms 
        var platformMap = await db.Platforms.AsNoTracking()
            .ToDictionaryAsync(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

        int lineNum = 1;
        string? lineText;

        while ((lineText = await reader.ReadLineAsync()) is not null)
        {
            lineNum++;
            report.RowsRead++;

            var cells = SplitCsv(lineText);

            var rawTitle = Get(cells, 0);
            var rawPlatform = Get(cells, 1);
            var rawRegion = Get(cells, 2);
            var rawCond = Get(cells, 3);
            var rawBox = Get(cells, 4);
            var rawManual = Get(cells, 5);
            var rawPrice = Get(cells, 6);
            var rawDate = Get(cells, 7);
            var rawValue = Get(cells, 8);
            var rawNotes = Get(cells, 9);

            var rawPublisher = Get(cells, 10);
            var rawDeveloper = Get(cells, 11);
            var rawGenre = Get(cells, 12);
            var rawYear = Get(cells, 13);
            var rawBarcode = Get(cells, 14);
            var rawKind = Get(cells, 15);

            var rr = new CsvImportReport.RowResult
            {
                LineNumber = lineNum,
                Title = rawTitle,
                Platform = rawPlatform
            };

            // --- VALIDATION ---
            if (string.IsNullOrWhiteSpace(rawTitle))
                rr.Errors.Add("Title is required.");

            int platformId = 0;
            if (string.IsNullOrWhiteSpace(rawPlatform))
                rr.Errors.Add("Platform is required.");
            else if (!platformMap.TryGetValue(rawPlatform.Trim(), out var pid))
                rr.Errors.Add($"Unknown platform '{rawPlatform}'.");
            else
                platformId = pid;

            // Parse Enums and Nullables safely
            if (!Enum.TryParse<Condition>(rawCond?.Trim(), true, out var condition))
                condition = Condition.Good; // Default

            ItemKind? kind = null;
            if (!string.IsNullOrWhiteSpace(rawKind) && Enum.TryParse<ItemKind>(rawKind.Trim(), true, out var k))
                kind = k;

            int? releaseYear = null;
            if (!string.IsNullOrWhiteSpace(rawYear) && int.TryParse(rawYear.Trim(), out var y))
                releaseYear = y;

            if (rr.IsValid && !dryRun)
            {
                var item = new Item
                {
                    Title = rawTitle!.Trim(),
                    PlatformId = platformId,
                    Region = rawRegion ?? "NTSC-U",
                    Condition = condition,
                    HasBox = ParseBool(rawBox),
                    HasManual = ParseBool(rawManual),
                    PurchasePrice = ParseDecimal(rawPrice),
                    PurchaseDate = ParseDate(rawDate),
                    EstimatedValue = ParseDecimal(rawValue),
                    Notes = rawNotes?.Trim(),

                    // New Fields
                    Publisher = rawPublisher?.Trim(),
                    Developer = rawDeveloper?.Trim(),
                    Genre = rawGenre?.Trim(),
                    ReleaseYear = releaseYear,
                    Barcode = rawBarcode?.Trim(),
                    Kind = kind,

                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                db.Items.Add(item);
                report.RowsInserted++;
            }
            report.Rows.Add(rr);
        }

        if (!dryRun) await db.SaveChangesAsync();
        return report;
    }


    private static string[] SplitCsv(string line)
    {
        var result = new List<string>();
        var cur = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"')
            {
                // Handle escaped quotes ("") inside a quoted string
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    cur.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(cur.ToString());
                cur.Clear();
            }
            else
            {
                cur.Append(c);
            }
        }
        result.Add(cur.ToString());
        return result.ToArray();
    }

    // --- Helpers ---
    private static string? Get(string[] a, int i) => i < a.Length ? a[i] : null;

    private static decimal? ParseDecimal(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) ? d : null;
    }

    private static DateOnly? ParseDate(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        // Allows yyyy-MM-dd
        return DateOnly.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    private static bool ParseBool(string? s) =>
        s?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
}