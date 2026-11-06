using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1Archive.Pages
{
    // Головна сторінка: цифри архіву, останній сезон, розділи і рекордсмени
    public partial class HomePage : UserControl
    {
        public record QuickItem(string Title, string Hint, string PageKey, ImageSource Icon);
        public record LeaderRow(string Place, string Name, int Value, int Max, bool IsLeader, int DriverId, string Tooltip);

        private Season? _latest;
        private List<DriverInfo> _drivers = new();

        public HomePage()
        {
            InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            try
            {
                ShowHero(await HomeService.GetCountsAsync());
                ShowLatestSeason(await HomeService.GetLatestSeasonAsync());

                _drivers = await DriverService.GetAllAsync();
                ShowLeaders("titles");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        private void ShowHero(ArchiveCounts counts)
        {
            HeroYears.Text = $"{counts.FirstYear} — {counts.LastYear}";

            StatsList.ItemsSource = new List<StatItem>
            {
                new("СЕЗОНІВ", counts.Seasons.ToString()),
                new("ГРАН-ПРІ", counts.GrandPrix.ToString()),
                new("ГОНЩИКІВ", counts.Drivers.ToString()),
                new("КОМАНД", counts.Teams.ToString()),
                new("ТРАС", counts.Circuits.ToString())
            };

            QuickList.ItemsSource = new List<QuickItem>
            {
                new("Сезони", $"{counts.Seasons} сезонів", PageKeys.Seasons, Icon("Icon.Trophy")),
                new("Гран-прі", $"{counts.GrandPrix} перегонів", PageKeys.GrandPrix, Icon("Icon.Flag")),
                new("Гонщики", $"{counts.Drivers} гонщиків", PageKeys.Drivers, Icon("Icon.Driver")),
                new("Команди", $"{counts.Teams} команд", PageKeys.Teams, Icon("Icon.Team")),
                new("Траси", $"{counts.Circuits} трас", PageKeys.Circuits, Icon("Icon.Circuit"))
            };
        }

        private ImageSource Icon(string key)
        {
            return (ImageSource)FindResource(key);
        }

        private void ShowLatestSeason(Season? latest)
        {
            _latest = latest;

            if (latest == null)
            {
                LatestSection.Visibility = Visibility.Collapsed;
                return;
            }

            LatestTitle.Text = "Останній завершений сезон · " + latest.Year;
            ChampionDriverCard.Show(latest.ChampionDriver?.FullName ?? "—", "", "", latest.ChampionDriver?.ImagePath);
            ChampionTeamCard.Show(latest.ChampionTeam?.Name ?? "—", "", "", latest.ChampionTeam?.LogoPath);
        }

        // Рекордсмени: вісім гонщиків з найбільшим показником
        private void ShowLeaders(string kind)
        {
            Func<DriverInfo, int> value = kind switch
            {
                "wins" => d => d.Wins,
                "podiums" => d => d.Podiums,
                "poles" => d => d.Poles,
                _ => d => d.Titles
            };

            string unit = kind switch
            {
                "wins" => "перемог",
                "podiums" => "подіумів",
                "poles" => "поулів",
                _ => "титулів"
            };

            List<DriverInfo> top = _drivers
                .Where(d => value(d) > 0)
                .OrderByDescending(value)
                .ThenBy(d => d.FullName)
                .Take(8)
                .ToList();

            var rows = new List<LeaderRow>();

            for (int i = 0; i < top.Count; i++)
            {
                DriverInfo d = top[i];
                rows.Add(new LeaderRow((i + 1).ToString("00"), d.FullName, value(d), value(top[0]), i == 0, d.Id,
                    $"{d.FullName}, {d.Country}: {value(d)} {unit}"));
            }

            LeadersList.ItemsSource = rows;
        }

        private void LatestSeason_Click(object sender, RoutedEventArgs e)
        {
            if (_latest != null)
            {
                Navigator.NavigateTo(PageKeys.SeasonDetails, _latest.Year, _latest.Year.ToString());
            }
        }

        private void ChampionDriver_Click(object sender, EventArgs e)
        {
            if (_latest?.ChampionDriver != null)
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, _latest.ChampionDriver.Id, _latest.ChampionDriver.FullName);
            }
        }

        private void ChampionTeam_Click(object sender, EventArgs e)
        {
            if (_latest?.ChampionTeam != null)
            {
                Navigator.NavigateTo(PageKeys.TeamDetails, _latest.ChampionTeam.Id, _latest.ChampionTeam.Name);
            }
        }

        private void Quick_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: QuickItem item })
            {
                Navigator.NavigateTo(item.PageKey);
            }
        }

        private void Leader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: LeaderRow row })
            {
                Navigator.NavigateTo(PageKeys.DriverDetails, row.DriverId, row.Name);
            }
        }

        private void Leaders_Checked(object sender, RoutedEventArgs e)
        {
            // Перша кнопка вмикається ще під час InitializeComponent, коли списку ще немає
            if (LeadersList == null)
            {
                return;
            }

            if (e.OriginalSource is RadioButton { Tag: string kind })
            {
                ShowLeaders(kind);
            }
        }
    }
}
