using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace F1Archive.Pages
{
    // Сторінка Гран-прі: шапка, основні дані, результати, кваліфікація і піт-стопи
    public partial class GrandPrixDetailsPage : UserControl
    {
        private const int RowsShown = 10;
        private List<RaceRow> _results = new();
        private readonly int _id;
        private GrandPrix? _profile;
        private Link? _previous;
        private Link? _next;

        public GrandPrixDetailsPage(int id)
        {
            InitializeComponent();
            _id = id;
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _profile = await GrandPrixService.GetProfileAsync(_id);
                if (_profile == null)
                {
                    return;
                }
                BindProfile(_profile);
                BindResults(await GrandPrixService.GetResultsAsync(_id));
                BindExtras(_profile.FastestLapTime, await GrandPrixService.GetQualifyingAsync(_id), await GrandPrixService.GetPitStopsAsync(_id));
                (_previous, _next) = await GrandPrixService.GetNeighboursAsync(_profile);
                PrevButton.Visibility = _previous != null ? Visibility.Visible : Visibility.Collapsed;
                NextButton.Visibility = _next != null ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void BindProfile(GrandPrix profile)
        {
            NameText.Text = profile.Name;
            RoundChipText.Text = $"ГРАН-ПРІ · СЕЗОН {profile.Season.Year}";
            DateText.Text = UiFormat.Date(profile.Date);
            HeroCircuitButton.Content = CircuitTileButton.Content = profile.Circuit.Name;
            CountryText.Text = profile.Circuit.City == ""
                ? profile.Circuit.Country
                : $"{profile.Circuit.City}, {profile.Circuit.Country}";
            RoundValue.Text = profile.RoundNumber.ToString();
            RoundSub.Text = profile.Season.RoundsCount > 0 ? $"етап з {profile.Season.RoundsCount}" : "етап";
            LapsValue.Text = profile.Laps?.ToString() ?? "—";
            PoleButton.Content = profile.PoleDriver?.FullName ?? "—";
            PoleButton.IsEnabled = profile.PoleDriver != null;
            FastestButton.Content = profile.FastestLapDriver?.FullName ?? "—";
            FastestButton.IsEnabled = profile.FastestLapDriver != null;
            Hero.Visibility = Visibility.Visible;
        }

        private void BindResults(List<RaceRow> results)
        {
            if (results.Count == 0)
            {
                ResultsEmpty.Visibility = Visibility.Visible;
                ResultsCard.Visibility = Visibility.Collapsed;
                return;
            }
            // Спочатку перші 10 рядків, решта за кнопкою
            _results = results;
            ResultsList.ItemsSource = _results.Take(RowsShown).ToList();
            AllRowsButton.Content = $"Показати всіх ({_results.Count})";
            AllRowsButton.Visibility = _results.Count > RowsShown ? Visibility.Visible : Visibility.Collapsed;
            ResultsCard.Visibility = Visibility.Visible;
        }

        // Кваліфікація, піт-стопи й час найшвидшого кола
        private void BindExtras(string? fastestLapTime, List<QualifyingResult> qualifying, List<PitRow> pitStops)
        {
            if (!string.IsNullOrWhiteSpace(fastestLapTime))
            {
                FastestSub.Text = $"найшвидше коло · {fastestLapTime}";
            }

            if (qualifying.Count > 0)
            {
                QualifyingList.ItemsSource = qualifying;
                QualifyingTab.Visibility = Visibility.Visible;
            }
            if (pitStops.Count > 0)
            {
                PitStopsList.ItemsSource = pitStops;
                PitStopsTab.Visibility = Visibility.Visible;
            }
        }

        // Вкладки: показується лише вибрана таблиця
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (ResultsSection == null || e.OriginalSource is not RadioButton { Tag: string tab })
            {
                return;
            }
            ResultsSection.Visibility = tab == "results" ? Visibility.Visible : Visibility.Collapsed;
            QualifyingSection.Visibility = tab == "qualifying" ? Visibility.Visible : Visibility.Collapsed;
            PitStopsSection.Visibility = tab == "pits" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void AllRows_Click(object sender, RoutedEventArgs e)
        {
            ResultsList.ItemsSource = _results;
            AllRowsButton.Visibility = Visibility.Collapsed;
        }

        // Перехід на сторінку гонщика, команди чи траси, якщо id відомий
        private static void Open(string page, int? id, string? title = null)
        {
            if (id != null)
            {
                Navigator.NavigateTo(page, id.Value, title);
            }
        }

        private void Circuit_Click(object sender, RoutedEventArgs e)
        {
            Open(PageKeys.CircuitDetails, _profile?.CircuitId, _profile?.Circuit.Name);
        }

        private void Pole_Click(object sender, RoutedEventArgs e)
        {
            Open(PageKeys.DriverDetails, _profile?.PoleDriverId, _profile?.PoleDriver?.FullName);
        }

        private void Fastest_Click(object sender, RoutedEventArgs e)
        {
            Open(PageKeys.DriverDetails, _profile?.FastestLapDriverId, _profile?.FastestLapDriver?.FullName);
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            Open(PageKeys.GrandPrixDetails, _previous?.Id, _previous?.Name);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Open(PageKeys.GrandPrixDetails, _next?.Id, _next?.Name);
        }

        private void ExtrasDriver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { Tag: int driverId })
            {
                Open(PageKeys.DriverDetails, driverId);
            }
        }

        private void ResultDriver_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RaceRow r })
            {
                Open(PageKeys.DriverDetails, r.DriverId, r.DriverName);
            }
        }

        private void ResultTeam_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: RaceRow r })
            {
                Open(PageKeys.TeamDetails, r.TeamId, r.TeamName);
            }
        }

        // Таблиця прокручується лише по горизонталі, тому колесо миші передаємо сторінці
        private void TableScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta);
        }
    }
}
