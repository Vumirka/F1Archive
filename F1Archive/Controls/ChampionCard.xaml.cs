using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace F1Archive.Controls
{
    // Картка чемпіона сезону (гонщик або команда) з фото
    public partial class ChampionCard : UserControl
    {
        public ChampionCard()
        {
            InitializeComponent();
        }

        // Підпис над ім'ям: "чемпіон світу", "кубок конструкторів"
        public string Overline
        {
            get => OverlineText.Text;
            set => OverlineText.Text = value;
        }

        // Для команди: логотип вписується в рамку повністю, з невеликим відступом
        public bool IsLogo
        {
            get => Photo.ImageStretch == Stretch.Uniform;
            set
            {
                Photo.ImageStretch = value ? Stretch.Uniform : Stretch.UniformToFill;
                PhotoFrame.Padding = new Thickness(value ? 10 : 0);
            }
        }

        public event EventHandler? Click;

        public void Show(string name, string subtitle, string details, string? imagePath)
        {
            NameText.Text = name;
            SubtitleText.Text = subtitle;
            DetailsText.Text = details;
            DetailsText.Visibility = string.IsNullOrWhiteSpace(details)
                ? Visibility.Collapsed
                : Visibility.Visible;
            Photo.SourcePath = imagePath;
            CardButton.IsHitTestVisible = true;
            CardButton.Focusable = true;
        }

        // Показати пояснення замість чемпіона (наприклад, до 1958 року для команд)
        public void ShowEmpty(string message, string explanation)
        {
            NameText.Text = message;
            NameText.FontSize = 20;
            NameText.TextWrapping = TextWrapping.Wrap;
            SubtitleText.Text = explanation;
            DetailsText.Visibility = Visibility.Collapsed;
            Photo.SourcePath = null;

            CardButton.IsHitTestVisible = false;
            CardButton.Focusable = false;
        }

        private void Card_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
