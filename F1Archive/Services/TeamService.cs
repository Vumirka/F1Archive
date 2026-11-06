using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Команда у списку і на своїй сторінці
    public class TeamInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string? LogoPath { get; set; }
        public int Wins { get; set; }
        public int Titles { get; set; }
        public int? FirstSeason { get; set; }
        public int? LastSeason { get; set; }
        public int SeasonsCount { get; set; }

        // Команда є в останньому сезоні архіву
        public bool IsActive { get; set; }

        public string YearsText => UiFormat.Years(FirstSeason, LastSeason);
        public bool IsChampion => Titles > 0;
        public string ChampionBadge => Titles == 1 ? "Чемпіон-конструктор" : $"{Titles}× Кубок конструкторів";
    }

    // Період участі без пропущених сезонів
    public class TeamPeriod
    {
        public int FromYear { get; set; }
        public int ToYear { get; set; }
        public int SeasonsCount { get; set; }
        public int Wins { get; set; }
        public int Titles { get; set; }

        public string RangeText => UiFormat.Years(FromYear, ToYear);
        public string Details => UiFormat.CountText(SeasonsCount, "сезон", "сезони", "сезонів") + " · Перемоги " + Wins
            + (Titles > 0 ? " · Кубків конструкторів " + Titles : "");
    }

    // Рядок сезону на сторінці команди
    public class TeamSeason
    {
        public int Year { get; set; }
        public List<Link> Drivers { get; set; } = new();
        public int GrandPrix { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public bool IsChampion { get; set; }

        // Кубок конструкторів розігрується з 1958 року
        public string StatusText => IsChampion ? "Кубок конструкторів" : Year >= 1958 ? "—" : "Кубок ще не проводився";
    }

    // Гонщик команди (цифри лише за гонки в цій команді)
    public class TeamDriver
    {
        public int DriverId { get; set; }
        public string Name { get; set; } = "";
        public string? ImagePath { get; set; }
        public int FirstYear { get; set; }
        public int LastYear { get; set; }
        public int Seasons { get; set; }
        public int Wins { get; set; }
        public int Podiums { get; set; }
        public int Titles { get; set; }

        public string YearsText => UiFormat.Years(FirstYear, LastYear) + " · " + UiFormat.CountText(Seasons, "сезон", "сезони", "сезонів");
        public string StatsText => $"Перемоги {Wins} · Подіуми {Podiums}";
        public bool HasTitles => Titles > 0;
        public string TitlesText => Titles == 1 ? "Чемпіон світу з командою" : $"{Titles}× чемпіон світу з командою";
    }

    public class TeamCareer
    {
        public List<TeamPeriod> Periods { get; set; } = new();
        public List<TeamSeason> Seasons { get; set; } = new();
        public List<int> TitleYears { get; set; } = new();
        public List<TeamDriver> Drivers { get; set; } = new();
        public int GrandPrix { get; set; }
        public int Podiums { get; set; }
        public int Poles { get; set; }
    }

    public static class TeamService
    {
        private record TeamRow(int GrandPrixId, int Year, int? PoleDriverId, int DriverId, int? FinishPosition);

        public static async Task<List<TeamInfo>> GetAllAsync()
        {
            using var db = new F1ArchiveContext();

            List<TeamInfo> teams = await db.Teams
                .Select(t => new TeamInfo
                {
                    Id = t.Id,
                    Name = t.Name,
                    Country = t.Country,
                    LogoPath = t.LogoPath,
                    Wins = t.Results.Where(r => r.FinishPosition == 1).Select(r => r.GrandPrixId).Distinct().Count(),
                    Titles = db.Seasons.Count(s => s.ChampionTeamId == t.Id)
                })
                .ToListAsync();

            // У які роки команда виступала
            var teamYears = await db.Results
                .Select(r => new { r.TeamId, r.GrandPrix.Season.Year })
                .Distinct()
                .ToListAsync();

            int lastYear = await db.Seasons.MaxAsync(s => s.Year);

            foreach (TeamInfo team in teams)
            {
                List<int> years = teamYears.Where(x => x.TeamId == team.Id).Select(x => x.Year).ToList();

                if (years.Count > 0)
                {
                    team.FirstSeason = years.Min();
                    team.LastSeason = years.Max();
                }

                team.SeasonsCount = years.Count;
                team.IsActive = years.Contains(lastYear);
            }

            return teams;
        }

        public static async Task<TeamInfo?> GetProfileAsync(int id)
        {
            List<TeamInfo> teams = await GetAllAsync();
            return teams.FirstOrDefault(t => t.Id == id);
        }

        public static async Task<TeamCareer> GetCareerAsync(int id)
        {
            using var db = new F1ArchiveContext();

            List<TeamRow> rows = await db.Results
                .Where(r => r.TeamId == id)
                .Select(r => new TeamRow(r.GrandPrixId, r.GrandPrix.Season.Year, r.GrandPrix.PoleDriverId,
                    r.DriverId, r.FinishPosition))
                .ToListAsync();

            // Хто за команду виступав у якому році (з результатів гонок)
            var lineups = await db.Results
                .Where(r => r.TeamId == id)
                .Select(r => new
                {
                    r.DriverId,
                    r.GrandPrix.Season.Year,
                    Name = r.Driver.FirstName + " " + r.Driver.LastName,
                    r.Driver.ImagePath
                })
                .Distinct()
                .ToListAsync();

            var seasons = await db.Seasons
                .Select(s => new { s.Year, s.ChampionTeamId, s.ChampionDriverId })
                .ToListAsync();

            var career = new TeamCareer();

            career.TitleYears = seasons
                .Where(s => s.ChampionTeamId == id)
                .Select(s => s.Year)
                .OrderBy(y => y)
                .ToList();

            List<int> years = lineups.Select(x => x.Year).Distinct().OrderBy(y => y).ToList();

            foreach (int year in years)
            {
                var yearRows = rows.Where(r => r.Year == year).ToList();

                career.Seasons.Add(new TeamSeason
                {
                    Year = year,
                    Drivers = lineups
                        .Where(x => x.Year == year)
                        .Select(x => new Link(x.DriverId, x.Name))
                        .OrderBy(d => d.Name)
                        .ToList(),
                    GrandPrix = yearRows.Select(r => r.GrandPrixId).Distinct().Count(),
                    Wins = CountWins(yearRows),
                    Podiums = CountPodiums(yearRows),
                    IsChampion = career.TitleYears.Contains(year)
                });
            }

            // Роки поспіль складають один період участі
            TeamPeriod? period = null;

            foreach (TeamSeason season in career.Seasons)
            {
                if (period == null || season.Year > period.ToYear + 1)
                {
                    period = new TeamPeriod { FromYear = season.Year };
                    career.Periods.Add(period);
                }

                period.ToYear = season.Year;
                period.SeasonsCount++;
                period.Wins += season.Wins;

                if (season.IsChampion)
                {
                    period.Titles++;
                }
            }

            foreach (var driver in lineups.GroupBy(x => x.DriverId))
            {
                List<int> driverYears = driver.Select(x => x.Year).Distinct().ToList();
                var driverRows = rows.Where(r => r.DriverId == driver.Key).ToList();

                career.Drivers.Add(new TeamDriver
                {
                    DriverId = driver.Key,
                    Name = driver.First().Name,
                    ImagePath = driver.First().ImagePath,
                    FirstYear = driverYears.Min(),
                    LastYear = driverYears.Max(),
                    Seasons = driverYears.Count,
                    Wins = CountWins(driverRows),
                    Podiums = CountPodiums(driverRows),
                    Titles = seasons.Count(s => s.ChampionDriverId == driver.Key && driverYears.Contains(s.Year))
                });
            }

            career.GrandPrix = rows.Select(r => r.GrandPrixId).Distinct().Count();
            career.Podiums = CountPodiums(rows);
            career.Poles = rows.Where(r => r.PoleDriverId == r.DriverId).Select(r => r.GrandPrixId).Distinct().Count();

            return career;
        }

        public static async Task<List<Car>> GetCarsAsync(int id)
        {
            using var db = new F1ArchiveContext();

            return await db.Cars
                .Include(c => c.Season)
                .Where(c => c.TeamId == id)
                .OrderBy(c => c.Season.Year)
                .ToListAsync();
        }

        // Перемога команди - це гонка, яку виграв хоч один її гонщик
        private static int CountWins(List<TeamRow> rows)
        {
            return rows.Where(r => r.FinishPosition == 1).Select(r => r.GrandPrixId).Distinct().Count();
        }

        // Два боліди на подіумі однієї гонки - це два подіуми
        private static int CountPodiums(List<TeamRow> rows)
        {
            return rows.Where(r => r.FinishPosition <= 3).Select(r => (r.GrandPrixId, r.FinishPosition)).Distinct().Count();
        }
    }
}
