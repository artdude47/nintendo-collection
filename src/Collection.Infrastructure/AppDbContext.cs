using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collection.Domain;
using Microsoft.EntityFrameworkCore;

namespace Collection.Infrastructure
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Item> Items => Set<Item>();    
        public DbSet<Platform> Platforms => Set<Platform>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<Item>().Property(x => x.Title).IsRequired().HasMaxLength(200);
            b.Entity<Item>().HasIndex(x => new { x.UserId, x.PlatformId, x.Title });

            b.Entity<Item>().Property(x => x.PurchasePrice).HasConversion<double?>();
            b.Entity<Item>().Property(x => x.EstimatedValue).HasConversion<double?>();

            // Seed platforms (so GET /platforms returns something on day 1)
            b.Entity<Platform>().HasData(
                new Platform { Id = 1, Name = "NES" },
                new Platform { Id = 2, Name = "SNES" },
                new Platform { Id = 3, Name = "N64" },
                new Platform { Id = 4, Name = "GameCube" },
                new Platform { Id = 5, Name = "Wii" },
                new Platform { Id = 6, Name = "Wii U" },
                new Platform { Id = 7, Name = "Switch" },
                new Platform { Id = 8, Name = "Game Boy" },
                new Platform { Id = 9, Name = "Game Boy Color" },
                new Platform { Id = 10, Name = "Game Boy Advance" },
                new Platform { Id = 11, Name = "Nintendo DS" },
                new Platform { Id = 12, Name = "Nintendo 3DS" },
                new Platform { Id = 13, Name = "Amiibo" }
            );
        }
    }
}
