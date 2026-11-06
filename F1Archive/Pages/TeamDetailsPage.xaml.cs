using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Профіль команди: шапка з цифрами, опис, історія (роки участі, Кубки, сезони), гонщики і боліди
    public partial class TeamDetailsPage : UserControl
    {
        private readonly int _id;
        private TeamInfo? _profile;
        private const int Shown = 10;  // скільки сезонів показати одразу
        private const int ShownTiles = 12; // скільки гонщиків і болідів показати одразу
        private List<TeamSeason> _seasons = new();
        private List<TeamDriver> _drivers = new();
        private List<Car> _cars = new();

        public TeamDetailsPage(int id)
        {
            InitializeComponent();
            _id = id;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _profile = await TeamService.GetProfileAsync(_id);
                if (_profile == null)
                {
                    return;
                }
                BindProfile(_profile);
                BindCareer(await TeamService.GetCareerAsync(_id));
                _cars = await TeamService.GetCarsAsync(_id);
                if (_cars.Count > 0)
                {
                    ShowFirst(CarsList, _cars, ShownTiles, AllCarsButton, "Показати всі боліди");
                    CarsSection.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void BindProfile(TeamInfo profile)
        {
            NameText.Text = profile.Name;
            CountryText.Text = profile.Country;
            CountryCodeText.Text = UiFormat.CountryCode(profile.Country);
            CountryBadge.Visibility = CountryCodeText.Text == "" ? Visibility.Collapsed : Visibility.Visible;
            Logo.SourcePath = profile.LogoPath;

            if (profile.IsChampion)
            {
                ChampionBadgeText.Text = profile.ChampionBadge;
                ChampionBadge.Visibility = Visibility.Visible;
            }
            StatusText.Text = profile.IsActive ? "Виступає зараз" : "Історична команда";
            YearsText.Text = profile.YearsText;
            FirstText.Text = profile.FirstSeason?.ToString() ?? "—";
            LastText.Text = profile.LastSeason?.ToString() ?? "—";
        }

        private void BindCareer(TeamCareer career)
        {
            StatsList.ItemsSource = new List<StatItem>
            {
                new("кубків конструкторів", _profile!.Titles.ToString()),
                new("перемог", UiFormat.Number(_profile.Wins)),
                new("подіумів", UiFormat.Number(career.Podiums)),
                new("поулів", UiFormat.Number(career.Poles)),
                new("Гран-прі", UiFormat.Number(career.GrandPrix)),
                new("сезонів", UiFormat.Number(career.Seasons.Count)),
                new("гонщиків", UiFormat.Number(career.Drivers.Count))
            };

            // Історія: роки участі, Кубки конструкторів і таблиця сезонів (від нових до старих)
            if (career.Seasons.Count > 0)
            {
                PeriodsList.ItemsSource = career.Periods;
                TitleYearsList.ItemsSource = career.TitleYears;
                TitlesRow.Visibility = career.TitleYears.Count > 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                _seasons = career.Seasons.OrderByDescending(s => s.Year).ToList();
                ShowFirst(SeasonsList, _seasons, Shown, AllSeasonsButton, "Показати всі сезони");
                HistorySection.Visibility = Visibility.Visible;
            }

            // Гонщики: спершу ті, хто виграв найбільше
            if (career.Drivers.Count > 0)
            {
                _drivers = career.Drivers
                    .OrderByDescending(d => d.Wins)
                    .ThenByDescending(d => d.Podiums)
                    .ToList();
                ShowFirst(DriversList, _drivers, ShownTiles, AllDriversButton, "Показати всіх гонщиків");
                DriversSection.Visibility = Visibility.Visible;
            }
        }

        // Показує перші count елементів; решту відкриває кнопка
        private static void ShowFirst<T>(ItemsControl list, List<T> items, int count, Button more,
            string text)
        {
            list.ItemsSource = items.Take(count).ToList();
            more.Content = $"{text} ({items.Count})";
            more.Visibility = items.Count > count ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AllSeasons_Click(object sender, RoutedEventArgs e)
        {
            ShowFirst(SeasonsList, _seasons, _seasons.Count, AllSeasonsButton, "");
        }

        private void AllDrivers_Click(object sender, RoutedEventArgs e)
        {
            ShowFirst(DriversList, _drivers, _drivers.Count, AllDriversButton, "");
        }

        private void AllCars_Click(object sender, RoutedEventArgs e)
        {
            ShowFirst(CarsList, _cars, _cars.Count, AllCarsButton, "");
        }

        private void TitleYear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: int year })
            {
                Navigator.NavigateTo(PageKeys.SeasonDetails, year, year.ToString());
            }
        }

        private void SeasonYear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: TeamSeason m })
            {
                Navigator.NavigateTo(PageKeys.SeasonDetails, m.Year, m.Year.ToString());
            }
        }

        private void SeasonDriver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Link d })
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, d.Id, d.Name);
            }
        }

        private void TeamDriver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: TeamDriver m })
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, m.DriverId, m.Name);
            }
        }
    }
}
