using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список трас: усі траси завантажуються один раз, а пошук і фільтри працюють у пам'яті
    public partial class CircuitsPage : UserControl
    {
        private List<CircuitInfo> _all = new();
        private List<CircuitInfo> _filtered = new();
        private int _shown = ListHelper.PageSize;

        public CircuitsPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _all = await CircuitService.GetAllAsync();
                SummaryText.Text =
                    UiFormat.Number(_all.Count) + " трас · " +
                    UiFormat.Number(_all.Select(c => c.Country).Distinct().Count()) + " країн · " +
                    UiFormat.Number(_all.Select(c => c.City).Distinct().Count()) + " міст · " +
                    UiFormat.Number(_all.Sum(c => c.GrandPrixCount)) + " проведено Гран-прі";
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
            IEnumerable<CircuitInfo> query = _all.Where(c =>
                ListHelper.Matches(SearchBox.Text, c.Name, c.City, c.Country) &&
                (ActiveCheck.IsChecked != true || c.IsActive));
            // Спочатку траси, де було найбільше Гран-прі
            query = query.OrderByDescending(c => c.GrandPrixCount).ThenBy(c => c.Name);
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
            if (sender is FrameworkElement { DataContext: CircuitInfo model })
            {
                Navigator.NavigateTo(PageKeys.CircuitDetails, model.Id, model.Name);
            }
        }
    }
}
