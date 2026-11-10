using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace F1Archive.Controls
{
    // Фото з папки Database\Media. Якщо фото немає, рамка лишається порожньою.
    public class MediaImage : Border
    {
        // Шлях до фото. Через DependencyProperty, щоб його можна було задати через Binding.
        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register(
            "SourcePath", typeof(string), typeof(MediaImage), new PropertyMetadata(null, SourcePathChanged));

        public string? SourcePath
        {
            get { return (string?)GetValue(SourcePathProperty); }
            set { SetValue(SourcePathProperty, value); }
        }

        // До якої ширини зменшувати фото, щоб воно займало менше пам'яті
        public int DecodeWidth { get; set; } = 480;

        // UniformToFill - фото на всю рамку, Uniform - логотип чи схема повністю
        public Stretch ImageStretch { get; set; } = Stretch.UniformToFill;

        private static void SourcePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MediaImage image = (MediaImage)d;
            image.ShowPhoto();
        }

        private void ShowPhoto()
        {
            Background = null;

            string? path = PhotoService.FullPath(SourcePath);

            if (path == null)
            {
                return;
            }

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.DecodePixelWidth = DecodeWidth;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                var brush = new ImageBrush(bitmap);
                brush.Stretch = ImageStretch;

                // Портрети вирівнюються за верхом, щоб не обрізати обличчя
                if (bitmap.PixelHeight >= bitmap.PixelWidth)
                {
                    brush.AlignmentY = AlignmentY.Top;
                }

                Background = brush;
            }
            catch (Exception)
            {
                // Файл пошкоджений - фото не показуємо
                Background = null;
            }
        }
    }
}
