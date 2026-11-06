using F1Archive.Helpers;
using F1Archive.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Сторінка траси: шапка з основними даними, схема та історія Гран-прі
    public partial class CircuitDetailsPage : UserControl
    {
        private readonly int _id;

        public CircuitDetailsPage(int id)
        {
            InitializeComponent();
            _id = id;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var profile = await CircuitService.GetProfileAsync(_id);
                if (profile == null)
                {
                    return;
                }
                BindProfile(profile);
                ShowCoordinates(profile.Latitude, profile.Longitude);
                BindHistory(await CircuitService.GetHistoryAsync(_id));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void BindProfile(CircuitInfo profile)
        {
            NameText.Text = profile.Name;
            LocationText.Text = (profile.City == ""
                ? profile.Country
                : $"{profile.City} · {profile.Country}").ToUpper();
            StatusText.Text = profile.IsActive ? "Діюча траса" : "Історична траса";
            StatsList.ItemsSource = new List<StatItem>
            {
                new("довжина кола", UiFormat.Length(profile.LengthKm)),
                new("поворотів", profile.Turns?.ToString() ?? "—"),
                new("перший Гран-прі", profile.FirstYear?.ToString() ?? "—"),
                new("проведено Гран-прі", profile.GrandPrixCount.ToString())
            };

            LayoutView.SourcePath = profile.LayoutImagePath;
            NoLayoutNote.Visibility = PhotoService.FullPath(profile.LayoutImagePath) == null
                ? Visibility.Visible
                : Visibility.Collapsed;
            InfoCountryText.Text = profile.Country == "" ? "—" : profile.Country;
            InfoCityText.Text = profile.City == "" ? "—" : profile.City;
            InfoYearsText.Text = UiFormat.Years(profile.FirstYear, profile.LastYear);
        }

        // Координати траси у вигляді "45.6156° пн. ш., 9.2811° сх. д."
        private void ShowCoordinates(double? latitude, double? longitude)
        {
            if (latitude is not double lat || longitude is not double lon)
            {
                return;
            }
            var c = CultureInfo.InvariantCulture;
            CoordinatesText.Text = $"{Math.Abs(lat).ToString("0.0000", c)}° {(lat >= 0
                ? "пн. ш."
                : "пд. ш.")}, " +
                                   $"{Math.Abs(lon).ToString("0.0000", c)}° {(lon >= 0
                                       ? "сх. д."
                                       : "зх. д.")}";
            CoordinatesPanel.Visibility = Visibility.Visible;
        }

        // Історія проведення Гран-прі на трасі: спочатку нові
        private void BindHistory(List<GrandPrixItem> history)
        {
            HistoryList.ItemsSource = history;
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: GrandPrixItem m })
            {
                Navigator.NavigateTo(PageKeys.GrandPrixDetails, m.Id, m.Name);
            }
        }
    }
}
