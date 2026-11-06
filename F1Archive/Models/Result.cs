namespace F1Archive.Models
{
    public class Result
    {
        public int Id { get; set; }
        public int? StartPosition { get; set; }
        public int? FinishPosition { get; set; }
        public double Points { get; set; }
        public int? CarNumber { get; set; }
        public int? LapsCompleted { get; set; }
        public string Status { get; set; } = "";
        // Час гонки або причина сходу
        public string TimeOrRetired { get; set; } = "";
        // У ранніх сезонах двоє гонщиків могли ділити один болід і очки
        public bool IsSharedDrive { get; set; }

        public int GrandPrixId { get; set; }
        public int DriverId { get; set; }
        public int TeamId { get; set; }
        public GrandPrix GrandPrix { get; set; } = null!;
        public Driver Driver { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}
