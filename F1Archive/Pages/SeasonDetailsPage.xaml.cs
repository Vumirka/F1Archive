using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Сторінка сезону: шапка з цифрами, чемпіони, календар, особистий залік і учасники
    public partial class SeasonDetailsPage : UserControl
    {
        private const int StandingsShown = 10;
        private readonly int _year;
        private SeasonInfo? _overview;
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
                _overview = await SeasonService.GetOverviewAsync(_year);
                if (_overview == null)
                {
                    return;
                }
                _standings = await SeasonService.GetStandingsAsync(_year);
                ShowOverview(_overview);
                ShowCalendar(await SeasonService.GetCalendarAsync(_year));
                ShowStandings();

                var teams = await SeasonService.GetTeamsAsync(_year);
                ParticipantsList.ItemsSource = teams;
                ParticipantsSection.Visibility = teams.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void ShowOverview(SeasonInfo overview)
        {
            HeroSubtitle.Text = overview.GrandPrixCount + " Гран-прі";

            StatsList.ItemsSource = new List<StatItem>
            {
                new("Гран-прі", UiFormat.Number(overview.GrandPrixCount)),
                new("гонщиків", UiFormat.Number(overview.DriversCount)),
                new("команд", UiFormat.Number(overview.TeamsCount)),
                new("трас", UiFormat.Number(overview.CircuitsCount)),
                new("переможців гонок", UiFormat.Number(overview.WinnersCount))
            };

            var champion = overview.Season.ChampionDriver;
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

            var team = overview.Season.ChampionTeam;
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

        private void ShowCalendar(List<CalendarRow> calendar)
        {
            CalendarSection.Visibility = calendar.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            CalendarList.ItemsSource = calendar;
        }

        // Залік: у таблиці спочатку перші 10 гонщиків
        private void ShowStandings()
        {
            StandingsSection.Visibility = _standings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            StandingsList.ItemsSource = _standings.Take(StandingsShown).ToList();
            AllStandingsButton.Content = $"Показати весь залік ({_standings.Count})";
            AllStandingsButton.Visibility = _standings.Count > StandingsShown
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void AllStandings_Click(object sender, RoutedEventArgs e)
        {
            StandingsList.ItemsSource = _standings;
            AllStandingsButton.Visibility = Visibility.Collapsed;
        }

        private void DriverCard_Click(object sender, EventArgs e)
        {
            if (_overview?.Season.ChampionDriver is { } d)
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, d.Id, d.FullName);
            }
        }

        private void TeamCard_Click(object sender, EventArgs e)
        {
            if (_overview?.Season.ChampionTeam is { } t)
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, t.Id, t.Name);
            }
        }

        private void TeamEntry_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: SeasonTeam t })
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, t.TeamId, t.TeamName);
            }
        }

        private void StandingRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: StandingRow m })
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, m.DriverId, m.DriverName);
            }
        }

        private void CalendarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: CalendarRow m })
            {
                Navigator.NavigateTo(PageKeys.GrandPrixDetails, m.GrandPrixId, m.Name);
            }
        }
    }
}
