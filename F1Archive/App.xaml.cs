using F1Archive.Database;
using System.IO;
using System.Windows;

namespace F1Archive
{
    public partial class App : Application
    {
        // Перед запуском перевіряємо, що файл бази на місці
        protected override void OnStartup(StartupEventArgs e)
        {
            if (!File.Exists(F1ArchiveContext.DatabasePath))
            {
                MessageBox.Show(
                    $"Не знайдено файл бази даних:\n{F1ArchiveContext.DatabasePath}",
                    "Помилка запуску",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(1);
                return;
            }

            base.OnStartup(e);
        }
    }
}
