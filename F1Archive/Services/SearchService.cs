using F1Archive.Database;
using F1Archive.Helpers;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Рядок у списку пошуку. Group - назва групи, Page - яку сторінку відкрити.
    public class SearchHit
    {
        public string Group { get; set; } = "";
        public string Page { get; set; } = "";
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Subtitle { get; set; } = "";
    }

    public static class SearchService
    {
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
