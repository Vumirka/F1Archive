namespace F1Archive.Models
{
    public class PitStop
    {
        public int Id { get; set; }
        // Номер зупинки цього гонщика в гонці: 1, 2, 3...
        public int StopNumber { get; set; }
        public int Lap { get; set; }
        // Тривалість текстом ("22.456") і в мілісекундах для сортування
        public string Duration { get; set; } = "";
        public int? DurationMs { get; set; }

        public int GrandPrixId { get; set; }
        public int DriverId { get; set; }
        public GrandPrix GrandPrix { get; set; } = null!;
        public Driver Driver { get; set; } = null!;
    }
}
