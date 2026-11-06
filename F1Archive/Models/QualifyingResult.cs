namespace F1Archive.Models
{
    public class QualifyingResult
    {
        public int Id { get; set; }
        public int? Position { get; set; }
        // Час кожної сесії, наприклад "1:29.708" (Q2 і Q3 є з 2006 року)
        public string? Q1 { get; set; }
        public string? Q2 { get; set; }
        public string? Q3 { get; set; }

        public int GrandPrixId { get; set; }
        public int DriverId { get; set; }
        public int TeamId { get; set; }
        public GrandPrix GrandPrix { get; set; } = null!;
        public Driver Driver { get; set; } = null!;
        public Team Team { get; set; } = null!;
    }
}
