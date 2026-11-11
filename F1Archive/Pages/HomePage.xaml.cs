using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1Archive.Pages
{
    // Головна сторінка: цифри архіву, останній сезон, розділи і рекордсмени
    public partial class HomePage : UserControl
    {
        // Картка розділу і рядок у таблиці рекордсменів
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
                using var db = new F1ArchiveContext();

                int seasons = await db.Seasons.CountAsync();
                int races = await db.GrandPrix.CountAsync();
                int drivers = await db.Drivers.CountAsync();
                int teams = await db.Teams.CountAsync();
                int circuits = await db.Circuits.CountAsync();

                HeroYears.Text = await db.Seasons.MinAsync(s => s.Year) + " — " + await db.Seasons.MaxAsync(s => s.Year);

                StatsList.ItemsSource = new List<StatItem>
                {
                    new StatItem("СЕЗОНІВ", seasons.ToString()),
                    new StatItem("ГРАН-ПРІ", races.ToString()),
                    new StatItem("ГОНЩИКІВ", drivers.ToString()),
                    new StatItem("КОМАНД", teams.ToString()),
                    new StatItem("ТРАС", circuits.ToString())
                };

                QuickList.ItemsSource = new List<QuickItem>
                {
                    new QuickItem("Сезони", seasons + " сезонів", PageKeys.Seasons, (ImageSource)FindResource("Icon.Trophy")),
                    new QuickItem("Гран-прі", races + " перегонів", PageKeys.GrandPrix, (ImageSource)FindResource("Icon.Flag")),
                    new QuickItem("Гонщики", drivers + " гонщиків", PageKeys.Drivers, (ImageSource)FindResource("Icon.Driver")),
                    new QuickItem("Команди", teams + " команд", PageKeys.Teams, (ImageSource)FindResource("Icon.Team")),
                    new QuickItem("Траси", circuits + " трас", PageKeys.Circuits, (ImageSource)FindResource("Icon.Circuit"))
                };

                // Останній сезон, у якому вже є чемпіон
                _latest = await db.Seasons
                    .Include(s => s.ChampionDriver)
                    .Include(s => s.ChampionTeam)
                    .Where(s => s.ChampionDriverId != null)
                    .OrderByDescending(s => s.Year)
                    .FirstOrDefaultAsync();

                if (_latest != null)
                {
                    LatestTitle.Text = "Останній завершений сезон · " + _latest.Year;
                    ChampionDriverCard.Show(_latest.ChampionDriver?.FullName ?? "—", "", "", _latest.ChampionDriver?.ImagePath);
                    ChampionTeamCard.Show(_latest.ChampionTeam?.Name ?? "—", "", "", _latest.ChampionTeam?.LogoPath);
                }
                else
                {
                    LatestSection.Visibility = Visibility.Collapsed;
                }

                _drivers = await DriverService.GetAllAsync();
                ShowLeaders("titles");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не вдалося завантажити дані: " + ex.Message);
            }
        }

        // Рекордсмени: вісім гонщиків з найбільшим показником
        private void ShowLeaders(string kind)
        {
            string unit = "титулів";

            if (kind == "wins")
            {
                unit = "перемог";
            }
            else if (kind == "podiums")
            {
                unit = "подіумів";
            }
            else if (kind == "poles")
            {
                unit = "поулів";
            }

            List<DriverInfo> top = _drivers
                .Where(d => LeaderValue(d, kind) > 0)
                .OrderByDescending(d => LeaderValue(d, kind))
                .ThenBy(d => d.FullName)
                .Take(8)
                .ToList();

            var rows = new List<LeaderRow>();

            for (int i = 0; i < top.Count; i++)
            {
                DriverInfo d = top[i];
                int value = LeaderValue(d, kind);
                int max = LeaderValue(top[0], kind);

                rows.Add(new LeaderRow((i + 1).ToString("00"), d.FullName, value, max, i == 0, d.Id,
                    $"{d.FullName}, {d.Country}: {value} {unit}"));
            }

            LeadersList.ItemsSource = rows;
        }

        // Показник гонщика, за яким будується таблиця рекордсменів
        private static int LeaderValue(DriverInfo d, string kind)
        {
            if (kind == "wins")
            {
                return d.Wins;
            }

            if (kind == "podiums")
            {
                return d.Podiums;
            }

            if (kind == "poles")
            {
                return d.Poles;
            }

            return d.Titles;
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
            var item = (QuickItem)((Button)sender).DataContext;
            Navigator.NavigateTo(item.PageKey);
        }

        private void Leader_Click(object sender, RoutedEventArgs e)
        {
            var row = (LeaderRow)((Button)sender).DataContext;
            Navigator.NavigateTo(PageKeys.DriverDetails, row.DriverId, row.Name);
        }

        private void Leaders_Checked(object sender, RoutedEventArgs e)
        {
            // Перша кнопка вмикається ще під час InitializeComponent, коли списку ще немає
            if (LeadersList == null)
            {
                return;
            }

            var button = (RadioButton)e.OriginalSource;
            ShowLeaders((string)button.Tag);
        }
    }
}
