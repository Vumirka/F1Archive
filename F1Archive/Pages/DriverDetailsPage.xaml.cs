using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Профіль гонщика: шапка з цифрами, кар'єра по сезонах і команди
    public partial class DriverDetailsPage : UserControl
    {
        private readonly int _id;

        public DriverDetailsPage(int id)
        {
            InitializeComponent();
            _id = id;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                DriverInfo? driver = await DriverService.GetProfileAsync(_id);

                if (driver == null)
                {
                    return;
                }

                NameText.Text = driver.FullName;
                CountryText.Text = driver.Country;
                Portrait.SourcePath = driver.ImagePath;
                BirthText.Text = driver.BirthDate != null ? UiFormat.Date(driver.BirthDate.Value) : "—";
                CareerText.Text = driver.YearsText;

                // Код країни на білій плашці (GB, IT ...)
                CountryCodeText.Text = UiFormat.CountryCode(driver.Country);

                if (CountryCodeText.Text != "")
                {
                    CountryBadge.Visibility = Visibility.Visible;
                }

                if (driver.IsChampion)
                {
                    ChampionBadgeText.Text = driver.ChampionBadge;
                    ChampionBadge.Visibility = Visibility.Visible;
                }

                // Постійний номер: великий на фоні шапки і в даних гонщика
                if (driver.PermanentNumber != null)
                {
                    HeroNumber.Text = driver.PermanentNumber.ToString();
                    PermanentNumberText.Text = "#" + driver.PermanentNumber;
                    PermanentNumberPanel.Visibility = Visibility.Visible;
                }

                DriverCareer career = await DriverService.GetCareerAsync(_id);

                StatsList.ItemsSource = new List<StatItem>
                {
                    new StatItem("титулів", driver.Titles.ToString()),
                    new StatItem("перемог", UiFormat.Number(driver.Wins)),
                    new StatItem("подіумів", UiFormat.Number(driver.Podiums)),
                    new StatItem("поулів", UiFormat.Number(driver.Poles)),
                    new StatItem("стартів", UiFormat.Number(career.Starts)),
                    new StatItem("сезонів", UiFormat.Number(career.Seasons.Count)),
                    new StatItem("найкраще місце", career.BestPosition?.ToString() ?? "—")
                };

                if (career.Seasons.Count > 0)
                {
                    CareerList.ItemsSource = career.Seasons;
                    CareerSection.Visibility = Visibility.Visible;
                }

                if (career.Teams.Count > 0)
                {
                    TeamsList.ItemsSource = career.Teams;
                    TeamsSection.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void Team_Click(object sender, RoutedEventArgs e)
        {
            var team = (DriverTeam)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.TeamDetails, team.TeamId, team.Name);
        }

        private void CareerYear_Click(object sender, RoutedEventArgs e)
        {
            var season = (DriverSeason)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.SeasonDetails, season.Year, season.Year.ToString());
        }

        private void CareerTeam_Click(object sender, RoutedEventArgs e)
        {
            var team = (Link)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.TeamDetails, team.Id, team.Name);
        }
    }
}
