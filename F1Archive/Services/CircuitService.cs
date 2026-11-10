using F1Archive.Database;
using F1Archive.Helpers;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Траса у списку і на своїй сторінці
    public class CircuitInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public double? LengthKm { get; set; }
        public int? Turns { get; set; }
        public string? LayoutImagePath { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? FirstYear { get; set; }
        public int? LastYear { get; set; }
        public int GrandPrixCount { get; set; }

        // Траса є в останньому сезоні архіву
        public bool IsActive { get; set; }

        public string LocationText => City == "" ? Country : $"{City} · {Country}";
        public string CountryCode => UiFormat.CountryCode(Country);
        public bool HasCountryCode => CountryCode != "";
        public string LengthText => UiFormat.Length(LengthKm);
    }

    public static class CircuitService
    {
        public static async Task<List<CircuitInfo>> GetAllAsync()
        {
            using var db = new F1ArchiveContext();

            List<CircuitInfo> circuits = await db.Circuits
                .Select(c => new CircuitInfo
                {
                    Id = c.Id,
                    Name = c.Name,
                    Country = c.Country,
                    City = c.City,
                    LengthKm = c.LengthKm,
                    Turns = c.Turns,
                    LayoutImagePath = c.LayoutImagePath,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude
                })
                .ToListAsync();

            var races = await db.GrandPrix
                .Select(g => new { g.CircuitId, g.Season.Year })
                .ToListAsync();

            int lastYear = await db.Seasons.MaxAsync(s => s.Year);

            foreach (CircuitInfo circuit in circuits)
            {
                List<int> years = races
                    .Where(r => r.CircuitId == circuit.Id)
                    .Select(r => r.Year)
                    .ToList();

                circuit.GrandPrixCount = years.Count;

                if (years.Count > 0)
                {
                    circuit.FirstYear = years.Min();
                    circuit.LastYear = years.Max();
                    circuit.IsActive = circuit.LastYear == lastYear;
                }
            }

            return circuits;
        }

        public static async Task<CircuitInfo?> GetProfileAsync(int id)
        {
            List<CircuitInfo> circuits = await GetAllAsync();
            return circuits.FirstOrDefault(c => c.Id == id);
        }

        // Усі Гран-прі на трасі з переможцями (список Гран-прі вже йде від нових до старих)
        public static async Task<List<GrandPrixItem>> GetHistoryAsync(int id)
        {
            List<GrandPrixItem> races = await GrandPrixService.GetAllAsync();
            return races.Where(r => r.CircuitId == id).ToList();
        }
    }
}
