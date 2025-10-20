using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collection.Domain
{
    public enum  ItemKind { Game, Console, Controller, Peripheral, Other }

    public class Item
    {
        public int Id { get; set; }
        public string UserId { get; set; } = "demo"; 
        public string Title { get; set; } = "";
        public int PlatformId { get; set; }
        public string Region { get; set; } = "NTSC-U";
        public Condition Condition { get; set; } = Condition.Good;

        public bool HasBox { get; set; }
        public bool HasManual { get; set; }
        public bool IsCib => HasBox && HasManual;

        public decimal? PurchasePrice { get; set; }
        public DateOnly? PurchaseDate { get; set; }

        public decimal? EstimatedValue { get; set; }

        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public Platform? Platform { get; set; }

        public string? Publisher { get; set; }
        public string? Developer { get; set; }
        public int? ReleaseYear { get; set; }
        public string? Genre { get; set; }
        public string? Barcode { get; set; }

        public ItemKind? Kind { get; set; }
    }
}
