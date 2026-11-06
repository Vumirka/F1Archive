using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список команд: усі команди завантажуються один раз, а пошук і фільтри працюють у пам'яті
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
            if (_all.Count == 0)
            {
                return;
            }

            IEnumerable<TeamInfo> query = _all.Where(t =>
                ListHelper.Matches(SearchBox.Text, t.Name, t.Country) &&
                (ChampionsCheck.IsChecked != true || t.Titles > 0));

            // За абеткою
            query = query.OrderBy(t => t.Name);
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
            if (sender is FrameworkElement { DataContext: TeamInfo model })
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, model.Id, model.Name);
            }
        }
    }
}
