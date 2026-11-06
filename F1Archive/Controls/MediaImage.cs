using F1Archive.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace F1Archive.Controls
{
    // Фото з папки Database\Media. Якщо фото немає, рамка лишається порожньою.
    public class MediaImage : UserControl
    {
        private readonly Border Frame = new();

        // Уже завантажені фото, щоб не читати один файл двічі
        private static readonly Dictionary<string, BitmapImage> Cache = new();

        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register(nameof(SourcePath),
            typeof(string), typeof(MediaImage), new PropertyMetadata(null, OnSourceChanged));

        public MediaImage()
        {
            Content = Frame;
        }

        public string? SourcePath
        {
            get => (string?)GetValue(SourcePathProperty);
            set => SetValue(SourcePathProperty, value);
        }

        // До якої ширини зменшувати фото (економія пам'яті)
        public int DecodeWidth { get; set; } = 480;

        // UniformToFill - фото на всю рамку, Uniform - логотип чи схема повністю
        public Stretch ImageStretch { get; set; } = Stretch.UniformToFill;

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _ = ((MediaImage)d).ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            string? requested = SourcePath;
            string? fullPath = PhotoService.FullPath(requested);
            if (fullPath == null)
            {
                Frame.Background = null;
                return;
            }
            int width = DecodeWidth;
            try
            {
                if (!Cache.TryGetValue(fullPath + "|" + width, out BitmapImage? bitmap))
                    Cache[fullPath + "|" + width] = bitmap = await Task.Run(() =>
                    {
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.UriSource = new Uri(fullPath);
                        image.DecodePixelWidth = width;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        image.Freeze();
                        return image;
                    });
                if (SourcePath != requested) return; // поки фото вантажилось, шлях змінився

                // Портрети вирівнюються за верхом, щоб не обрізати обличчя
                bool portrait = bitmap.PixelHeight >= bitmap.PixelWidth;
                Frame.Background = new ImageBrush(bitmap)
                {
                    Stretch = ImageStretch,
                    AlignmentY = portrait ? AlignmentY.Top : AlignmentY.Center
                };
            }
            catch (Exception)
            {
                Frame.Background = null; // пошкоджений файл
            }
        }
    }
}
