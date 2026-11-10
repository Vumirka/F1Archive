namespace F1Archive.Models
{
    public class Season
    {
        public int Id { get; set; }
        public int Year { get; set; }
        public int RoundsCount { get; set; }

        // Кубок конструкторів розігрується з 1958 року, до того чемпіона-команди немає
        public int? ChampionDriverId { get; set; }
        public int? ChampionTeamId { get; set; }

        public Driver? ChampionDriver { get; set; }
        public Team? ChampionTeam { get; set; }
        public List<GrandPrix> GrandPrix { get; set; } = new();
    }
}
