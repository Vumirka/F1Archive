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
        private DriverInfo? _profile;

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
                _profile = await DriverService.GetProfileAsync(_id);
                if (_profile == null)
                {
                    return;
                }
                BindProfile(_profile);
                BindCareer(await DriverService.GetCareerAsync(_id));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void BindProfile(DriverInfo profile)
        {
            NameText.Text = profile.FullName;
            // Великий номер у шапці: постійний номер, а якщо його немає, то ініціали
            HeroNumber.Text = profile.PermanentNumber?.ToString() ?? "";
            CountryText.Text = profile.Country;
            CountryCodeText.Text = UiFormat.CountryCode(profile.Country);
            CountryBadge.Visibility = CountryCodeText.Text == "" ? Visibility.Collapsed : Visibility.Visible;
            Portrait.SourcePath = profile.ImagePath;

            if (profile.IsChampion)
            {
                ChampionBadgeText.Text = profile.ChampionBadge;
                ChampionBadge.Visibility = Visibility.Visible;
            }
            if (profile.PermanentNumber.HasValue)
            {
                PermanentNumberText.Text = "#" + profile.PermanentNumber;
                PermanentNumberPanel.Visibility = Visibility.Visible;
            }
            BirthText.Text = profile.BirthDate.HasValue ? UiFormat.Date(profile.BirthDate.Value) : "—";
            CareerText.Text = profile.YearsText;
        }

        private void BindCareer(DriverCareer career)
        {
            StatsList.ItemsSource = new List<StatItem>
            {
                new("титулів", _profile!.Titles.ToString()),
                new("перемог", UiFormat.Number(_profile.Wins)),
                new("подіумів", UiFormat.Number(_profile.Podiums)),
                new("поулів", UiFormat.Number(_profile.Poles)),
                new("стартів", UiFormat.Number(career.Starts)),
                new("сезонів", UiFormat.Number(career.Seasons.Count)),
                new("найкраще місце", career.BestPosition?.ToString() ?? "—")
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

        private void Team_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DriverTeam team })
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, team.TeamId, team.Name);
            }
        }

        private void CareerYear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DriverSeason m })
            {
                Navigator.NavigateTo(PageKeys.SeasonDetails, m.Year, m.Year.ToString());
            }
        }

        private void CareerTeam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: Link team })
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, team.Id, team.Name);
            }
        }
    }
}
