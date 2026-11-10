using F1Archive.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1Archive.Editing
{
    // Поле, у яке вписують гонщика, команду або трасу.
    // Можна писати ім'я, прізвище або обидва в будь-якому порядку,
    // а під полем видно, який запис знайдено.
    public class NameBox : UserControl
    {
        private readonly TextBox _box = new();
        private readonly TextBlock _hint = new() { FontSize = 12, Margin = new Thickness(2, 4, 0, 0) };

        // З яких записів шукати
        public List<ListItem> Choices { get; set; } = new();

        public NameBox()
        {
            var panel = new StackPanel();
            panel.Children.Add(_box);
            panel.Children.Add(_hint);
            Content = panel;

            _box.TextChanged += (sender, e) => ShowHint();
        }

        // id знайденого запису або null, якщо поле порожнє чи підходить не один запис
        public int? SelectedId
        {
            get
            {
                List<ListItem> found = Found();
                return found.Count == 1 ? found[0].Id : null;
            }
            set
            {
                _box.Text = Choices.FirstOrDefault(c => c.Id == value)?.Name ?? "";
            }
        }

        // Щось вписано, але запис не знайдено
        public bool IsWrong => _box.Text.Trim() != "" && SelectedId == null;

        // Записи, у назві яких є всі введені слова
        private List<ListItem> Found()
        {
            string text = _box.Text.Trim();
            string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return new List<ListItem>();
            }

            List<ListItem> found = Choices
                .Where(c => words.All(w => ListHelper.Matches(w, c.Name)))
                .ToList();

            // Якщо назву вписано повністю, беремо саме її, навіть коли є схожі
            List<ListItem> exact = found
                .Where(c => UiFormat.ForSearch(c.Name) == UiFormat.ForSearch(text))
                .ToList();

            return exact.Count == 1 ? exact : found;
        }

        private void ShowHint()
        {
            List<ListItem> found = Found();

            if (_box.Text.Trim() == "")
            {
                _hint.Text = "";
            }
            else if (found.Count == 1)
            {
                _hint.Text = "✓ " + found[0].Name;
            }
            else if (found.Count == 0)
            {
                _hint.Text = "Нічого не знайдено";
            }
            else
            {
                _hint.Text = $"Знайдено {found.Count}: " + string.Join(", ", found.Take(3).Select(c => c.Name))
                    + (found.Count > 3 ? "..." : "") + " Уточніть.";
            }

            _hint.Foreground = found.Count == 1 ? Brushes.ForestGreen : Brushes.Firebrick;
        }
    }
}
