using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;

namespace F1Archive.Editing
{
    public partial class DriversEdit : EditPage
    {
        public DriversEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton, Photo);
            LoadList();
            NewRecord();
        }

        protected override void LoadList()
        {
            using var db = new F1ArchiveContext();
            List<Driver> drivers = db.Drivers.OrderBy(d => d.LastName).ToList();

            List.ItemsSource = drivers
                .Where(d => ListHelper.Matches(SearchBox.Text, d.FirstName, d.LastName, d.Country))
                .ToList();
        }

        protected override void ClearForm()
        {
            FormTitle.Text = "Новий гонщик";
            FirstNameBox.Text = "";
            LastNameBox.Text = "";
            CountryBox.Text = "";
            BirthDateBox.Text = "";
            NumberBox.Text = "";
            ShowPhoto(null);
        }

        protected override void ShowRecord(object item)
        {
            var driver = (Driver)item;

            EditId = driver.Id;
            FormTitle.Text = driver.FirstName + " " + driver.LastName;
            FirstNameBox.Text = driver.FirstName;
            LastNameBox.Text = driver.LastName;
            CountryBox.Text = driver.Country;
            BirthDateBox.Text = driver.BirthDate?.ToString("dd.MM.yyyy");
            NumberBox.Text = driver.PermanentNumber.ToString();
            ShowPhoto(driver.ImagePath);
        }

        protected override int? SaveRecord()
        {
            if (EditHelper.Text(FirstNameBox) == "" || EditHelper.Text(LastNameBox) == "" || EditHelper.Text(CountryBox) == "")
            {
                ShowStatus("Заповніть ім'я, прізвище і країну.");

                return null;
            }

            if (BirthDateBox.Text != "" && EditHelper.Date(BirthDateBox) == null)
            {
                ShowStatus("Дату народження введіть як 22.02.1949.");

                return null;
            }

            using var db = new F1ArchiveContext();
            Driver driver = EditId == null ? new Driver() : db.Drivers.Find(EditId)!;

            driver.FirstName = EditHelper.Text(FirstNameBox);
            driver.LastName = EditHelper.Text(LastNameBox);
            driver.Country = EditHelper.Text(CountryBox);
            driver.BirthDate = EditHelper.Date(BirthDateBox);
            driver.PermanentNumber = EditHelper.Int(NumberBox);

            if (EditId == null)
            {
                db.Drivers.Add(driver);
            }

            if (!EditHelper.Save(db))
            {
                ShowStatus("Не вдалося зберегти гонщика.");

                return null;
            }

            // Фото копіюємо після запису, бо в назві файлу є id
            driver.ImagePath = UpdatePhoto(driver.ImagePath, "Drivers", "driver-" + driver.Id);
            EditHelper.Save(db);

            FormTitle.Text = driver.FirstName + " " + driver.LastName;

            return driver.Id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();
            Driver driver = db.Drivers.Find(EditId)!;
            db.Drivers.Remove(driver);

            if (!EditHelper.Save(db))
            {
                return "Гонщика не можна видалити: у нього є результати гонок.";
            }

            PhotoService.DeleteFile(driver.ImagePath);

            return null;
        }
    }
}
