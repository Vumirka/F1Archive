using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список усіх Гран-прі (від нових до старих): гонки завантажуються один раз, а пошук і фільтр працюють у пам'яті
    public partial class GrandPrixPage : UserControl
    {
        private List<GrandPrixItem> _all = new();
        private List<GrandPrixItem> _filtered = new();
        private int _shown = ListHelper.PageSize;

        public GrandPrixPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _all = await GrandPrixService.GetAllAsync();
                SummaryText.Text =
                    UiFormat.Number(_all.Count) + " Гран-прі · " +
                    UiFormat.Number(_all.Select(r => r.Year).Distinct().Count()) + " сезонів · " +
                    UiFormat.Number(_all.Select(r => r.CircuitId).Distinct().Count()) + " трас · " +
                    UiFormat.Number(_all.Select(r => r.CircuitCountry).Distinct().Count()) + " країн";

                // Список сезонів для фільтра: перший пункт - усі сезони
                SeasonCombo.Items.Add("Усі сезони");

                foreach (int year in _all.Select(r => r.Year).Distinct())
                {
                    SeasonCombo.Items.Add(year.ToString());
                }

                // Після вибору першого пункту спрацює Filter_Changed і покаже картки
                SeasonCombo.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void ApplyFilters()
        {
            _filtered = _all
                .Where(r => ListHelper.Matches(SearchBox.Text, r.Name, r.CircuitName, r.CircuitCountry))
                .ToList();

            // Вибраний сезон (0 - "Усі сезони")
            if (SeasonCombo.SelectedIndex > 0)
            {
                string year = (string)SeasonCombo.SelectedItem;
                _filtered = _filtered.Where(r => r.Year.ToString() == year).ToList();
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
            var race = (GrandPrixItem)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.GrandPrixDetails, race.Id, race.Name);
        }
    }
}
