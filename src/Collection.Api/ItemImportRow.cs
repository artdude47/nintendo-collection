namespace Collection.Api
{
    public class ItemImportRow
    {
        public int LineNumber { get; set; } // used for error reporting
        public string? Title { get; set; }
        public string? Platform { get; set; }
        public string? Region { get; set; }
        public string? Condition { get; set; }
        public string? HasBox { get; set; }
        public string? HasManual { get; set; }
        public string? PurchasePrice { get; set; }
        public string? PurchaseDate { get; set; }
        public string? EstimatedValue { get; set; }
        public string? Notes { get; set; }

    }
}
