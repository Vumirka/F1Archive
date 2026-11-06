namespace F1Archive.Models
{
    // Болід команди в сезоні
    public class Car
    {
        public int Id { get; set; }
        public string Model { get; set; } = "";
        public string Engine { get; set; } = "";
        public string? Chassis { get; set; }

        public int TeamId { get; set; }
        public int SeasonId { get; set; }
        public Team Team { get; set; } = null!;
        public Season Season { get; set; } = null!;
    }
}
