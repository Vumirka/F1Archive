using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список усіх Гран-прі: усі гонки завантажуються один раз, а пошук і фільтри працюють у пам'яті
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
                FillFilters();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void FillFilters()
        {
            SeasonCombo.Items.Clear();
            SeasonCombo.Items.Add(new ComboBoxItem { Content = "Усі сезони", Tag = "" });
            foreach (int year in _all.Select(r => r.Year).Distinct().OrderByDescending(y => y))
            {
                SeasonCombo.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year.ToString() });
            }
            SeasonCombo.SelectedIndex = 0;
        }

        private void ApplyFilters()
        {
            if (_all.Count == 0)
            {
                return;
            }
            string season = ListHelper.Selected(SeasonCombo);
            IEnumerable<GrandPrixItem> query = _all.Where(r =>
                ListHelper.Matches(SearchBox.Text, r.Name, r.CircuitName, r.CircuitCountry) &&
                (season == "" || r.Year.ToString() == season));
            // Гран-прі завжди від нових до старих
            query = query.OrderByDescending(r => r.Date);
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
            if (sender is FrameworkElement { DataContext: GrandPrixItem model })
            {
                Navigator.NavigateTo(PageKeys.GrandPrixDetails, model.Id, model.Name);
            }
        }
    }
}
