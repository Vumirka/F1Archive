using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список команд: усі команди завантажуються один раз, а пошук і фільтр працюють у пам'яті
    public partial class TeamsPage : UserControl
    {
        private List<TeamInfo> _all = new();
        private List<TeamInfo> _filtered = new();
        private int _shown = ListHelper.PageSize;

        public TeamsPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                _all = await TeamService.GetAllAsync();
                SummaryText.Text =
                    UiFormat.Number(_all.Count) + " команд · " +
                    UiFormat.Number(_all.Count(t => t.Titles > 0)) + " чемпіонів-конструкторів · " +
                    UiFormat.Number(_all.Count(t => t.Wins > 0)) + " команд-переможців гонок · " +
                    UiFormat.Number(_all.Select(t => t.Country).Distinct().Count()) + " країн";
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
                .Where(t => ListHelper.Matches(SearchBox.Text, t.Name, t.Country))
                .ToList();

            // Кнопка "Лише чемпіони-конструктори": від найбільшої кількості Кубків до найменшої
            if (ChampionsCheck.IsChecked == true)
            {
                _filtered = _filtered
                    .Where(t => t.Titles > 0)
                    .OrderByDescending(t => t.Titles)
                    .ThenByDescending(t => t.Wins)
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
            var team = (TeamInfo)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.TeamDetails, team.Id, team.Name);
        }
    }
}
