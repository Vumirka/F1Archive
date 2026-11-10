using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Картка сезону на сторінці "Сезони"
    public class SeasonItem
    {
        public int Year { get; set; }
        public int RoundsCount { get; set; }
        public int? ChampionDriverId { get; set; }
        public string? DriverName { get; set; }
        public string? ChampionPhoto { get; set; }
        public int? ChampionTeamId { get; set; }
        public string? TeamName { get; set; }

        public string RoundsText => RoundsCount + " Гран-прі";

        // Сезон цього року без чемпіона ще триває
        public bool InProgress => DriverName == null && Year == DateTime.Now.Year;

        public string ChampionDriver => DriverName ?? (InProgress ? "Сезон триває" : "—");

        // Кубок конструкторів розігрується з 1958 року
        public string ChampionTeam => Year < 1958
            ? "Кубок ще не проводився"
            : TeamName ?? (InProgress ? "Сезон триває" : "—");
    }

    // Рядок особистого заліку
    public class StandingRow
    {
        public int Position { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string Country { get; set; } = "";
        public string Teams { get; set; } = "";
        public double Points { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Poles { get; set; }

        public string PointsText => UiFormat.Points(Points);
        public bool IsLeader => Position == 1;
    }

    // Команда сезону з її гонщиками
    public class SeasonTeam
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public string Country { get; set; } = "";
        public string? LogoPath { get; set; }
        public string Drivers { get; set; } = "";
    }

    public static class SeasonService
    {
        public static async Task<List<SeasonItem>> GetAllAsync()
        {
            using var db = new F1ArchiveContext();

            return await db.Seasons
                .OrderByDescending(s => s.Year)
                .Select(s => new SeasonItem
                {
                    Year = s.Year,
                    RoundsCount = s.RoundsCount,
                    ChampionDriverId = s.ChampionDriverId,
                    DriverName = s.ChampionDriver != null ? s.ChampionDriver.FirstName + " " + s.ChampionDriver.LastName : null,
                    ChampionPhoto = s.ChampionDriver != null ? s.ChampionDriver.ImagePath : null,
                    ChampionTeamId = s.ChampionTeamId,
                    TeamName = s.ChampionTeam != null ? s.ChampionTeam.Name : null
                })
                .ToListAsync();
        }

        // Календар сезону: Гран-прі цього року по порядку етапів
        public static async Task<List<GrandPrixItem>> GetCalendarAsync(int year)
        {
            List<GrandPrixItem> races = await GrandPrixService.GetAllAsync();

            return races
                .Where(r => r.Year == year)
                .OrderBy(r => r.Round)
                .ToList();
        }

        // Особистий залік сезону, порахований з результатів гонок.
        // Чемпіон завжди перший (у старих сезонах враховувались не всі гонки),
        // далі за очками, перемогами і подіумами.
        public static async Task<List<StandingRow>> GetStandingsAsync(int year)
        {
            using var db = new F1ArchiveContext();

            int? championId = await db.Seasons
                .Where(s => s.Year == year)
                .Select(s => s.ChampionDriverId)
                .FirstOrDefaultAsync();

            var results = await db.Results
                .Where(r => r.GrandPrix.Season.Year == year)
                .Select(r => new
                {
                    r.DriverId,
                    DriverName = r.Driver.FirstName + " " + r.Driver.LastName,
                    r.Driver.Country,
                    TeamName = r.Team.Name,
                    r.GrandPrixId,
                    r.FinishPosition,
                    r.Points
                })
                .ToListAsync();

            List<int?> poles = await db.GrandPrix
                .Where(g => g.Season.Year == year)
                .Select(g => g.PoleDriverId)
                .ToListAsync();

            List<StandingRow> rows = results
                .GroupBy(r => r.DriverId)
                .Select(g => new StandingRow
                {
                    DriverId = g.Key,
                    DriverName = g.First().DriverName,
                    Country = g.First().Country,
                    Teams = string.Join(", ", g.Select(r => r.TeamName).Distinct()),
                    Points = g.Sum(r => r.Points),
                    Wins = g.Count(r => r.FinishPosition == 1),
                    Podiums = g.Where(r => r.FinishPosition <= 3).Select(r => r.GrandPrixId).Distinct().Count(),
                    Poles = poles.Count(p => p == g.Key)
                })
                .OrderByDescending(r => r.DriverId == championId)
                .ThenByDescending(r => r.Points)
                .ThenByDescending(r => r.Wins)
                .ThenByDescending(r => r.Podiums)
                .ToList();

            for (int i = 0; i < rows.Count; i++)
            {
                rows[i].Position = i + 1;
            }

            return rows;
        }

        // Команди сезону та їхні гонщики (з результатів гонок)
        public static async Task<List<SeasonTeam>> GetTeamsAsync(int year)
        {
            using var db = new F1ArchiveContext();

            var rows = await db.Results
                .Where(r => r.GrandPrix.Season.Year == year)
                .Select(r => new
                {
                    r.TeamId,
                    r.Team.Name,
                    r.Team.Country,
                    r.Team.LogoPath,
                    DriverName = r.Driver.FirstName + " " + r.Driver.LastName
                })
                .ToListAsync();

            return rows
                .GroupBy(x => x.TeamId)
                .Select(g => new SeasonTeam
                {
                    TeamId = g.Key,
                    TeamName = g.First().Name,
                    Country = g.First().Country,
                    LogoPath = g.First().LogoPath,
                    Drivers = string.Join(" · ", g.Select(x => x.DriverName).Distinct())
                })
                .ToList();
        }
    }
}
