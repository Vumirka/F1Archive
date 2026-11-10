using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Профіль команди: шапка з цифрами, історія (роки участі, Кубки, сезони), і гонщики
    public partial class TeamDetailsPage : UserControl
    {
        private const int SeasonsShown = 10; // скільки сезонів показати одразу
        private const int DriversShown = 12; // скільки гонщиків показати одразу

        private readonly int _id;
        private List<TeamSeason> _seasons = new();
        private List<TeamDriver> _drivers = new();

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
                TeamInfo? team = await TeamService.GetProfileAsync(_id);

                if (team == null)
                {
                    return;
                }

                NameText.Text = team.Name;
                CountryText.Text = team.Country;
                StatusText.Text = team.IsActive ? "Виступає зараз" : "Історична команда";
                Logo.SourcePath = team.LogoPath;

                // Для деяких старих команд логотипа немає зовсім, тоді біла рамка не потрібна
                if (PhotoService.FullPath(team.LogoPath) == null)
                {
                    LogoBox.Visibility = Visibility.Collapsed;
                }

                CountryCodeText.Text = UiFormat.CountryCode(team.Country);

                if (CountryCodeText.Text != "")
                {
                    CountryBadge.Visibility = Visibility.Visible;
                }

                if (team.IsChampion)
                {
                    ChampionBadgeText.Text = team.ChampionBadge;
                    ChampionBadge.Visibility = Visibility.Visible;
                }

                TeamCareer career = await TeamService.GetCareerAsync(_id);

                StatsList.ItemsSource = new List<StatItem>
                {
                    new StatItem("кубків конструкторів", team.Titles.ToString()),
                    new StatItem("перемог", UiFormat.Number(team.Wins)),
                    new StatItem("подіумів", UiFormat.Number(career.Podiums)),
                    new StatItem("поулів", UiFormat.Number(career.Poles)),
                    new StatItem("Гран-прі", UiFormat.Number(career.GrandPrix)),
                    new StatItem("сезонів", UiFormat.Number(career.Seasons.Count)),
                    new StatItem("гонщиків", UiFormat.Number(career.Drivers.Count))
                };

                YearsText.Text = career.YearsText;

                // Історія: Кубки конструкторів і таблиця сезонів
                if (career.Seasons.Count > 0)
                {
                    TitleYearsList.ItemsSource = career.TitleYears;

                    if (career.TitleYears.Count > 0)
                    {
                        TitlesCard.Visibility = Visibility.Visible;
                    }

                    _seasons = career.Seasons;
                    ShowFirst(SeasonsList, _seasons, SeasonsShown, AllSeasonsButton, "Показати всі сезони");
                    HistorySection.Visibility = Visibility.Visible;
                }

                if (career.Drivers.Count > 0)
                {
                    _drivers = career.Drivers;
                    ShowFirst(DriversList, _drivers, DriversShown, AllDriversButton, "Показати всіх гонщиків");
                    DriversSection.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        // Показує перші count елементів, решту відкриває кнопка
        private static void ShowFirst<T>(ItemsControl list, List<T> items, int count, Button more, string text)
        {
            list.ItemsSource = items.Take(count).ToList();
            more.Content = text + " (" + items.Count + ")";
            more.Visibility = items.Count > count ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AllSeasons_Click(object sender, RoutedEventArgs e)
        {
            SeasonsList.ItemsSource = _seasons;
            AllSeasonsButton.Visibility = Visibility.Collapsed;
        }

        private void AllDrivers_Click(object sender, RoutedEventArgs e)
        {
            DriversList.ItemsSource = _drivers;
            AllDriversButton.Visibility = Visibility.Collapsed;
        }

        private void TitleYear_Click(object sender, RoutedEventArgs e)
        {
            int year = (int)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.SeasonDetails, year, year.ToString());
        }

        private void SeasonYear_Click(object sender, RoutedEventArgs e)
        {
            var season = (TeamSeason)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.SeasonDetails, season.Year, season.Year.ToString());
        }

        private void SeasonDriver_Click(object sender, RoutedEventArgs e)
        {
            var driver = (Link)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, driver.Id, driver.Name);
        }

        private void TeamDriver_Click(object sender, RoutedEventArgs e)
        {
            var driver = (TeamDriver)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, driver.DriverId, driver.Name);
        }
    }
}
