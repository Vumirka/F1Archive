using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список гонщиків: усі гонщики завантажуються один раз, а пошук і фільтри працюють у пам'яті
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
            if (_all.Count == 0)
            {
                return;
            }

            IEnumerable<DriverInfo> query = _all.Where(d =>
                ListHelper.Matches(SearchBox.Text, d.FullName, d.Country) &&
                (ChampionsCheck.IsChecked != true || d.Titles > 0));

            // За абеткою, як на картці: спочатку прізвище
            query = query.OrderBy(d => d.LastName).ThenBy(d => d.FirstName);
            _filtered = query.ToList();
            ShowCards();
        }

        private void ShowCards()
        {
            ListHelper.Show(CardsList, _filtered, _shown, CountText, MoreButton);
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            _shown = ListHelper.PageSize;
            ApplyFilters();
        }

        private void More_Click(object sender, RoutedEventArgs e)
        {
            _shown += ListHelper.PageSize;
            ShowCards();
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: DriverInfo model })
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, model.Id, model.FullName);
            }
        }
    }
}
