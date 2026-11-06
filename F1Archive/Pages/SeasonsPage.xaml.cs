using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Pages
{
    // Список сезонів від нових до старих
    public partial class SeasonsPage : UserControl
    {
        public SeasonsPage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                var seasons = await SeasonService.GetAllAsync();
                SummaryText.Text =
                    UiFormat.Number(seasons.Count) + " сезонів · " +
                    UiFormat.Number(seasons.Sum(s => s.RoundsCount)) + " Гран-прі · " +
                    UiFormat.Number(seasons.Where(s => s.ChampionDriverId != null).Select(s => s.ChampionDriverId).Distinct().Count()) + " чемпіонів світу · " +
                    UiFormat.Number(seasons.Where(s => s.ChampionTeamId != null).Select(s => s.ChampionTeamId).Distinct().Count()) + " чемпіонів-конструкторів";
                CardsList.ItemsSource = seasons;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: SeasonItem model })
            {
                Navigator.NavigateTo(PageKeys.SeasonDetails, model.Year, model.Year.ToString());
            }
        }
    }
}
