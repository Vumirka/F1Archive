using F1Archive.Helpers;
using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace F1Archive
{
    // Головне вікно: меню зліва, зверху заголовок і пошук, у центрі сторінка
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Width = SystemParameters.WorkArea.Width * 0.9;
            Height = SystemParameters.WorkArea.Height * 0.9;

#if !DEBUG
            // У релізі "Керування даними" сховане: користувач лише переглядає архів
            AdminSection.Visibility = Visibility.Collapsed;
            AdminButton.Visibility = Visibility.Collapsed;
#endif

            Navigator.Init(PageHost);
            Navigator.Navigated += OnNavigated;
            Navigator.NavigateTo(PageKeys.Home);
        }

        // Клік по пункту меню: назва сторінки записана в CommandParameter
        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            var button = e.OriginalSource as RadioButton;

            if (button != null)
            {
                Navigator.NavigateTo((string)button.CommandParameter);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Navigator.GoBack();
        }

        // Клік по назві розділу під заголовком ("Гонщики › Льюїс Гамільтон")
        private void Section_Click(object sender, RoutedEventArgs e)
        {
            Navigator.NavigateTo(PageKeys.SectionOf(Navigator.Current!.PageKey));
        }

        // Після переходу оновлюємо заголовок, кнопку "Назад" і вибраний пункт меню
        private void OnNavigated()
        {
            Navigator.Entry page = Navigator.Current!;
            string section = PageKeys.SectionOf(page.PageKey);

            PageTitleText.Text = page.Title;
            SectionLink.Content = PageKeys.TitleOf(section);
            CurrentTitleText.Text = page.Title;
            PathPanel.Visibility = section != page.PageKey ? Visibility.Visible : Visibility.Collapsed;
            BackButton.IsEnabled = Navigator.CanGoBack;

            foreach (RadioButton item in NavPanel.Children.OfType<RadioButton>())
            {
                item.IsChecked = (string)item.CommandParameter == section;
            }
        }

        // Пошук: під полем з'являється список, сторінка відкривається після кліку
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text.Trim();

            if (text.Length < 2)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            List<SearchHit> hits;

            try
            {
                hits = await SearchService.SearchAsync(text);
            }
            catch (Exception)
            {
                // Якщо пошук не вдався, список просто не показуємо
                SearchPopup.IsOpen = false;
                return;
            }

            // Поки йшов пошук, текст могли змінити
            if (SearchBox.Text.Trim() != text)
            {
                return;
            }

            // Результати групуються: гонщики, команди, Гран-прі, траси
            var view = new ListCollectionView(hits);
            view.GroupDescriptions.Add(new PropertyGroupDescription("Group"));

            SearchResultsList.ItemsSource = view;
            SearchResultsList.Visibility = hits.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            SearchEmptyText.Visibility = hits.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            SearchPopup.IsOpen = true;
        }

        private void SearchResults_Click(object sender, MouseButtonEventArgs e)
        {
            var hit = SearchResultsList.SelectedItem as SearchHit;

            if (hit == null)
            {
                return;
            }

            SearchPopup.IsOpen = false;
            SearchBox.Clear();
            Navigator.NavigateTo(hit.Page, hit.Id, hit.Title);
        }
    }
}
