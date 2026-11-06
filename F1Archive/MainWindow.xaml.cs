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

            Navigator.Init(PageHost);
            Navigator.Navigated += OnNavigated;
            Navigator.NavigateTo(PageKeys.Home);
        }

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is RadioButton { CommandParameter: string key })
            {
                Navigator.NavigateTo(key);
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

        private void OnNavigated()
        {
            var page = Navigator.Current!;
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

        // Пошук: під полем з'являється список, сторінка відкривається після кліку або Enter
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text.Trim();

            if (text.Length < 2)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            try
            {
                List<SearchHit> hits = await HomeService.SearchAsync(text);

                // Поки йшов пошук, текст могли змінити
                if (SearchBox.Text.Trim() != text)
                {
                    return;
                }

                var view = new ListCollectionView(hits);
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SearchHit.Group)));

                SearchResultsList.ItemsSource = view;
                SearchResultsList.Visibility = hits.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                SearchEmptyText.Visibility = hits.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                SearchPopup.IsOpen = true;
            }
            catch (Exception)
            {
                // Якщо пошук не вдався, список просто не показуємо
                SearchPopup.IsOpen = false;
            }
        }

        // Esc закриває пошук, стрілка вниз переходить у список результатів
        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchPopup.IsOpen = false;
                SearchBox.Clear();
            }
            else if (e.Key == Key.Down && SearchPopup.IsOpen && SearchResultsList.Items.Count > 0)
            {
                SearchResultsList.SelectedIndex = 0;
                ((ListBoxItem)SearchResultsList.ItemContainerGenerator.ContainerFromIndex(0)).Focus();
                e.Handled = true;
            }
        }

        private void SearchResults_Click(object sender, MouseButtonEventArgs e)
        {
            if (SearchResultsList.SelectedItem is SearchHit hit)
            {
                OpenSearchHit(hit);
            }
        }

        private void SearchResults_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResultsList.SelectedItem is SearchHit hit)
            {
                OpenSearchHit(hit);
            }
        }

        private void OpenSearchHit(SearchHit hit)
        {
            SearchPopup.IsOpen = false;
            SearchBox.Clear();

            Navigator.NavigateTo(hit.Page, hit.Id, hit.Title);
        }
    }
}
