using F1Archive.Database;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1Archive.Editing
{
    public class ListItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";

        public override string ToString()
        {
            return Name;
        }
    }

    public static class EditHelper
    {
        public static string Text(TextBox box)
        {
            return box.Text.Trim();
        }

        // Ціле число або null, якщо поле порожнє чи введено не число
        public static int? Int(TextBox box)
        {
            int value;

            if (int.TryParse(box.Text.Trim(), out value))
            {
                return value;
            }

            return null;
        }

        // Дробове число: можна писати і з комою, і з крапкою
        public static double? Number(TextBox box)
        {
            string text = box.Text.Trim().Replace(',', '.');
            double value;

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return null;
        }

        // Дата у вигляді дд.мм.рррр
        public static DateTime? Date(TextBox box)
        {
            DateTime date;

            if (DateTime.TryParseExact(box.Text.Trim(), "dd.MM.yyyy", null, DateTimeStyles.None, out date))
            {
                return date;
            }

            return null;
        }

        public static int? Selected(ComboBox combo)
        {
            return combo.SelectedValue as int?;
        }

        // Записує зміни в базу. false, якщо база відмовила
        // (такий запис уже є або на запис, який видаляють, посилаються інші дані).
        public static bool Save(F1ArchiveContext db)
        {
            try
            {
                db.SaveChanges();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        public static void Status(TextBlock text, string message, bool error = true)
        {
            text.Text = message;
            text.Foreground = error ? Brushes.Firebrick : Brushes.ForestGreen;
        }

        public static bool ConfirmDelete()
        {
            MessageBoxResult answer = MessageBox.Show("Видалити запис? Цю дію не можна скасувати.", "Видалення",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            return answer == MessageBoxResult.Yes;
        }

        public static List<ListItem> Seasons()
        {
            using var db = new F1ArchiveContext();

            return db.Seasons
                .OrderByDescending(s => s.Year)
                .Select(s => new ListItem { Id = s.Id, Name = s.Year.ToString() })
                .ToList();
        }

        public static List<ListItem> Drivers()
        {
            using var db = new F1ArchiveContext();

            return db.Drivers
                .OrderBy(d => d.LastName)
                .Select(d => new ListItem { Id = d.Id, Name = d.LastName + " " + d.FirstName })
                .ToList();
        }

        public static List<ListItem> Teams()
        {
            using var db = new F1ArchiveContext();

            return db.Teams
                .OrderBy(t => t.Name)
                .Select(t => new ListItem { Id = t.Id, Name = t.Name })
                .ToList();
        }

        public static List<ListItem> Circuits()
        {
            using var db = new F1ArchiveContext();

            return db.Circuits
                .OrderBy(c => c.Name)
                .Select(c => new ListItem { Id = c.Id, Name = c.Name })
                .ToList();
        }

        public static List<ListItem> GrandPrix(int seasonId)
        {
            using var db = new F1ArchiveContext();

            return db.GrandPrix
                .Where(g => g.SeasonId == seasonId)
                .OrderBy(g => g.RoundNumber)
                .Select(g => new ListItem { Id = g.Id, Name = g.RoundNumber.ToString() + ". " + g.Name })
                .ToList();
        }
    }
}
