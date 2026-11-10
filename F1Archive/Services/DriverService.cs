using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Гонщик у списку і на своїй сторінці
    public class DriverInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public string Country { get; set; } = "";
        public string? ImagePath { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? PermanentNumber { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Poles { get; set; }
        public int Titles { get; set; }
        public int? DebutYear { get; set; }
        public int? LastYear { get; set; }

        // Ім'я дрібно, прізвище великими літерами (як на табло)
        public string FirstName => FullName.Contains(' ') ? FullName[..FullName.LastIndexOf(' ')] : "";
        public string LastName => FullName[(FullName.LastIndexOf(' ') + 1)..].ToUpper();
        public string YearsText => UiFormat.Years(DebutYear, LastYear);
        public bool IsChampion => Titles > 0;
        public string ChampionBadge => Titles == 1 ? "Чемпіон світу" : $"{Titles}× чемпіон світу";
    }

    // Один сезон кар'єри
    public class DriverSeason
    {
        public int Year { get; set; }
        public List<Link> Teams { get; set; } = new();
        public int? Position { get; set; }
        public double Points { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Poles { get; set; }
        public string RaceNumbers { get; set; } = "";

        public string PositionText => Position?.ToString() ?? "—";
        public bool IsChampion => Position == 1;
        public string PointsText => UiFormat.Points(Points);
    }

    // Виступи гонщика за одну команду
    public class DriverTeam
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = "";
        public string? LogoPath { get; set; }
        public int FirstYear { get; set; }
        public int LastYear { get; set; }
        public int SeasonsCount { get; set; }
        public int Starts { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public double Points { get; set; }

        // Команда, з якою в гонщика найбільше перемог
        public bool IsMostSuccessful { get; set; }

        public string YearsText => UiFormat.Years(FirstYear, LastYear);
        public string SeasonsText => UiFormat.SeasonsText(SeasonsCount);
        public string StatsText => $"Старти {Starts} · Перемоги {Wins} · Подіуми {Podiums} · Очки {UiFormat.Points(Points)}";
    }

    public class DriverCareer
    {
        public List<DriverSeason> Seasons { get; set; } = new();
        public List<DriverTeam> Teams { get; set; } = new();
        public int Starts { get; set; }
        public int? BestPosition { get; set; }
    }

    public static class DriverService
    {
        // Гонщики з цифрами, порахованими з результатів гонок
        private static IQueryable<DriverInfo> Query(F1ArchiveContext db)
        {
            return db.Drivers.Select(d => new DriverInfo
            {
                Id = d.Id,
                FullName = d.FirstName + " " + d.LastName,
                Country = d.Country,
                ImagePath = d.ImagePath,
                BirthDate = d.BirthDate,
                PermanentNumber = d.PermanentNumber,
                Wins = d.Results.Count(r => r.FinishPosition == 1),
                Podiums = d.Results.Where(r => r.FinishPosition <= 3).Select(r => r.GrandPrixId).Distinct().Count(),
                Poles = db.GrandPrix.Count(g => g.PoleDriverId == d.Id),
                Titles = db.Seasons.Count(s => s.ChampionDriverId == d.Id),
                DebutYear = d.Results.Min(r => (int?)r.GrandPrix.Season.Year),
                LastYear = d.Results.Max(r => (int?)r.GrandPrix.Season.Year)
            });
        }

        public static async Task<List<DriverInfo>> GetAllAsync()
        {
            using var db = new F1ArchiveContext();
            return await Query(db).OrderBy(d => d.Id).ToListAsync();
        }

        public static async Task<DriverInfo?> GetProfileAsync(int id)
        {
            using var db = new F1ArchiveContext();
            DriverInfo? driver = await Query(db).FirstOrDefaultAsync(d => d.Id == id);

            // Постійні номери з'явилися у 2014 році
            if (driver != null && (driver.LastYear ?? 0) < 2014)
            {
                driver.PermanentNumber = null;
            }

            return driver;
        }

        public static async Task<DriverCareer> GetCareerAsync(int id)
        {
            using var db = new F1ArchiveContext();

            var results = await db.Results
                .Where(r => r.DriverId == id)
                .Select(r => new
                {
                    r.GrandPrix.Season.Year,
                    r.TeamId,
                    TeamName = r.Team.Name,
                    TeamLogo = r.Team.LogoPath,
                    r.FinishPosition,
                    r.Points,
                    r.Status,
                    r.CarNumber
                })
                .ToListAsync();

            var career = new DriverCareer();

            foreach (var season in results.GroupBy(r => r.Year).OrderBy(g => g.Key))
            {
                // Місце і очки беремо із заліку сезону
                List<StandingRow> table = await SeasonService.GetStandingsAsync(season.Key);
                StandingRow standing = table.First(s => s.DriverId == id);

                string numbers = string.Join(", ", season
                    .Where(r => r.CarNumber != null)
                    .Select(r => r.CarNumber)
                    .Distinct()
                    .OrderBy(n => n));

                career.Seasons.Add(new DriverSeason
                {
                    Year = season.Key,
                    Teams = season.Select(r => new Link(r.TeamId, r.TeamName)).Distinct().ToList(),
                    Wins = standing.Wins,
                    Podiums = standing.Podiums,
                    Poles = standing.Poles,
                    Position = standing.Position,
                    Points = standing.Points,
                    RaceNumbers = numbers == "" ? "—" : numbers
                });
            }

            foreach (var team in results.GroupBy(r => r.TeamId))
            {
                career.Teams.Add(new DriverTeam
                {
                    TeamId = team.Key,
                    Name = team.First().TeamName,
                    LogoPath = team.First().TeamLogo,
                    FirstYear = team.Min(r => r.Year),
                    LastYear = team.Max(r => r.Year),
                    SeasonsCount = team.Select(r => r.Year).Distinct().Count(),
                    Starts = team.Count(r => ResultStatus.IsStart(r.Status)),
                    Wins = team.Count(r => r.FinishPosition == 1),
                    Podiums = team.Count(r => r.FinishPosition <= 3),
                    Points = team.Sum(r => r.Points)
                });
            }

            DriverTeam? best = career.Teams
                .Where(t => t.Wins > 0)
                .OrderByDescending(t => t.Wins)
                .FirstOrDefault();

            if (best != null)
            {
                best.IsMostSuccessful = true;
            }

            career.Starts = results.Count(r => ResultStatus.IsStart(r.Status));
            career.BestPosition = career.Seasons.Min(s => s.Position);

            return career;
        }
    }
}
