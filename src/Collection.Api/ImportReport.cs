using System.ComponentModel;

namespace Collection.Api
{
    public class ImportReport
    {
        public bool DryRun { get; init; }
        public int RowsRead { get; set; }
        public int RowsInserted { get; set; }
        public List<RowResult> Rows { get; set; } = new();

        public class RowResult
        {
            public int LineNumber { get; init; }
            public bool Ok => Errors.Count == 0;
            public List<string> Errors { get; set; } = new();
            public string? Title { get; set; }
            public string? Platform { get; set; }
        }
    }
}
