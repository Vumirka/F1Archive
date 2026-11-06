using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Helpers
{
    // Спільне для сторінок-списків: фільтри, пошук, кнопка "Показати ще"
    public static class ListHelper
    {
        // Скільки карток показується одразу
        public const int PageSize = 36;

        // Випадаючий список: перший пункт "Усі ...", далі значення за абеткою
        public static void Fill(ComboBox combo, string allText, IEnumerable<string> values)
        {
            combo.Items.Clear();
            combo.Items.Add(new ComboBoxItem { Content = allText, Tag = "" });

            foreach (string value in values.Where(v => v != "").Distinct().OrderBy(v => v))
            {
                combo.Items.Add(new ComboBoxItem { Content = value, Tag = value });
            }

            combo.SelectedIndex = 0;
        }

        // Вибране значення списку ("" - усі)
        public static string Selected(ComboBox combo)
        {
            return (combo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
        }

        public static bool Matches(string search, params string?[] texts)
        {
            return UiFormat.ForSearch(string.Join(" ", texts)).Contains(UiFormat.ForSearch(search));
        }

        // Показує перші shown карток, лічильник і кнопку "Показати ще".
        // Якщо нічого не знайдено, про це пише лічильник.
        public static void Show<T>(ItemsControl list, List<T> items, int shown, TextBlock count, Button more)
        {
            list.ItemsSource = items.Take(shown).ToList();

            int visible = Math.Min(shown, items.Count);
            int remaining = items.Count - visible;

            if (items.Count == 0)
            {
                count.Text = "Нічого не знайдено. Змініть пошук або фільтри.";
            }
            else
            {
                count.Text = $"Знайдено: {UiFormat.Number(items.Count)} · показано: {UiFormat.Number(visible)}";
            }

            more.Content = $"Показати ще ({UiFormat.Number(remaining)})";
            more.Visibility = remaining > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
