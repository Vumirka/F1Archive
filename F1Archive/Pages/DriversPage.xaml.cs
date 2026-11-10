using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список гонщиків: усі гонщики завантажуються один раз, а пошук і фільтр працюють у пам'яті
    public partial class DriversPage : UserControl
    {
        private List<DriverInfo> _all = new();
        private List<DriverInfo> _filtered = new();
        private int _shown = ListHelper.PageSize;

        public DriversPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _all = await DriverService.GetAllAsync();
                SummaryText.Text =
                    UiFormat.Number(_all.Count) + " гонщиків · " +
                    UiFormat.Number(_all.Select(d => d.Country).Distinct().Count()) + " країн · " +
                    UiFormat.Number(_all.Count(d => d.Wins > 0)) + " переможців гонок · " +
                    UiFormat.Number(_all.Count(d => d.Titles > 0)) + " чемпіонів світу";
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void ApplyFilters()
        {
            _filtered = _all
                .Where(d => ListHelper.Matches(SearchBox.Text, d.FullName, d.Country))
                .ToList();

            // Кнопка "Лише чемпіони світу": чемпіони від найбільшої кількості титулів до найменшої
            if (ChampionsCheck.IsChecked == true)
            {
                _filtered = _filtered
                    .Where(d => d.Titles > 0)
                    .OrderByDescending(d => d.Titles)
                    .ThenByDescending(d => d.Wins)
                    .ToList();
            }

            ListHelper.Show(CardsList, _filtered, _shown, CountText, MoreButton);
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            _shown = ListHelper.PageSize;
            ApplyFilters();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            _shown += ListHelper.PageSize;
            ListHelper.Show(CardsList, _filtered, _shown, CountText, MoreButton);
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            var driver = (DriverInfo)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, driver.Id, driver.FullName);
        }

        // Відкриває сторінку будь-якого з гонщиків архіву
        private void Random_Click(object sender, RoutedEventArgs e)
        {
            if (_all.Count > 0)
            {
                DriverInfo driver = _all[Random.Shared.Next(_all.Count)];
                Navigator.NavigateTo(PageKeys.DriverDetails, driver.Id, driver.FullName);
            }
        }
    }
}
