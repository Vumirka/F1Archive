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
                CircuitInfo? circuit = await CircuitService.GetProfileAsync(_id);

                if (circuit == null)
                {
                    return;
                }

                NameText.Text = circuit.Name;
                LocationText.Text = circuit.LocationText.ToUpper();
                StatusText.Text = circuit.IsActive ? "Діюча траса" : "Історична траса";

                StatsList.ItemsSource = new List<StatItem>
                {
                    new StatItem("довжина кола", circuit.LengthText),
                    new StatItem("поворотів", circuit.Turns?.ToString() ?? "—"),
                    new StatItem("перший Гран-прі", circuit.FirstYear?.ToString() ?? "—"),
                    new StatItem("проведено Гран-прі", circuit.GrandPrixCount.ToString())
                };

                LayoutView.SourcePath = circuit.LayoutImagePath;

                if (PhotoService.FullPath(circuit.LayoutImagePath) == null)
                {
                    NoLayoutNote.Visibility = Visibility.Visible;
                }

                InfoCountryText.Text = circuit.Country == "" ? "—" : circuit.Country;
                InfoCityText.Text = circuit.City == "" ? "—" : circuit.City;
                InfoYearsText.Text = UiFormat.Years(circuit.FirstYear, circuit.LastYear);

                // Координати у вигляді "45.6156° пн. ш., 9.2811° сх. д."
                if (circuit.Latitude != null && circuit.Longitude != null)
                {
                    double lat = circuit.Latitude.Value;
                    double lon = circuit.Longitude.Value;
                    string latText = Math.Abs(lat).ToString("0.0000", CultureInfo.InvariantCulture);
                    string lonText = Math.Abs(lon).ToString("0.0000", CultureInfo.InvariantCulture);

                    CoordinatesText.Text = latText + "° " + (lat >= 0 ? "пн. ш." : "пд. ш.") + ", "
                        + lonText + "° " + (lon >= 0 ? "сх. д." : "зх. д.");
                    CoordinatesPanel.Visibility = Visibility.Visible;
                }

                // Історія проведення Гран-прі на трасі: спочатку нові
                HistoryList.ItemsSource = await CircuitService.GetHistoryAsync(_id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            var race = (GrandPrixItem)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.GrandPrixDetails, race.Id, race.Name);
        }
    }
}
