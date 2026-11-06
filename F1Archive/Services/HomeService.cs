using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Скільки записів у кожній таблиці архіву
    public class ArchiveCounts
    {
        public int FirstYear { get; set; }
        public int LastYear { get; set; }
        public int Seasons { get; set; }
        public int GrandPrix { get; set; }
        public int Drivers { get; set; }
        public int Teams { get; set; }
        public int Circuits { get; set; }
    }

    // Рядок у списку пошуку. Group - назва групи, Page - яку сторінку відкрити.
    public class SearchHit
    {
        public string Group { get; set; } = "";
        public string Page { get; set; } = "";
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
    }

    public static class HomeService
    {
        public static async Task<ArchiveCounts> GetCountsAsync()
        {
            using var db = new F1ArchiveContext();

            return new ArchiveCounts
            {
                FirstYear = await db.Seasons.MinAsync(s => s.Year),
                LastYear = await db.Seasons.MaxAsync(s => s.Year),
                Seasons = await db.Seasons.CountAsync(),
                GrandPrix = await db.GrandPrix.CountAsync(),
                Drivers = await db.Drivers.CountAsync(),
                Teams = await db.Teams.CountAsync(),
                Circuits = await db.Circuits.CountAsync()
            };
        }

        // Останній сезон, у якому вже є чемпіон
        public static async Task<Season?> GetLatestSeasonAsync()
        {
            using var db = new F1ArchiveContext();

            return await db.Seasons
                .Include(s => s.ChampionDriver)
                .Include(s => s.ChampionTeam)
                .Where(s => s.ChampionDriverId != null)
                .OrderByDescending(s => s.Year)
                .FirstOrDefaultAsync();
        }

        // Пошук за частиною назви: по п'ять гонщиків, команд, Гран-прі і трас.
        // Записів небагато, тому беремо всі назви з бази і шукаємо в пам'яті
        // (так "лауда" знаходить "Лауда", а "raikkonen" - "Räikkönen").
        public static async Task<List<SearchHit>> SearchAsync(string text)
        {
            using var db = new F1ArchiveContext();

            List<SearchHit> drivers = await db.Drivers
                .OrderBy(d => d.LastName)
                .Select(d => new SearchHit
                {
                    Group = "Гонщики",
                    Page = PageKeys.DriverDetails,
                    Id = d.Id,
                    Title = d.FirstName + " " + d.LastName,
                    Subtitle = d.Country
                })
                .ToListAsync();

            List<SearchHit> teams = await db.Teams
                .OrderBy(t => t.Name)
                .Select(t => new SearchHit
                {
                    Group = "Команди",
                    Page = PageKeys.TeamDetails,
                    Id = t.Id,
                    Title = t.Name,
                    Subtitle = t.Country
                })
                .ToListAsync();

            List<SearchHit> races = await db.GrandPrix
                .OrderByDescending(g => g.Date)
                .Select(g => new SearchHit
                {
                    Group = "Гран-прі",
                    Page = PageKeys.GrandPrixDetails,
                    Id = g.Id,
                    Title = g.Name,
                    Subtitle = g.Season.Year.ToString() + " · етап " + g.RoundNumber.ToString()
                })
                .ToListAsync();

            List<SearchHit> circuits = await db.Circuits
                .OrderBy(c => c.Name)
                .Select(c => new SearchHit
                {
                    Group = "Траси",
                    Page = PageKeys.CircuitDetails,
                    Id = c.Id,
                    Title = c.Name,
                    Subtitle = c.City + ", " + c.Country
                })
                .ToListAsync();

            var hits = new List<SearchHit>();
            hits.AddRange(drivers.Where(h => ListHelper.Matches(text, h.Title)).Take(5));
            hits.AddRange(teams.Where(h => ListHelper.Matches(text, h.Title)).Take(5));
            hits.AddRange(races.Where(h => ListHelper.Matches(text, h.Title)).Take(5));
            hits.AddRange(circuits.Where(h => ListHelper.Matches(text, h.Title, h.Subtitle)).Take(5));

            return hits;
        }
    }
}
