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

    // Сезон з чемпіонами і цифрами для шапки
    public class SeasonInfo
    {
        public Season Season { get; set; } = null!;
        public int GrandPrixCount { get; set; }
        public int DriversCount { get; set; }
        public int TeamsCount { get; set; }
        public int CircuitsCount { get; set; }
        public int WinnersCount { get; set; }
    }

    // Етап у календарі сезону
    public class CalendarRow
    {
        public int GrandPrixId { get; set; }
        public int Round { get; set; }
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
        public string CircuitName { get; set; } = "";
        public string CircuitCountry { get; set; } = "";

        // Кілька імен через " / ", якщо перемогу поділено
        public string WinnerName { get; set; } = "—";

        public string RoundText => "ЕТАП " + UiFormat.Round(Round);
        public string DateText => UiFormat.Date(Date);
        public string CircuitText => $"{CircuitName} · {CircuitCountry}";
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

        public static async Task<SeasonInfo?> GetOverviewAsync(int year)
        {
            using var db = new F1ArchiveContext();

            Season? season = await db.Seasons
                .Include(s => s.ChampionDriver)
                .Include(s => s.ChampionTeam)
                .FirstOrDefaultAsync(s => s.Year == year);

            if (season == null)
            {
                return null;
            }

            var races = db.GrandPrix.Where(g => g.SeasonId == season.Id);
            var results = db.Results.Where(r => r.GrandPrix.SeasonId == season.Id);

            return new SeasonInfo
            {
                Season = season,
                GrandPrixCount = await races.CountAsync(),
                CircuitsCount = await races.Select(g => g.CircuitId).Distinct().CountAsync(),
                DriversCount = await results.Select(r => r.DriverId).Distinct().CountAsync(),
                TeamsCount = await results.Select(r => r.TeamId).Distinct().CountAsync(),
                WinnersCount = await results
                    .Where(r => r.FinishPosition == 1)
                    .Select(r => r.DriverId)
                    .Distinct()
                    .CountAsync()
            };
        }

        public static async Task<List<CalendarRow>> GetCalendarAsync(int year)
        {
            using var db = new F1ArchiveContext();

            List<CalendarRow> races = await db.GrandPrix
                .Where(g => g.Season.Year == year)
                .OrderBy(g => g.RoundNumber)
                .Select(g => new CalendarRow
                {
                    GrandPrixId = g.Id,
                    Round = g.RoundNumber,
                    Name = g.Name,
                    Date = g.Date,
                    CircuitName = g.Circuit.Name,
                    CircuitCountry = g.Circuit.Country
                })
                .ToListAsync();

            var winners = await GrandPrixService.LoadWinnersAsync(db.Results.Where(r => r.GrandPrix.Season.Year == year));

            foreach (CalendarRow race in races)
            {
                if (winners.TryGetValue(race.GrandPrixId, out var winner))
                {
                    race.WinnerName = winner.Names;
                }
            }

            return races;
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
                    Drivers = string.Join(" · ", g.Select(x => x.DriverName).Distinct().OrderBy(n => n))
                })
                .OrderBy(t => t.TeamName)
                .ToList();
        }
    }
}
