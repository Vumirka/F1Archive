using F1Archive.Database;
using F1Archive.Helpers;
using F1Archive.Models;
using F1Archive.Services;

namespace F1Archive.Editing
{
    public partial class TeamsEdit : EditPage
    {
        public TeamsEdit()
        {
            InitializeComponent();
            SetControls(StatusText, DeleteButton, Photo);
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
        }

        protected override void ShowRecord(object item)
        {
            var team = (Team)item;

            EditId = team.Id;
            FormTitle.Text = team.Name;
            NameBox.Text = team.Name;
            CountryBox.Text = team.Country;
            ShowPhoto(team.LogoPath);
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

            FormTitle.Text = team.Name;

            return team.Id;
        }

        protected override string? DeleteRecord()
        {
            using var db = new F1ArchiveContext();
            Team team = db.Teams.Find(EditId)!;
            db.Teams.Remove(team);

            if (!EditHelper.Save(db))
            {
                return "Команду не можна видалити: у неї є результати.";
            }

            PhotoService.DeleteFile(team.LogoPath);

            return null;
        }
    }
}
