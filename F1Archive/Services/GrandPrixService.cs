using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;

namespace F1Archive.Services
{
    // Картка Гран-прі у списку
    public class GrandPrixItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
        public int Year { get; set; }
        public int Round { get; set; }
        public int CircuitId { get; set; }
        public string CircuitName { get; set; } = "";
        public string CircuitCountry { get; set; } = "";

        // Кілька імен через " / ", якщо перемогу поділено
        public string WinnerName { get; set; } = "—";

        public string RoundText => "ЕТАП " + Round.ToString("00");
        public string DateText => UiFormat.Date(Date);
        public string CircuitText => $"{CircuitName} · {CircuitCountry}";
    }

    // Рядок таблиці результатів гонки
    public class RaceRow
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public int TeamId { get; set; }
        public string TeamName { get; set; } = "";
        public int? CarNumber { get; set; }
        public int? StartPosition { get; set; }
        public int? FinishPosition { get; set; }
        public int? LapsCompleted { get; set; }
        public double Points { get; set; }
        public string Status { get; set; } = "";
        public string TimeOrRetired { get; set; } = "";
        public bool IsShared { get; set; }

        public bool IsWinner => FinishPosition == 1;
        public string CarNumberText => CarNumber != null ? "№ " + CarNumber : "";
        public string GridText => StartPosition == null ? "—" : StartPosition == 0 ? "Піт-лейн" : StartPosition.ToString()!;
        public string PointsText => UiFormat.Points(Points);

        public string StatusText => Status == "" ? "—" : Status;
        public string TimeText => TimeOrRetired == "" ? "—" : TimeOrRetired;

        // Від цього залежить колір статусу в таблиці
        public string StatusKind => ResultStatus.IsFinished(Status) ? "finished"
            : ResultStatus.IsDisqualified(Status) ? "dsq"
            : "retired";

        // Скільки позицій виграв (▲) чи втратив (▼) від старту до фінішу
        public string ChangeText
        {
            get
            {
                if (!ResultStatus.IsFinished(Status) || FinishPosition == null || !(StartPosition > 0))
                {
                    return "";
                }

                int change = StartPosition.Value - FinishPosition.Value;
                return change > 0 ? "▲ " + change : change < 0 ? "▼ " + (-change) : "=";
            }
        }

        public string ChangeKind => ChangeText.StartsWith("▲") ? "up" : ChangeText.StartsWith("▼") ? "down" : "";
    }

    // Піт-стопи одного гонщика в гонці
    public class PitRow
    {
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public int Count { get; set; }
        public string Laps { get; set; } = "";
        public string Best { get; set; } = "—";
    }

    public static class GrandPrixService
    {
        public static async Task<List<GrandPrixItem>> GetAllAsync()
        {
            using var db = new F1ArchiveContext();

            // Гран-прі від нових до старих
            List<GrandPrixItem> races = await db.GrandPrix
                .OrderByDescending(g => g.Date)
                .Select(g => new GrandPrixItem
                {
                    Id = g.Id,
                    Name = g.Name,
                    Date = g.Date,
                    Year = g.Season.Year,
                    Round = g.RoundNumber,
                    CircuitId = g.CircuitId,
                    CircuitName = g.Circuit.Name,
                    CircuitCountry = g.Circuit.Country
                })
                .ToListAsync();

            // Переможці гонок. У ранніх сезонах двоє гонщиків могли ділити одну перемогу,
            // тоді імена пишуться через " / "
            var winners = await db.Results
                .Where(r => r.FinishPosition == 1)
                .OrderBy(r => r.Id)
                .Select(r => new { r.GrandPrixId, Name = r.Driver.FirstName + " " + r.Driver.LastName })
                .ToListAsync();

            foreach (GrandPrixItem race in races)
            {
                List<string> names = winners
                    .Where(w => w.GrandPrixId == race.Id)
                    .Select(w => w.Name)
                    .Distinct()
                    .ToList();

                if (names.Count > 0)
                {
                    race.WinnerName = string.Join(" / ", names);
                }
            }

            return races;
        }

        public static async Task<GrandPrix?> GetProfileAsync(int id)
        {
            using var db = new F1ArchiveContext();

            return await db.GrandPrix
                .Include(g => g.Season)
                .Include(g => g.Circuit)
                .Include(g => g.PoleDriver)
                .Include(g => g.FastestLapDriver)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        // Попередній і наступний етапи того самого сезону
        public static async Task<(Link? Previous, Link? Next)> GetNeighboursAsync(GrandPrix race)
        {
            using var db = new F1ArchiveContext();

            List<Link> races = await db.GrandPrix
                .Where(g => g.SeasonId == race.SeasonId)
                .OrderBy(g => g.RoundNumber)
                .Select(g => new Link(g.Id, g.Name))
                .ToListAsync();

            int index = races.FindIndex(r => r.Id == race.Id);
            Link? previous = index > 0 ? races[index - 1] : null;
            Link? next = index < races.Count - 1 ? races[index + 1] : null;

            return (previous, next);
        }

        public static async Task<List<RaceRow>> GetResultsAsync(int id)
        {
            using var db = new F1ArchiveContext();

            // Хто не має місця (знявся, не стартував), той унизу
            return await db.Results
                .Where(r => r.GrandPrixId == id)
                .OrderBy(r => r.FinishPosition == null)
                .ThenBy(r => r.FinishPosition)
                .ThenBy(r => r.Id)
                .Select(r => new RaceRow
                {
                    DriverId = r.DriverId,
                    DriverName = r.Driver.FirstName + " " + r.Driver.LastName,
                    TeamId = r.TeamId,
                    TeamName = r.Team.Name,
                    CarNumber = r.CarNumber,
                    StartPosition = r.StartPosition,
                    FinishPosition = r.FinishPosition,
                    LapsCompleted = r.LapsCompleted,
                    Points = r.Points,
                    Status = r.Status,
                    TimeOrRetired = r.TimeOrRetired,
                    IsShared = r.IsSharedDrive
                })
                .ToListAsync();
        }

        public static async Task<List<QualifyingResult>> GetQualifyingAsync(int id)
        {
            using var db = new F1ArchiveContext();

            return await db.QualifyingResults
                .Include(q => q.Driver)
                .Include(q => q.Team)
                .Where(q => q.GrandPrixId == id)
                .OrderBy(q => q.Position == null)
                .ThenBy(q => q.Position)
                .ToListAsync();
        }

        public static async Task<List<PitRow>> GetPitStopsAsync(int id)
        {
            using var db = new F1ArchiveContext();

            List<PitStop> stops = await db.PitStops
                .Include(p => p.Driver)
                .Where(p => p.GrandPrixId == id)
                .ToListAsync();

            var rows = new List<PitRow>();

            foreach (var driver in stops.GroupBy(p => p.DriverId))
            {
                PitStop? best = driver
                    .Where(p => p.DurationMs != null)
                    .OrderBy(p => p.DurationMs)
                    .FirstOrDefault();

                rows.Add(new PitRow
                {
                    DriverId = driver.Key,
                    DriverName = driver.First().Driver.FullName,
                    Count = driver.Count(),
                    Laps = string.Join(", ", driver.OrderBy(p => p.StopNumber).Select(p => p.Lap)),
                    Best = best == null ? "—" : best.Duration + " с"
                });
            }

            return rows;
        }
    }
}
