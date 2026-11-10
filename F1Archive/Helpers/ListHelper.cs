using System.Windows;
using System.Windows.Controls;

namespace F1Archive.Helpers
{
    // Спільне для сторінок-списків: пошук і кнопка "Показати ще"
    public static class ListHelper
    {
        // Скільки карток показується одразу
        public const int PageSize = 36;

        // Чи є текст пошуку в одному з полів (без різниці великі чи малі літери)
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
