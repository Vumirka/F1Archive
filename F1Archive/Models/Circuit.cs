namespace F1Archive.Models
{
    public class Circuit
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public double? LengthKm { get; set; }
        public int? Turns { get; set; }
        public string? LayoutImagePath { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<GrandPrix> GrandPrix { get; set; } = new();
    }
}
