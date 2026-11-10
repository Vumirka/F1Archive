using F1Archive.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace F1Archive.Database
{
    public class F1ArchiveContext : DbContext
    {
        // Папка Database з файлом бази і фото (Media).
        // Програма запускається з bin\Debug\..., тому папку шукаємо вище.
        public static string DataFolder { get; } = FindFolder();

        public static string DatabasePath
        {
            get { return Path.Combine(DataFolder, "F1Archive.db"); }
        }

        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<Driver> Drivers => Set<Driver>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<Circuit> Circuits => Set<Circuit>();
        public DbSet<GrandPrix> GrandPrix => Set<GrandPrix>();
        public DbSet<Result> Results => Set<Result>();
        public DbSet<QualifyingResult> QualifyingResults => Set<QualifyingResult>();
        public DbSet<PitStop> PitStops => Set<PitStop>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={DatabasePath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Запис не можна видалити, поки на нього посилаються інші записи
            foreach (var key in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                if (key.DeleteBehavior == DeleteBehavior.Cascade)
                {
                    key.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }

            // Якщо гонщика видалили, поул і найшвидше коло в Гран-прі стають порожніми
            modelBuilder.Entity<GrandPrix>()
                .HasOne(g => g.PoleDriver)
                .WithMany()
                .HasForeignKey(g => g.PoleDriverId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<GrandPrix>()
                .HasOne(g => g.FastestLapDriver)
                .WithMany()
                .HasForeignKey(g => g.FastestLapDriverId)
                .OnDelete(DeleteBehavior.SetNull);

            // Значення, які не можуть повторюватися
            modelBuilder.Entity<Season>()
                .HasIndex(s => s.Year)
                .IsUnique();

            modelBuilder.Entity<GrandPrix>()
                .HasIndex(g => new { g.SeasonId, g.RoundNumber })
                .IsUnique();

            modelBuilder.Entity<QualifyingResult>()
                .HasIndex(q => new { q.GrandPrixId, q.DriverId })
                .IsUnique();

            modelBuilder.Entity<PitStop>()
                .HasIndex(p => new { p.GrandPrixId, p.DriverId, p.StopNumber })
                .IsUnique();
        }

        private static string FindFolder()
        {
            var folder = new DirectoryInfo(AppContext.BaseDirectory);

            while (folder != null)
            {
                string path = Path.Combine(folder.FullName, "Database");

                if (File.Exists(Path.Combine(path, "F1Archive.db")))
                {
                    return path;
                }

                folder = folder.Parent;
            }

            return Path.Combine(AppContext.BaseDirectory, "Database");
        }
    }
}
