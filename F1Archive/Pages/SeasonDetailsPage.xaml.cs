using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Сторінка сезону: шапка з цифрами, чемпіони, календар, особистий залік і учасники
    public partial class SeasonDetailsPage : UserControl
    {
        private const int StandingsShown = 10;
        private readonly int _year;
        private Season? _season;
        private List<StandingRow> _standings = new();

        public SeasonDetailsPage(int year)
        {
            InitializeComponent();
            _year = year;
            HeroTitle.Text = year.ToString();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                using var db = new F1ArchiveContext();

                _season = await db.Seasons
                    .Include(s => s.ChampionDriver)
                    .Include(s => s.ChampionTeam)
                    .FirstOrDefaultAsync(s => s.Year == _year);

                if (_season == null)
                {
                    return;
                }

                // Цифри для шапки
                var races = db.GrandPrix.Where(g => g.SeasonId == _season.Id);
                var results = db.Results.Where(r => r.GrandPrix.SeasonId == _season.Id);

                int raceCount = await races.CountAsync();
                HeroSubtitle.Text = raceCount + " Гран-прі";

                StatsList.ItemsSource = new List<StatItem>
                {
                    new StatItem("Гран-прі", UiFormat.Number(raceCount)),
                    new StatItem("гонщиків", UiFormat.Number(await results.Select(r => r.DriverId).Distinct().CountAsync())),
                    new StatItem("команд", UiFormat.Number(await results.Select(r => r.TeamId).Distinct().CountAsync())),
                    new StatItem("трас", UiFormat.Number(await races.Select(g => g.CircuitId).Distinct().CountAsync())),
                    new StatItem("переможців гонок", UiFormat.Number(await results.Where(r => r.FinishPosition == 1).Select(r => r.DriverId).Distinct().CountAsync()))
                };

                _standings = await SeasonService.GetStandingsAsync(_year);
                ShowChampions();

                List<GrandPrixItem> calendar = await SeasonService.GetCalendarAsync(_year);
                CalendarList.ItemsSource = calendar;
                CalendarSection.Visibility = calendar.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Залік: у таблиці спочатку перші 10 гонщиків, решта за кнопкою
                StandingsList.ItemsSource = _standings.Take(StandingsShown).ToList();
                StandingsSection.Visibility = _standings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                AllStandingsButton.Content = $"Показати весь залік ({_standings.Count})";
                AllStandingsButton.Visibility = _standings.Count > StandingsShown ? Visibility.Visible : Visibility.Collapsed;

                List<SeasonTeam> teams = await SeasonService.GetTeamsAsync(_year);
                ParticipantsList.ItemsSource = teams;
                ParticipantsSection.Visibility = teams.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void ShowChampions()
        {
            Driver? champion = _season!.ChampionDriver;

            if (champion != null)
            {
                // Цифри чемпіона беремо з його рядка в заліку
                string stats = "";
                StandingRow? row = _standings.FirstOrDefault(s => s.DriverId == champion.Id);

                if (row != null)
                {
                    stats = $"Очки {row.PointsText} · Перемоги {row.Wins} · Подіуми {row.Podiums} · Поули {row.Poles}";
                }

                DriverCard.Show(champion.FullName, champion.Country, stats, champion.ImagePath);
            }
            else
            {
                DriverCard.ShowEmpty("Чемпіона не визначено",
                    "Для цього сезону немає даних про підсумковий залік.");
            }

            Team? team = _season.ChampionTeam;

            if (team != null)
            {
                TeamCard.Show(team.Name, team.Country, "", team.LogoPath);
            }
            else if (_year < 1958)
            {
                TeamCard.ShowEmpty("Кубок конструкторів ще не проводився",
                    "Кубок конструкторів запроваджено у 1958 році.");
            }
            else
            {
                TeamCard.ShowEmpty("Чемпіона серед команд не визначено",
                    "Для цього сезону немає даних про залік конструкторів.");
            }
        }

        private void AllStandings_Click(object sender, RoutedEventArgs e)
        {
            StandingsList.ItemsSource = _standings;
            AllStandingsButton.Visibility = Visibility.Collapsed;
        }

        private void DriverCard_Click(object sender, EventArgs e)
        {
            Driver? d = _season?.ChampionDriver;

            if (d != null)
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, d.Id, d.FullName);
            }
        }

        private void TeamCard_Click(object sender, EventArgs e)
        {
            Team? t = _season?.ChampionTeam;

            if (t != null)
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, t.Id, t.Name);
            }
        }

        private void TeamEntry_Click(object sender, RoutedEventArgs e)
        {
            var team = (SeasonTeam)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.TeamDetails, team.TeamId, team.TeamName);
        }

        private void StandingRow_Click(object sender, RoutedEventArgs e)
        {
            var row = (StandingRow)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, row.DriverId, row.DriverName);
        }

        private void CalendarItem_Click(object sender, RoutedEventArgs e)
        {
            var race = (GrandPrixItem)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.GrandPrixDetails, race.Id, race.Name);
        }
    }
}
