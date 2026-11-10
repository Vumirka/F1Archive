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
        private readonly int _id;
        private GrandPrix? _race;
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
                _race = await GrandPrixService.GetProfileAsync(_id);

                if (_race == null)
                {
                    return;
                }

                // Шапка
                NameText.Text = _race.Name;
                RoundChipText.Text = "ГРАН-ПРІ · СЕЗОН " + _race.Season.Year;
                DateText.Text = UiFormat.Date(_race.Date);
                HeroCircuitButton.Content = _race.Circuit.Name;
                CircuitTileButton.Content = _race.Circuit.Name;
                CountryText.Text = _race.Circuit.City == "" ? _race.Circuit.Country : _race.Circuit.City + ", " + _race.Circuit.Country;

                // Смуга з основними даними внизу шапки
                RoundValue.Text = _race.RoundNumber.ToString();
                RoundSub.Text = _race.Season.RoundsCount > 0 ? "етап з " + _race.Season.RoundsCount : "етап";
                LapsValue.Text = _race.Laps?.ToString() ?? "—";
                PoleButton.Content = _race.PoleDriver?.FullName ?? "—";
                PoleButton.IsEnabled = _race.PoleDriver != null;
                FastestButton.Content = _race.FastestLapDriver?.FullName ?? "—";
                FastestButton.IsEnabled = _race.FastestLapDriver != null;

                if (!string.IsNullOrWhiteSpace(_race.FastestLapTime))
                {
                    FastestSub.Text = "найшвидше коло · " + _race.FastestLapTime;
                }

                // Результати гонки
                List<RaceRow> results = await GrandPrixService.GetResultsAsync(_id);

                if (results.Count > 0)
                {
                    ResultsList.ItemsSource = results;
                    ResultsCard.Visibility = Visibility.Visible;
                }
                else
                {
                    ResultsEmpty.Visibility = Visibility.Visible;
                }

                // Кваліфікація і піт-стопи є не в усіх гонках, тоді їхніх вкладок немає
                List<QualifyingResult> qualifying = await GrandPrixService.GetQualifyingAsync(_id);

                if (qualifying.Count > 0)
                {
                    QualifyingList.ItemsSource = qualifying;
                    QualifyingTab.Visibility = Visibility.Visible;
                }

                List<PitRow> pitStops = await GrandPrixService.GetPitStopsAsync(_id);

                if (pitStops.Count > 0)
                {
                    PitStopsList.ItemsSource = pitStops;
                    PitStopsTab.Visibility = Visibility.Visible;
                }

                // Кнопки попереднього і наступного етапу
                (_previous, _next) = await GrandPrixService.GetNeighboursAsync(_race);

                if (_previous != null)
                {
                    PrevButton.Visibility = Visibility.Visible;
                }

                if (_next != null)
                {
                    NextButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        // Вкладки: показується лише вибрана таблиця
        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            // Перша вкладка вмикається ще під час InitializeComponent, коли таблиць ще немає
            if (ResultsSection == null)
            {
                return;
            }

            string tab = (string)((RadioButton)e.OriginalSource).Tag;

            ResultsSection.Visibility = tab == "results" ? Visibility.Visible : Visibility.Collapsed;
            QualifyingSection.Visibility = tab == "qualifying" ? Visibility.Visible : Visibility.Collapsed;
            PitStopsSection.Visibility = tab == "pits" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Circuit_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.CircuitDetails, _race!.CircuitId, _race.Circuit.Name);
        }

        // Кнопки поула і найшвидшого кола вимкнені, якщо гонщика немає
        private void Pole_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.DriverDetails, _race!.PoleDriver!.Id, _race.PoleDriver.FullName);
        }

        private void Fastest_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.DriverDetails, _race!.FastestLapDriver!.Id, _race.FastestLapDriver.FullName);
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.GrandPrixDetails, _previous!.Id, _previous.Name);
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.GrandPrixDetails, _next!.Id, _next.Name);
        }

        // Гонщик у таблицях кваліфікації і піт-стопів: id записаний у Tag кнопки
        private void ExtrasDriver_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            Navigator.NavigateTo(PageKeys.DriverDetails, (int)button.Tag, (string)button.Content);
        }

        private void ResultDriver_Click(object sender, RoutedEventArgs e)
        {
            var row = (RaceRow)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, row.DriverId, row.DriverName);
        }

        private void ResultTeam_Click(object sender, RoutedEventArgs e)
        {
            var row = (RaceRow)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.TeamDetails, row.TeamId, row.TeamName);
        }

        // Таблиця прокручується лише по горизонталі, тому колесо миші передаємо сторінці
        private void TableScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            Scroller.ScrollToVerticalOffset(Scroller.VerticalOffset - e.Delta);
        }
    }
}
