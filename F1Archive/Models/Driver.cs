namespace F1Archive.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public DateTime? BirthDate { get; set; }
        public string Country { get; set; } = "";
        public string? ImagePath { get; set; }

        // Постійний номер (з 2014 року). Номер у конкретній гонці лежить у Result.CarNumber
        public int? PermanentNumber { get; set; }

        public List<Result> Results { get; set; } = new();

        // Не зберігається в базі, лише для показу
        public string FullName => FirstName + " " + LastName;
    }
}
