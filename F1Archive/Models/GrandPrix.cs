namespace F1Archive.Models
{
    // Гран-прі (етап сезону)
    public class GrandPrix
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime Date { get; set; }
        public int RoundNumber { get; set; }
        public int? Laps { get; set; }

        public int SeasonId { get; set; }
        public int CircuitId { get; set; }
        public Season Season { get; set; } = null!;
        public Circuit Circuit { get; set; } = null!;

        // Поул і найшвидше коло (у старих сезонах можуть бути порожні)
        public int? PoleDriverId { get; set; }
        public int? FastestLapDriverId { get; set; }
        public string? FastestLapTime { get; set; }
        public int? FastestLapNumber { get; set; }
        public Driver? PoleDriver { get; set; }
        public Driver? FastestLapDriver { get; set; }

        public List<Result> Results { get; set; } = new();
        public List<QualifyingResult> QualifyingResults { get; set; } = new();
        public List<PitStop> PitStops { get; set; } = new();
    }
}
