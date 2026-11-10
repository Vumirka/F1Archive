using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список трас: усі траси завантажуються один раз, а пошук і фільтр працюють у пам'яті
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
            _filtered = _all
                .Where(c => ListHelper.Matches(SearchBox.Text, c.Name, c.City, c.Country))
                .ToList();

            // Кнопка "Лише діючі": траси з останнього сезону архіву
            if (ActiveCheck.IsChecked == true)
            {
                _filtered = _filtered.Where(c => c.IsActive).ToList();
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
            var circuit = (CircuitInfo)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.CircuitDetails, circuit.Id, circuit.Name);
        }
    }
}
