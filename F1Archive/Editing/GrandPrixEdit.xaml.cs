using F1Archive.Database;
using F1Archive.Models;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Editing
{
    // Сезони і Гран-прі. Зверху вибраний сезон і його дані, нижче його етапи, справа форма етапу.
    public partial class GrandPrixEdit : EditPage
    {
        public GrandPrixEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton);

            List<ListItem> drivers = EditHelper.Drivers();
            List<ListItem> teams = EditHelper.Teams();
            CircuitBox.Choices = EditHelper.Circuits();
            PoleBox.Choices = drivers;
            FastestDriverBox.Choices = drivers;
            ChampionBox.Choices = drivers;
            ChampionTeamBox.Choices = teams;

            SeasonFilter.ItemsSource = EditHelper.Seasons();
            SeasonFilter.SelectedIndex = 0;
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ShowSeason();
            LoadList();
            NewRecord();
        }

        private void ShowSeason()
        {
            using var db = new F1ArchiveContext();
            int? seasonId = EditHelper.Selected(SeasonFilter);
            Season? season = seasonId == null ? null : db.Seasons.Find(seasonId);

            YearBox.Text = season?.Year.ToString();
            ChampionBox.SelectedId = season?.ChampionDriverId;
            ChampionTeamBox.SelectedId = season?.ChampionTeamId;
            RoundsBox.Text = season?.RoundsCount.ToString();
            SeasonStatusText.Text = "";
        }

        private void SaveSeason_Click(object sender, RoutedEventArgs e)
        {
            int? year = EditHelper.Int(YearBox);

            if (year == null)
            {
                EditHelper.Status(SeasonStatusText, "Вкажіть рік сезону.");

                return;
            }

            if (ChampionBox.IsWrong || ChampionTeamBox.IsWrong)
            {
                EditHelper.Status(SeasonStatusText, "Уточніть чемпіонів: під полем має бути зелена галочка.");

                return;
            }

            int? seasonId = EditHelper.Selected(SeasonFilter);

            using var db = new F1ArchiveContext();
            Season season = seasonId == null ? new Season() : db.Seasons.Find(seasonId)!;

            season.Year = year.Value;
            season.ChampionDriverId = ChampionBox.SelectedId;
            season.ChampionTeamId = ChampionTeamBox.SelectedId;

            // Якщо кількість етапів не вказали, рахуємо вже додані Гран-прі
            int? rounds = EditHelper.Int(RoundsBox);

            if (rounds == null)
            {
                rounds = db.GrandPrix.Count(g => g.SeasonId == season.Id);
            }

            season.RoundsCount = rounds.Value;

            if (seasonId == null)
            {
                db.Seasons.Add(season);
            }

            if (!EditHelper.Save(db))
            {
                EditHelper.Status(SeasonStatusText, "Сезон з таким роком уже є.");

                return;
            }

            SeasonFilter.ItemsSource = EditHelper.Seasons();
            SeasonFilter.SelectedValue = season.Id;
            EditHelper.Status(SeasonStatusText, "Сезон збережено.", false);
        }

        private void NewSeason_Click(object sender, RoutedEventArgs e)
        {
            SeasonFilter.SelectedItem = null;
        }

        private void DeleteSeason_Click(object sender, RoutedEventArgs e)
        {
            int? seasonId = EditHelper.Selected(SeasonFilter);

            if (seasonId == null || !EditHelper.ConfirmDelete())
            {
                return;
            }

            using var db = new F1ArchiveContext();
            db.Seasons.Remove(db.Seasons.Find(seasonId)!);

            if (!EditHelper.Save(db))
            {
                EditHelper.Status(SeasonStatusText, "Сезон не можна видалити: у ньому є Гран-прі.");

                return;
            }

            SeasonFilter.ItemsSource = EditHelper.Seasons();
            SeasonFilter.SelectedIndex = 0;
        }

        protected override void LoadList()
        {
            using var db = new F1ArchiveContext();
            int? seasonId = EditHelper.Selected(SeasonFilter);

            List.ItemsSource = db.GrandPrix
                .Include(g => g.Circuit)
                .Where(g => g.SeasonId == seasonId)
                .OrderBy(g => g.RoundNumber)
                .ToList();
        }

        protected override void ClearForm()
        {
            FormTitle.Text = "Нове Гран-прі";
            RaceNameBox.Text = "";
            RoundBox.Text = (List.Items.Count + 1).ToString();
            DateBox.Text = "";
            CircuitBox.SelectedId = null;
            LapsBox.Text = "";
            PoleBox.SelectedId = null;
            FastestDriverBox.SelectedId = null;
            FastestTimeBox.Text = "";
            FastestLapBox.Text = "";
        }

        protected override void ShowRecord(object item)
        {
            var race = (GrandPrix)item;

            EditId = race.Id;
            FormTitle.Text = race.Name;
            RaceNameBox.Text = race.Name;
            RoundBox.Text = race.RoundNumber.ToString();
            DateBox.Text = race.Date.ToString("dd.MM.yyyy");
            CircuitBox.SelectedId = race.CircuitId;
            LapsBox.Text = race.Laps.ToString();
            PoleBox.SelectedId = race.PoleDriverId;
            FastestDriverBox.SelectedId = race.FastestLapDriverId;
            FastestTimeBox.Text = race.FastestLapTime ?? "";
            FastestLapBox.Text = race.FastestLapNumber.ToString();
        }

        protected override int? SaveRecord()
        {
            int? seasonId = EditHelper.Selected(SeasonFilter);
            int? round = EditHelper.Int(RoundBox);
            DateTime? date = EditHelper.Date(DateBox);
            int? circuitId = CircuitBox.SelectedId;

            if (seasonId == null)
            {
                ShowStatus("Спочатку виберіть або збережіть сезон.");

                return null;
            }

            if (EditHelper.Text(RaceNameBox) == "" || round == null || date == null || circuitId == null)
            {
                ShowStatus("Заповніть назву, номер етапу, дату (дд.мм.рррр) і трасу.");

                return null;
            }

            if (PoleBox.IsWrong || FastestDriverBox.IsWrong)
            {
                ShowStatus("Уточніть гонщиків: під полем має бути зелена галочка.");

                return null;
            }

            using var db = new F1ArchiveContext();
            GrandPrix race = EditId == null ? new GrandPrix() : db.GrandPrix.Find(EditId)!;

            race.SeasonId = seasonId.Value;
            race.Name = EditHelper.Text(RaceNameBox);
            race.RoundNumber = round.Value;
            race.Date = date.Value;
            race.CircuitId = circuitId.Value;
            race.Laps = EditHelper.Int(LapsBox);
            race.PoleDriverId = PoleBox.SelectedId;
            race.FastestLapDriverId = FastestDriverBox.SelectedId;
            race.FastestLapTime = EditHelper.Text(FastestTimeBox) == "" ? null : EditHelper.Text(FastestTimeBox);
            race.FastestLapNumber = EditHelper.Int(FastestLapBox);

            if (EditId == null)
            {
                db.GrandPrix.Add(race);
            }

            if (!EditHelper.Save(db))
            {
                ShowStatus("Етап з таким номером у цьому сезоні вже є.");

                return null;
            }
            FormTitle.Text = race.Name;

            return race.Id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();
            db.GrandPrix.Remove(db.GrandPrix.Find(EditId)!);

            if (!EditHelper.Save(db))
            {
                return "Гран-прі не можна видалити: у ньому є результати.";
            }

            return null;
        }
    }
}
