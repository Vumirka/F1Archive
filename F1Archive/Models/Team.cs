namespace F1Archive.Models
{
    // Команда (конструктор)
    public class Team
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string? LogoPath { get; set; }

        public List<Car> Cars { get; set; } = new();
        public List<Result> Results { get; set; } = new();
    }
}
