using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Editing
{
    public partial class TeamsEdit : EditPage
    {
        // id вибраного боліда (null - новий болід)
        private int? _carId;

        public TeamsEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton, Photo);
            CarSeasonBox.ItemsSource = EditHelper.Seasons();
            LoadList();
            NewRecord();
        }

        protected override void LoadList()
        {
            using var db = new F1ArchiveContext();
            List<Team> teams = db.Teams.OrderBy(t => t.Name).ToList();

            List.ItemsSource = teams
                .Where(t => ListHelper.Matches(SearchBox.Text, t.Name, t.Country))
                .ToList();
        }

        protected override void ClearForm()
        {
            FormTitle.Text = "Нова команда";
            NameBox.Text = "";
            CountryBox.Text = "";
            ShowPhoto(null);
            LoadCars();
        }

        protected override void ShowRecord(object item)
        {
            var team = (Team)item;

            EditId = team.Id;
            FormTitle.Text = team.Name;
            NameBox.Text = team.Name;
            CountryBox.Text = team.Country;
            ShowPhoto(team.LogoPath);
            LoadCars();
        }

        protected override int? SaveRecord()
        {
            if (EditHelper.Text(NameBox) == "")
            {
                ShowStatus("Вкажіть назву команди.");

                return null;
            }

            using var db = new F1ArchiveContext();
            Team team = EditId == null ? new Team() : db.Teams.Find(EditId)!;

            team.Name = EditHelper.Text(NameBox);
            team.Country = EditHelper.Text(CountryBox);

            if (EditId == null)
            {
                db.Teams.Add(team);
            }

            if (!EditHelper.Save(db))
            {
                ShowStatus("Не вдалося зберегти команду.");

                return null;
            }

            // Фото копіюємо після запису, бо в назві файлу є id
            team.LogoPath = UpdatePhoto(team.LogoPath, "Teams", "team-" + team.Id);
            EditHelper.Save(db);

            // Після збереження нової команди їй уже можна додавати боліди
            FormTitle.Text = team.Name;
            EditId = team.Id;
            LoadCars();

            return team.Id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();
            Team team = db.Teams.Find(EditId)!;
            db.Teams.Remove(team);

            if (!EditHelper.Save(db))
            {
                return "Команду не можна видалити: у неї є результати або боліди.";
            }

            PhotoService.DeleteFile(team.LogoPath);

            return null;
        }

        private void LoadCars()
        {
            using var db = new F1ArchiveContext();

            CarsList.ItemsSource = db.Cars
                .Include(c => c.Season)
                .Where(c => c.TeamId == EditId)
                .OrderByDescending(c => c.Season.Year)
                .ToList();

            CarsPanel.IsEnabled = EditId != null;
            ClearCar();
        }

        private void ClearCar()
        {
            _carId = null;
            CarModelBox.Text = "";
            CarSeasonBox.SelectedValue = null;
            CarEngineBox.Text = "";
            CarChassisBox.Text = "";
            CarStatusText.Text = "";
        }

        private void CarsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var car = CarsList.SelectedItem as Car;

            if (car == null)
            {
                return;
            }

            _carId = car.Id;
            CarModelBox.Text = car.Model;
            CarSeasonBox.SelectedValue = car.SeasonId;
            CarEngineBox.Text = car.Engine;
            CarChassisBox.Text = car.Chassis ?? "";
            CarStatusText.Text = "";
        }

        private void SaveCar_Click(object sender, RoutedEventArgs e)
        {
            int? seasonId = EditHelper.Selected(CarSeasonBox);

            if (EditHelper.Text(CarModelBox) == "" || seasonId == null)
            {
                EditHelper.Status(CarStatusText, "Заповніть модель і сезон боліда.");

                return;
            }

            using var db = new F1ArchiveContext();
            Car car = _carId == null ? new Car() : db.Cars.Find(_carId)!;

            car.TeamId = EditId!.Value;
            car.SeasonId = seasonId.Value;
            car.Model = EditHelper.Text(CarModelBox);
            car.Engine = EditHelper.Text(CarEngineBox);
            car.Chassis = EditHelper.Text(CarChassisBox) == "" ? null : EditHelper.Text(CarChassisBox);

            if (_carId == null)
            {
                db.Cars.Add(car);
            }

            if (!EditHelper.Save(db))
            {
                EditHelper.Status(CarStatusText, "Не вдалося зберегти болід.");

                return;
            }

            LoadCars();
            EditHelper.Status(CarStatusText, "Болід збережено.", false);
        }

        private void NewCar_Click(object sender, RoutedEventArgs e)
        {
            ClearCar();
        }

        private void DeleteCar_Click(object sender, RoutedEventArgs e)
        {
            if (_carId == null || !EditHelper.ConfirmDelete())
            {
                return;
            }

            using var db = new F1ArchiveContext();
            Car car = db.Cars.Find(_carId)!;
            db.Cars.Remove(car);

            if (!EditHelper.Save(db))
            {
                EditHelper.Status(CarStatusText, "Болід не можна видалити: до нього прив'язані гонщики.");

                return;
            }

            LoadCars();
        }
    }
}
