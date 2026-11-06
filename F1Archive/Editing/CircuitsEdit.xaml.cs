using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;

namespace F1Archive.Editing
{
    public partial class CircuitsEdit : EditPage
    {
        public CircuitsEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton, Photo);
            LoadList();
            NewRecord();
        }

        protected override void LoadList()
        {
            using var db = new F1ArchiveContext();
            List<Circuit> circuits = db.Circuits.OrderBy(c => c.Name).ToList();

            List.ItemsSource = circuits
                .Where(c => ListHelper.Matches(SearchBox.Text, c.Name, c.Country, c.City))
                .ToList();
        }

        protected override void ClearForm()
        {
            FormTitle.Text = "Нова траса";
            NameBox.Text = "";
            CountryBox.Text = "";
            CityBox.Text = "";
            LengthBox.Text = "";
            TurnsBox.Text = "";
            LatitudeBox.Text = "";
            LongitudeBox.Text = "";
            ShowPhoto(null);
        }

        protected override void ShowRecord(object item)
        {
            var circuit = (Circuit)item;

            EditId = circuit.Id;
            FormTitle.Text = circuit.Name;
            NameBox.Text = circuit.Name;
            CountryBox.Text = circuit.Country;
            CityBox.Text = circuit.City;
            LengthBox.Text = circuit.LengthKm.ToString();
            TurnsBox.Text = circuit.Turns.ToString();
            LatitudeBox.Text = circuit.Latitude.ToString();
            LongitudeBox.Text = circuit.Longitude.ToString();
            ShowPhoto(circuit.LayoutImagePath);
        }

        protected override int? SaveRecord()
        {
            if (EditHelper.Text(NameBox) == "")
            {
                ShowStatus("Вкажіть назву траси.");

                return null;
            }

            using var db = new F1ArchiveContext();
            Circuit circuit = EditId == null ? new Circuit() : db.Circuits.Find(EditId)!;

            circuit.Name = EditHelper.Text(NameBox);
            circuit.Country = EditHelper.Text(CountryBox);
            circuit.City = EditHelper.Text(CityBox);
            circuit.LengthKm = EditHelper.Number(LengthBox);
            circuit.Turns = EditHelper.Int(TurnsBox);
            circuit.Latitude = EditHelper.Number(LatitudeBox);
            circuit.Longitude = EditHelper.Number(LongitudeBox);

            if (EditId == null)
            {
                db.Circuits.Add(circuit);
            }

            if (!EditHelper.Save(db))
            {
                ShowStatus("Не вдалося зберегти трасу.");

                return null;
            }

            // Фото копіюємо після запису, бо в назві файлу є id
            circuit.LayoutImagePath = UpdatePhoto(circuit.LayoutImagePath, "Circuits", "circuit-" + circuit.Id);
            EditHelper.Save(db);

            FormTitle.Text = circuit.Name;

            return circuit.Id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();
            Circuit circuit = db.Circuits.Find(EditId)!;
            db.Circuits.Remove(circuit);

            if (!EditHelper.Save(db))
            {
                return "Трасу не можна видалити: на ній проходили Гран-прі.";
            }

            PhotoService.DeleteFile(circuit.LayoutImagePath);

            return null;
        }
    }
}
